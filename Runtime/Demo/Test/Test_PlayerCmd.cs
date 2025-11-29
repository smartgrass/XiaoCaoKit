using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using XiaoCao;
using NaughtyAttributes;

/// <summary>
/// 调节地面射线检查用
/// </summary>
public class Test_PlayerCmd : MonoBehaviour
{
#if UNITY_EDITOR
    private bool isGrounded;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public Transform tf => transform;


    private void FixedUpdate()
    {
        GroundedCheck();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(tf.position.x, tf.position.y - GroundedOffset, tf.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, Layers.GROUND_MASK, QueryTriggerInteraction.Ignore);
    }
    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (isGrounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }



    [MiniBtn(nameof(ChangeToTestBody))]
    public string testChangeBodyName = "Body_E_2_Gun";
    void ChangeToTestBody()
    {
        GameAllData.CommonData.Player0.ChangeBody(testChangeBodyName);
    }

    [MiniBtn(nameof(ChangeToTestEnemy))]
    public string testChangeToEnmey = "E_2_Gun";
    void ChangeToTestEnemy()
    {
        Player0 player0 = GameAllData.CommonData.Player0;
        player0.ChangeToTestEnemy(testChangeToEnmey);
    }
    [MiniBtn(nameof(PlayRoleSKill))]
    public string testSkillCmd = "1";

    void PlayRoleSKill()
    {
        int.TryParse(testSkillCmd, out int msgNum);
        Player0 player0 = GameAllData.CommonData.Player0;
        player0.component.control.TryPlaySkill(testSkillCmd);
    }
#endif
}
