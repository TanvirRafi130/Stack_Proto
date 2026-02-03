using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGen : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] DataType generateType;
    [SerializeField] List<Transform> generatePoint;
    [SerializeField] float generateIntervalTime = 1.5f;
    [SerializeField] float heightOffset = 0.65f;
    [Header("Collection Settings")]
    [SerializeField] float packetSendingInterval = 0.85f;



    private Stack<GameObject> objectCollection = new Stack<GameObject>();
    private bool isPlayerPresent = false;
    private ICollectible currentCollector;
    private int generationIndex = 0;

    private void Start()
    {
        StartCoroutine(StartGeneration());
    }

    IEnumerator StartGeneration()
    {
        float startHeight = generatePoint[0].position.y;

        while (true)
        {
            yield return new WaitForSeconds(generateIntervalTime);

            var obj = PoolManager.Instance.GetFromPool(generateType);
            if (obj != null)
            {
                int pointIndex = generationIndex % generatePoint.Count;

                int heightLevel = generationIndex / generatePoint.Count;

                float currentHeight = startHeight + (heightLevel * heightOffset);

                var point = generatePoint[pointIndex];
                Vector3 pos = point.position;
                pos.y = currentHeight;

                obj.transform.position = pos;
                obj.transform.rotation = point.rotation;

                objectCollection.Push(obj);

                generationIndex++;
            }
        }
    }

    IEnumerator StartSendingPacket()
    {
        while (isPlayerPresent)
        {
            yield return new WaitForSeconds(packetSendingInterval);

            if (objectCollection.Count > 0)
            {
                var obj = objectCollection.Pop();
                currentCollector.onCollectionStart?.Invoke(obj, generateType);
                generationIndex--;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ICollectible>(out ICollectible collectible))
        {
            isPlayerPresent = true;
            currentCollector = collectible;
            StartCoroutine(StartSendingPacket());
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<ICollectible>(out ICollectible collectible))
        {
            isPlayerPresent = false;
            StopCoroutine(StartSendingPacket());
        }
    }




}
