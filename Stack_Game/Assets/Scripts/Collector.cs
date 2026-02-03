using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Collector : MonoBehaviour , ICollectible
{
    [SerializeField] DataType collectionType;

    public Action<GameObject, DataType> onCollectionStart { get; set; }

    private void Start()
    {
        onCollectionStart += Collect;
    }

    void Collect(GameObject obj, DataType type)
    {
       obj.transform
            .DOLocalMove(transform.localPosition, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                PoolManager.Instance.ReturnToPool(obj, collectionType);
            });
            
    }
}
