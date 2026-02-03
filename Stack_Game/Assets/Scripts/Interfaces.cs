
using System;
using UnityEngine;

public interface ICollectible
{
    public Action<GameObject, DataType> onCollectionStart { get; set; }

}
public interface IRecycle
{
    DataType dataType{get;}
    public Action<GameObject, DataType> onCollectionStart { get; set; }

}

public interface IPlayer
{

}