
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_ObjectView : MonoBehaviour
{
    public Object obj;

    public GameAllData gameData;

    public string inputStr = "GameDataCommon.Current";

    public object value;


    [Button]
    void GetStaticeValue()
    {
        //TODO

        //if (!string.IsNullOrEmpty(inputStr))
        //{
        //    value = ReflectionHelper.GetValueFromPath(inputStr);
        //}

    }
}

