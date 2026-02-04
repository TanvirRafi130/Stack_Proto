using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour, ICollectible, IPlayer
{
    [Header("Player Collection Settings")]
    [SerializeField] Transform collectionPointStart;
    [SerializeField] float backwardOffset = 0.5f;
    [SerializeField] float upwardOffset = 0.5f;

    public Action<GameObject, DataType> onCollectionStart { get; set; }

    // Actual collected objects
    private Dictionary<DataType, Stack<GameObject>> collectedObjects = new();

    // Permanent slot booking
    private Dictionary<DataType, int> slotIndexByType = new();
    private int nextSlotIndex = 0;

    IRecycle currentCollector;
    DataType collectorType = DataType.None;
    bool shouldSendPacket = false;

    void Start()
    {
        onCollectionStart += Collect;
    }

    // -------------------- COLLECTION --------------------

    void Collect(GameObject obj, DataType type)
    {
        // Create stack if first time
        if (!collectedObjects.ContainsKey(type))
        {
            collectedObjects[type] = new Stack<GameObject>();

            // Book permanent slot
            if (!slotIndexByType.ContainsKey(type))
            {
                slotIndexByType[type] = nextSlotIndex++;
            }
        }

        collectedObjects[type].Push(obj);

        obj.transform.SetParent(transform, true);

        UpdatePositions(true);
    }

    // -------------------- RECYCLING --------------------

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IRecycle>(out var recyclable) && !(recyclable is Player))
        {
            currentCollector = recyclable;
            collectorType = recyclable.dataType;
            shouldSendPacket = true;
            StartCoroutine(StartSendingPacket());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IRecycle>(out var recyclable) && !(recyclable is Player))
        {
            shouldSendPacket = false;
            currentCollector = null;
            collectorType = DataType.None;
        }
    }

    IEnumerator StartSendingPacket()
    {
        while (shouldSendPacket)
        {
            yield return new WaitForSeconds(0.15f);

            if (
                collectedObjects.ContainsKey(collectorType) &&
                collectedObjects[collectorType].Count > 0
            )
            {
                
                var obj = collectedObjects[collectorType].Pop();
                currentCollector.onCollectionStart?.Invoke(obj, collectorType);

                UpdatePositions(false);
            }
            else
            {
                yield break;
            }
        }
    }

    // -------------------- POSITION LOGIC --------------------

    void UpdatePositions(bool instantJump)
    {
        // Sort all DataTypes by their permanent slot index
        var orderedTypes = new List<DataType>(slotIndexByType.Keys);
        orderedTypes.Sort((a, b) => slotIndexByType[a].CompareTo(slotIndexByType[b]));

        int visualIndex = 0;

        foreach (var type in orderedTypes)
        {
            // Skip empty stacks (but slot stays reserved)
            if (!collectedObjects.ContainsKey(type) || collectedObjects[type].Count == 0)
                continue;

            Vector3 basePos =
                collectionPointStart.localPosition +
                Vector3.back * visualIndex * backwardOffset;

            int height = 0;
            foreach (var item in collectedObjects[type])
            {
                Vector3 targetPos =
                    basePos + Vector3.up * upwardOffset * (++height);

                if (instantJump)
                {
                    item.transform.DOLocalJump(targetPos, 1f, 1, 0.5f)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            item.transform.DOLocalRotate(Vector3.zero, 0.1f);
                        })
                        ;
                }
                else
                {
                    item.transform.DOLocalMove(targetPos, 0.3f)
                        .SetEase(Ease.OutBack).OnComplete(() =>
                        {
                            item.transform.DOLocalRotate(Vector3.zero, 0.1f);
                        });
                }
            }

            visualIndex++;
        }
    }
}
