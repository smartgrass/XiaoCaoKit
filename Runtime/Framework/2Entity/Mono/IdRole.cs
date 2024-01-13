using UnityEngine;
using XiaoCao;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class IdRole : IdComponent
{
    public RuntimeAnimatorController runtimeAnim;
    public Rigidbody rb;
    public CharacterController cc;


    public Transform Follow;
    public Transform LookAt;

    [HideInInspector]
    public Transform tf;

    [HideInInspector]
    public Animator animator;

    private void Awake()
    {
        tf = transform;
    }

    private void OnValidate()
    {
        if(rb==null)
            rb = GetComponent<Rigidbody>();
        if(cc==null)
            cc = GetComponent<CharacterController>();
        if (Follow == null)
            Follow = transform.Find("Follow");
    }
}
