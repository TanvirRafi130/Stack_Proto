using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataSO", menuName = "ScriptableObjects/DataSO")]
public class DataSO : ScriptableObject
{

    [System.Serializable]
    public struct DataStruct
    {
        public DataType dataType;
        public GameObject prefab;
        [Range(1, 10000)]public int maxSpawn;
    }


    public List<DataStruct> dataList;

}
public enum DataType
{
    None = 0,
    ObjectTypeA = 1,
    ObjectTypeB = 2,
}