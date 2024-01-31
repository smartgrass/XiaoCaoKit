using cfg;
using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_YooSteam : MonoBehaviour
{
    [Button("Load")]
    void fun1()
    {
        //SkillSetting
        // ResMgr.ExtraLoader.CreateResourceImporter();
    }    
    [Button("Path")]
    void Path()
    {
        RoleType RoleType = RoleType.Player;
        string path = $"{ResMgr.EXTRARESDIR}/Role/{RoleType}/{RoleType}{0}.prefab";
        GameObject go = ResMgr.LoadInstan(path, PackageType.ExtraPackage);
        Debug.Log($"---  {go}");
    }
}
