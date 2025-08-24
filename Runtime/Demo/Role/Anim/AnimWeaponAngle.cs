using UnityEngine;
using XiaoCao;

public class AnimWeaponAngle : StateMachineBehaviour
{
    public Vector3 angle = Vector3.zero;

    private Transform weapon;

    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        weapon = WeaponHelper.FindWeapon(animator);
        weapon.localEulerAngles = angle;
    }


    //OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        weapon.localEulerAngles = Vector3.zero;
    }
}

