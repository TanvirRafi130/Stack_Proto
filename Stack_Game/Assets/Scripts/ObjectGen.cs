using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGen : MonoBehaviour
{
    [SerializeField] DataType generateType;
    [SerializeField] List<Transform> generatePoint;
    [SerializeField] float generateIntervalTime = 1.5f;
    [SerializeField] float heightOffset = 0.65f;



    private void Start()
    {
        StartCoroutine(StartGeneration());
    }

    IEnumerator StartGeneration()
    {
        int index = 0;
        float startHeight = generatePoint[0].position.y;

        while (true)
        {
            yield return new WaitForSeconds(generateIntervalTime);

            var obj = PoolManager.Instance.GetFromPool(generateType);
            if (obj != null)
            {
                int pointIndex = index % generatePoint.Count;

                int heightLevel = index / generatePoint.Count;

                float currentHeight = startHeight + (heightLevel * heightOffset);

                var point = generatePoint[pointIndex];
                Vector3 pos = point.position;
                pos.y = currentHeight;

                obj.transform.position = pos;
                obj.transform.rotation = point.rotation;

                index++;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        
    }


}
