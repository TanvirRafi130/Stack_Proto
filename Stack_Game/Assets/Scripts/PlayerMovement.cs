using UnityEngine;
using NaughtyAttributes;
using UnityEditor.Animations;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed in units/second")]
    public float moveSpeed = 5f;
    [Tooltip("How fast the player rotates to face movement direction")]
    public float rotationSpeed = 10f;
    [Tooltip("If true, movement is relative to the main camera's forward/right")]
    public bool useCameraRelative = true;
    [Header("Animation Settings")]
    [SerializeField] Animator anim;
    [AnimatorParam(nameof(anim))] [SerializeField] string animParamMoveSpeed;


    Rigidbody rb;

    // Touch / swipe state
    [Tooltip("Minimum swipe distance in pixels to start movement")]
    public float minSwipeDistance = 10f;
    Vector2 touchStartPos;
    bool isTouchActive = false;
    Vector3 touchMoveDir = Vector3.zero; // world-space direction derived from swipe

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        InputHandle();
    }

    void InputHandle()
    {
        // 1. Read WASD / arrow key input (Horizontal = A/D; Vertical = W/S)
        Vector3 keyboardInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        Vector3 keyboardDir = keyboardInput.normalized;

        // 2. Handle touch / mouse-drag swipe input (direction = start -> current; hold to keep moving)
        bool touchHasDirection = false;
        touchMoveDir = Vector3.zero;

        // Touch input (mobile)
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
            // Mouse drag support for testing in Editor: left mouse button acts like a touch
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

        // 3. Decide final movement direction: touch input takes priority when active and valid, otherwise keyboard input
        Vector3 finalMoveDir = Vector3.zero;
        if (touchHasDirection)
        {
            finalMoveDir = touchMoveDir;
        }
        else if (keyboardDir.sqrMagnitude >= 0.0001f)
        {
            // Convert keyboard direction to camera-relative if needed
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
            finalMoveDir = moveDir;
        }

        // 4. If we have a movement direction, rotate & move the player
        if (finalMoveDir.sqrMagnitude >= 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalMoveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            Vector3 delta = finalMoveDir * moveSpeed * Time.deltaTime;
            if (rb != null && !rb.isKinematic)
            {
                anim.SetFloat(animParamMoveSpeed, moveSpeed);
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                transform.position += delta;
            }
        }
        else
        {
             anim.SetFloat(animParamMoveSpeed, 0);
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

        // Fallback: map screen x->world x, screen y->world z
        return new Vector3(screenDir.x, 0f, screenDir.y).normalized;
    }
}
