using NaughtyAttributes;
using UnityEngine;
using XiaoCao;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class IdRole : IdComponent
{
    public int raceId = 1;
    public string bodyName = "Body_P_0";
    public int aiId = 0;
    public RuntimeAnimatorController runtimeAnim;
    public Rigidbody rb;
    public CharacterController cc;
    public Collider[] triggerCols;


    public Transform Follow;
    public Transform LookAt;
    public Vector3 hpBarOffset = Vector3.up;

    [HideInInspector]
    public Transform tf;

    [ReadOnly]
    public Animator animator;

    private void Awake()
    {
        tf = transform;
    }

    private void OnDestroy()
    {
        //防止EntityMgr自身已被销毁
        if (EntityMgr.IsOn)
        {
            EntityMgr.Inst.RemoveEntity(this.GetEntity());
        }
    }


    private void OnValidate()
    {
        CheckData();
    }

    public void CheckData()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (cc == null)
            cc = GetComponent<CharacterController>();
        if (Follow == null)
        {
            Follow = transform.Find("Follow"); 
            LookAt = Follow;
        }
    }
}
