using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

public class Test_FindRoleWeapon: MonoBehaviour
{
    public GameObject weapon;

    private void OnEnable()
    {
        try
        {
            FindWeapon();
        }
        catch { 
        
        }
    }


    [Button]
    void FindWeapon()
    {
        var Anim = transform.GetComponentInChildren<Animator>();
        weapon = transform.FindChildEx(Role.WeaponPointName).gameObject;
    }

}
