using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using System.Collections;

public class Player : MonoBehaviour, ICollectible, IPlayer
{

    [Header("Player Collection Settings")]
    [SerializeField] Transform collectionPointStart;
    [SerializeField] float backwardOffset = 0.5f;
    [SerializeField] float upwardOffset = 0.5f;


    public Action<GameObject, DataType> onCollectionStart { get; set; }

    private Dictionary<DataType, Vector3> collectedObjectPosition = new Dictionary<DataType, Vector3>();
    private Dictionary<DataType, Stack<GameObject>> collectedObjects = new Dictionary<DataType, Stack<GameObject>>();

    IRecycle currentCollector;
    DataType collectorType = DataType.None;
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
                collectionPointStart.localPosition + (Vector3.back * collectedObjectPosition.Count * backwardOffset);
        }

        Vector3 localPlacement =
            collectedObjectPosition[type] +
            (Vector3.up * upwardOffset * collectedObjects[type].Count);

        obj.transform.SetParent(transform, worldPositionStays: true);

        obj.transform.DOLocalJump(localPlacement, 1f, 1, .5f).SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
            obj.transform.DOLocalRotate(Vector3.zero, 0.08f);
        })
        ;

        // Recalculate & animate positions of all collected objects
        //UpdatePositions();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IRecycle>(out var collectible) && !(collectible is Player))
        {
            currentCollector = collectible;
            collectorType = collectible.dataType;
            shouldSendPacket = true;
            StartCoroutine(StartSendingPacket());
            // collectible.onCollectionStart?.Invoke(other.gameObject, DataType.Packet);
        }

    }
    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IRecycle>(out var collectible) && !(collectible is Player))
        {
            shouldSendPacket = false;
            currentCollector = null;
            collectorType = DataType.None;
            StopCoroutine(StartSendingPacket());
        }
    }

    IEnumerator StartSendingPacket()
    {
        while (shouldSendPacket)
        {

            yield return new WaitForSeconds(0.15f);
            if (collectedObjects.ContainsKey(collectorType) && collectedObjects[collectorType].Count > 0)
            {
                var obj = collectedObjects[collectorType].Pop();

                currentCollector.onCollectionStart?.Invoke(obj, collectorType);

            }
            else
            {
                collectedObjects.Remove(collectorType);
                collectedObjectPosition.Remove(collectorType);
                UpdatePositions();
                yield break;
            }

        }
    }


    void UpdatePositions()
    {
        // Compute base positions for each DataType 
        var keys = new List<DataType>(collectedObjectPosition.Keys);
        int count = keys.Count;
        for (int i = 0; i < count; i++)
        {
            var newPos =
                collectedObjectPosition[keys[i]] =
                    collectionPointStart.localPosition + (Vector3.back * i * backwardOffset);


            foreach (var item in collectedObjects[keys[i]])
            {
                newPos.y = item.transform.localPosition.y;
                item.transform.DOLocalMove(newPos, 0.3f).SetEase(Ease.OutBack);
            }

        }


    }
}
