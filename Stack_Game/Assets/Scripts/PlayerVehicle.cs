using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerVehicle : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units/second")]
    public float moveSpeed = 5f;
    [Tooltip("How fast the player rotates to face movement direction")]
    public float rotationSpeed = 10f;
    [Tooltip("If true, movement is relative to the main camera's forward/right")]
    public bool useCameraRelative = true;

    [Header("Touch / Swipe")]
    [Tooltip("Minimum swipe distance in pixels to start movement")]
    public float minSwipeDistance = 10f;

    [Header("Following Settings")]
    [Tooltip("List of cargos in serial order (first follows vehicle)")]
    public List<Transform> childListSerial = new List<Transform>();
    [Tooltip("Distance (in world units) between consecutive cargos along the path")]
    public float followDistance = 1.0f;
    [Tooltip("How fast followers interpolate to their target position")]
    public float followLerpSpeed = 10f;
    [Tooltip("How fast followers rotate to face movement direction")]
    public float followerRotateSpeed = 20f;

    Rigidbody rb;

    // Input / movement state
    Vector3 currentMoveDir = Vector3.zero; // world-space direction
    Vector2 touchStartPos;
    bool isTouchActive = false;
    
    List<Vector3> pathPositions = new List<Vector3>();
    float minRecordDistance = 0.01f; // min world distance to add a new sample

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // initialize path with samples laid out behind the vehicle so followers start in a line
        pathPositions.Clear();
        Vector3 head = transform.position;
        Vector3 backDir = -transform.forward;
        int samples = 50;
        // create samples from oldest (farthest behind) -> newest (head)
        for (int i = samples; i >= 0; i--)
        {
            Vector3 sample = head + backDir * followDistance * i;
            pathPositions.Add(sample);
        }
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveVehicle();
        RecordPath();
        UpdateFollowers();
        TrimPathIfNeeded();
    }

    // --- Input (keyboard + swipe) ---
    void HandleInput()
    {
        // Keyboard input (WASD / arrows)
        Vector3 keyboardInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Vector3 keyboardDir = keyboardInput.normalized;

        // Touch / mouse drag (screen-space direction => world-space direction)
        
        Vector3 touchMoveDir = Vector3.zero;
        bool touchHasDirection = false;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                isTouchActive = true;
                touchStartPos = t.position;
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                Vector2 delta = t.position - touchStartPos;
                if (delta.magnitude >= minSwipeDistance)
                {
                    Vector2 dirScreen = delta.normalized;
                    touchMoveDir = ScreenDirectionToWorld(dirScreen);
                    touchHasDirection = true;
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                isTouchActive = false;
            }
        }
        else
        {
            // Mouse support (left button acts as swipe)
            if (Input.GetMouseButtonDown(0))
            {
                isTouchActive = true;
                touchStartPos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && isTouchActive)
            {
                Vector2 curr = (Vector2)Input.mousePosition;
                Vector2 delta = curr - touchStartPos;
                if (delta.magnitude >= minSwipeDistance)
                {
                    Vector2 dirScreen = delta.normalized;
                    touchMoveDir = ScreenDirectionToWorld(dirScreen);
                    touchHasDirection = true;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isTouchActive = false;
            }
        }

        // Priority: active valid touch swipe > keyboard
        if (touchHasDirection)
        {
            currentMoveDir = touchMoveDir;
        }
        else if (keyboardDir.sqrMagnitude >= 0.0001f)
        {
            Vector3 moveDir = keyboardDir;
            if (useCameraRelative && Camera.main != null)
            {
                Vector3 camForward = Camera.main.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                Vector3 camRight = Camera.main.transform.right;
                camRight.y = 0f;
                camRight.Normalize();
                moveDir = (camRight * keyboardDir.x + camForward * keyboardDir.z).normalized;
            }
            currentMoveDir = moveDir;
        }
        else
        {
            currentMoveDir = Vector3.zero;
        }
    }

    // Convert a normalized screen-space direction (x = right, y = up) to a world-space XZ direction.
    Vector3 ScreenDirectionToWorld(Vector2 screenDir)
    {
        if (useCameraRelative && Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 worldDir = (camRight * screenDir.x + camForward * screenDir.y).normalized;
            return worldDir;
        }

        return new Vector3(screenDir.x, 0f, screenDir.y).normalized;
    }

    // --- Movement ---
    void MoveVehicle()
    {
        if (currentMoveDir.sqrMagnitude >= 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentMoveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

            Vector3 delta = currentMoveDir * moveSpeed * Time.fixedDeltaTime;
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                transform.position += delta;
            }
        }
    }

    // --- Path sampling ---
    void RecordPath()
    {
        Vector3 headPos = transform.position;
        if ((headPos - pathPositions[pathPositions.Count - 1]).sqrMagnitude >= minRecordDistance * minRecordDistance)
        {
            pathPositions.Add(headPos);
        }
    }

    // Keep path length manageable while preserving points needed by followers
    
    void TrimPathIfNeeded()
    {
        // approximate needed samples: (distance per follower) / minRecordDistance * count + margin
        float neededLength = (childListSerial.Count + 2) * followDistance / Mathf.Max(minRecordDistance, 0.0001f);
        int maxSamples = Mathf.CeilToInt(neededLength) + 50;
        if (pathPositions.Count > maxSamples)
        {
            int removeCount = pathPositions.Count - maxSamples;
            pathPositions.RemoveRange(0, removeCount);
        }
    }

    // --- Followers update ---
    void UpdateFollowers()
    {
        if (childListSerial == null || childListSerial.Count == 0) return;

        for (int i = 0; i < childListSerial.Count; i++)
        {
            Transform follower = childListSerial[i];
            if (follower == null) continue;

            float desiredDistance = followDistance * (i + 1); // first follower at followDistance, second at 2* etc.
            Vector3 targetPos = GetPointAtDistanceFromHead(desiredDistance);

            // smoothly move follower toward target position
            follower.position = Vector3.Lerp(follower.position, targetPos, followLerpSpeed * Time.fixedDeltaTime);

            // rotate to face movement direction (prefer movement vector along path ahead)
            Vector3 lookDir = GetPointAtDistanceFromHead(desiredDistance - 0.2f) - follower.position;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                follower.rotation = Quaternion.Slerp(follower.rotation, targetRot, followerRotateSpeed * Time.fixedDeltaTime);
            }
        }
    }

    Vector3 GetPointAtDistanceFromHead(float distanceFromHead)
    {
        if (pathPositions.Count == 0) return transform.position;
        if (distanceFromHead <= 0f) return pathPositions[pathPositions.Count - 1]; // head

        float remaining = distanceFromHead;
        // iterate from newest->oldest
        for (int s = pathPositions.Count - 1; s > 0; s--)
        {
            Vector3 a = pathPositions[s];
            Vector3 b = pathPositions[s - 1];
            float segLen = Vector3.Distance(a, b);
            if (segLen >= remaining)
            {
                // interpolate on this segment
                float t = remaining / segLen;
                return Vector3.Lerp(a, b, t);
            }
            remaining -= segLen;
        }

        // if we get here, path wasn't long enough; return oldest point
        return pathPositions[0];
    }

    // Helper to attach a cargo at runtime (keeps it in serial list)
    public void AttachCargo(Transform cargo)
    {
        if (cargo == null) return;
        // keep cargo unparented so it follows independently in world space
        cargo.SetParent(null);
        childListSerial.Add(cargo);

        // place the newly attached cargo at its follow target so it doesn't jump to the head
        float desiredDistance = followDistance * childListSerial.Count;
        Vector3 targetPos = GetPointAtDistanceFromHead(desiredDistance);
        cargo.position = targetPos;

        // match rotation to the object ahead (or the head if first)
        if (childListSerial.Count == 1)
            cargo.rotation = transform.rotation;
        else
            cargo.rotation = childListSerial[childListSerial.Count - 2].rotation;
    }
}

