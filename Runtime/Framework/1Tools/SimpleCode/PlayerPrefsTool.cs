using UnityEngine;
#if UNITY_EDITOR
#endif

public static class PlayerPrefsTool 
{
    public static void SetKeyBool(this string key,bool isOn)
    {
        PlayerPrefs.SetInt(key,isOn.ToInt());
    }
    public static bool GetKeyBool(this string key)
    {
        return PlayerPrefs.GetInt(key).ToBool();
    }    
    public static string GetKeyString(this string key,string defaultValue = ""){
        return PlayerPrefs.GetString(key,defaultValue);
    }    
    public static void SetKeyString(this string key,string value){
        PlayerPrefs.SetString(key,value);
    }


    public static bool ToBool(this int num)
    {
        return num != 0;
    }

    public static int ToInt(this bool value)
    {
        return value ? 1 : 0;
    }
}
