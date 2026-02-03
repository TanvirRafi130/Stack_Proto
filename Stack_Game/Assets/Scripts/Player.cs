using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class Player : MonoBehaviour, ICollectible, IPlayer
{

    [Header("Player Collection Settings")]
    [SerializeField] Transform collectionPointStart;
    [SerializeField] float backwardOffset = 0.5f;
    [SerializeField] float upwardOffset = 0.5f;


    public Action<GameObject, DataType> onCollectionStart { get; set; }

    private Dictionary<DataType, Vector3> collectedObjectPosition = new Dictionary<DataType, Vector3>();
    private Dictionary<DataType, Stack<GameObject>> collectedObjects = new Dictionary<DataType, Stack<GameObject>>();

    ICollectible currentCollector;
    bool shouldSendPacket = false;

    // Start is called before the first frame update
    void Start()
    {
        onCollectionStart += Collect;
    }





    void Collect(GameObject obj, DataType type)
    {
        if (!collectedObjects.ContainsKey(type))
        {
            collectedObjects[type] = new Stack<GameObject>();
        }

        collectedObjects[type].Push(obj);

        if (!collectedObjectPosition.ContainsKey(type))
        {
            collectedObjectPosition[type] =
                collectionPointStart.localPosition + (Vector3.back * collectedObjectPosition.Count);
        }

        Vector3 localPlacement =
            collectedObjectPosition[type] +
            (Vector3.up * upwardOffset * collectedObjects[type].Count);

        obj.transform.SetParent(transform, worldPositionStays: false);

        obj.transform
            .DOLocalMove(localPlacement, 0.5f)
            .SetEase(Ease.OutBack);
    }

    void OnTriggerEnter(Collider other)
    {
        if (TryGetComponent<ICollectible>(out var collectible) && !(collectible is Player))
        {
            currentCollector = collectible;
            shouldSendPacket = true;
           // StartCoroutine(StartSendingPacket());
           // collectible.onCollectionStart?.Invoke(other.gameObject, DataType.Packet);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (TryGetComponent<ICollectible>(out var collectible) && !(collectible is Player))
        {
            shouldSendPacket = false;
            //StopCoroutine(StartSendingPacket());
        }
    }

/*     IEnumerator StartSendingPacket()
    {
        while (shouldSendPacket)
        {
            yield return new WaitForSeconds(0.15f);

            if (objectCollection.Count > 0)
            {
                var obj = objectCollection.Pop();
                currentCollector.onCollectionStart?.Invoke(obj, generateType);
                generationIndex--;
            }
        }
    } */

}
