using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Collector : MonoBehaviour , IRecycle
{
    [SerializeField] DataType collectionType;

    public Action<GameObject, DataType> onCollectionStart { get; set; }

    public DataType dataType => collectionType;

    private void Start()
    {
        onCollectionStart += Collect;
    }

    void Collect(GameObject obj, DataType type)
    {
        obj.transform.parent = null;
       obj.transform
            .DOLocalMove(transform.localPosition, 0.5f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                PoolManager.Instance.ReturnToPool(obj, collectionType);
            });
            
    }
}
