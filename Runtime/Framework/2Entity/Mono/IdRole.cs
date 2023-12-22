using UnityEngine;
using XiaoCao;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class IdRole : IdComponent
{
    public RuntimeAnimatorController runtimeAnim;
    public Rigidbody rb;
    public CharacterController cc;


    [HideInInspector]
    public Animator animator;

    private void OnValidate()
    {
        if(rb==null)
            rb = GetComponent<Rigidbody>();
        if(cc==null)
            cc = GetComponent<CharacterController>();
    }
}
