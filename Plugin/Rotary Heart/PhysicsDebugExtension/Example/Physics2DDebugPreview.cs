using RotaryHeart.Lib.PhysicsExtension;
using UnityEngine;
using Physics = RotaryHeart.Lib.PhysicsExtension.Physics2D;

public class Physics2DDebugPreview : MonoBehaviour
{
    public enum PhysicsType
    {
        BoxSingle,
        BoxAll,
        BoxNonAlloc,

        CapsuleSingle,
        CapsuleAll,
        CapsuleNonAlloc,

        CircleSingle,
        CircleAll,
        CircleNonAlloc,

        LineSingle,
        LineAll,
        LineNonAlloc,

        RaySingle,
        RayAll,
        RayNonAlloc,

        OverlapAreaSingle,
        OverlapAreaAll,
        OverlapAreaNonAlloc,

        OverlapBox,
        OverlapBoxAll,
        OverlapBoxNonAlloc,

        OverlapCapsule,
        OverlapCapsuleAll,
        OverlapCapsuleNonAlloc,

        OverlapCircle,
        OverlapCircleAll,
        OverlapCircleNonAlloc,

        OverlapPoint,
        OverlapPointAll,
        OverlapPointNonAlloc,

        ConeSight
    }

    public PhysicsType castType;
    public PreviewCondition preview = PreviewCondition.Editor;
    public CastDrawType castDrawType = CastDrawType.Minimal;
    public float drawDuration = 0;
    public Color hitColor = Color.green;
    public Color noHitColor = Color.red;
    public float distance = 5;
    public float angle = 0;
    public Vector3 capsuleSize = new Vector2(3, 6);
    public CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Vertical;

    RaycastHit2D[] results = new RaycastHit2D[5];
    Collider2D[] colliderResults = new Collider2D[5];

    // Update is called once per frame
    void Update()
    {
        Vector3 startPoint = transform.position;
        Vector3 direction = transform.up;
        Vector3 endPoint = startPoint + direction * distance;

        switch (castType)
        {
            case PhysicsType.BoxSingle:
                Physics.BoxCast(startPoint, Vector3.one * 2, angle, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.BoxAll:
                Physics.BoxCastAll(startPoint, Vector3.one, angle, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.BoxNonAlloc:
                Physics.BoxCastNonAlloc(startPoint, Vector3.one, angle, transform.up, results, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            
            case PhysicsType.CapsuleSingle:
                Physics.CapsuleCast(startPoint, capsuleSize, capsuleDirection, angle, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CapsuleAll:
                Physics.CapsuleCastAll(startPoint, capsuleSize, capsuleDirection, angle, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CapsuleNonAlloc:
                Physics.CapsuleCastNonAlloc(startPoint, capsuleSize, capsuleDirection, angle, transform.up, results, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            
            case PhysicsType.CircleSingle:
                Physics.CircleCast(startPoint, 1, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CircleAll:
                Physics.CircleCastAll(startPoint, 1, transform.up, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CircleNonAlloc:
                Physics.CircleCastNonAlloc(startPoint, 1, transform.up, results, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;

            case PhysicsType.LineSingle:
                Physics.Linecast(startPoint, endPoint, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.LineAll:
                Physics.LinecastAll(startPoint, endPoint, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.LineNonAlloc:
                Physics.LinecastNonAlloc(startPoint, endPoint, results, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.RaySingle:
                Physics.Raycast(startPoint, direction, distance, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.RayAll:
                Physics.RaycastAll(startPoint, direction, distance, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.RayNonAlloc:
                Physics.RaycastNonAlloc(startPoint, direction, results, distance, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.OverlapAreaSingle:
                Physics.OverlapArea(startPoint - Vector3.right * 3 + Vector3.up * 3, startPoint + Vector3.right * 3 - Vector3.up * 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapAreaAll:
                Physics.OverlapAreaAll(startPoint - Vector3.right * 3 + Vector3.up * 3, startPoint + Vector3.right * 3 - Vector3.up * 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapAreaNonAlloc:
                Physics.OverlapAreaNonAlloc(startPoint - Vector3.right * 3 + Vector3.up * 3, startPoint + Vector3.right * 3 - Vector3.up * 3, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.OverlapBox:
                Physics.OverlapBox(startPoint, Vector3.one * 6, angle, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapBoxAll:
                Physics.OverlapBoxAll(startPoint, Vector3.one * 6, angle, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapBoxNonAlloc:
                Physics.OverlapBoxNonAlloc(startPoint, Vector3.one * 6, angle, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.OverlapCapsule:
                Physics.OverlapCapsule(startPoint, capsuleSize, capsuleDirection, angle, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCapsuleAll:
                Physics.OverlapCapsuleAll(startPoint, capsuleSize, capsuleDirection, angle, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCapsuleNonAlloc:
                Physics.OverlapCapsuleNonAlloc(startPoint, capsuleSize, capsuleDirection, angle, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.OverlapCircle:
                Physics.OverlapCircle(startPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCircleAll:
                Physics.OverlapCircleAll(startPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCircleNonAlloc:
                Physics.OverlapCircleNonAlloc(startPoint, 3, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            
            case PhysicsType.OverlapPoint:
                Physics.OverlapPoint(startPoint, 6, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapPointAll:
                Physics.OverlapPointAll(startPoint, 6, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapPointNonAlloc:
                Physics.OverlapPointNonAlloc(startPoint, colliderResults, 6, preview, drawDuration, hitColor, noHitColor);
                break;
        }
    }
}