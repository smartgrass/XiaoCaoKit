using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using XiaoCao;

public class Test_DrawGizmos : MonoBehaviour
{
    private bool isGrounded;
    float GroundedOffset = -0.14f;
    float GroundedRadius = 0.28f;
    public Transform tf => transform;


    private void FixedUpdate()
    {
        GroundedCheck();
    }

    private void GroundedCheck()
    {
        LayerMask GroundLayers = LayerMask.GetMask("Default");
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(tf.position.x, tf.position.y - GroundedOffset, tf.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
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
}
