
using System;
using UnityEngine;

public interface ICollectible
{
    public Action<GameObject, DataType> onCollectionStart { get; set; }

}

public interface IPlayer
{

}