using RotaryHeart.Lib.PhysicsExtension;
using UnityEngine;
using Physics = RotaryHeart.Lib.PhysicsExtension.Physics;

public class Physics3DDebugPreview : MonoBehaviour
{
    public enum PhysicsType
    {
        BoxSingle,
        BoxAll,
        BoxNonAlloc,

        CapsuleSingle,
        CapsuleAll,
        CapsuleNonAlloc,

        Line,

        RaySingle,
        RayAll,
        RayNonAlloc,

        SphereSingle,
        SphereAll,
        SphereNonAlloc,

        CheckBox,
        CheckCapsule,
        CheckSphere,

        OverlapBox,
        OverlapBoxNonAlloc,
        OverlapCapsule,
        OverlapCapsuleNonAlloc,
        OverlapSphere,
        OverlapSphereNonAlloc
    }

    public PhysicsType castType;
    public PreviewCondition preview = PreviewCondition.Editor;
    public CastDrawType castDrawType = CastDrawType.Minimal;
    public float drawDuration = 0;
    public Color hitColor = Color.green;
    public Color noHitColor = Color.red;
    public bool useRay = false;
    public float distance = 5;

    RaycastHit[] results = new RaycastHit[5];
    Collider[] colliderResults = new Collider[5];

    // Update is called once per frame
    void Update()
    {
        Vector3 startPoint = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPoint = startPoint + direction * distance;
        Ray ray = new Ray(startPoint, direction);

        switch (castType)
        {
            case PhysicsType.BoxSingle:
                Physics.BoxCast(startPoint, Vector3.one, direction, transform.rotation, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.BoxAll:
                Physics.BoxCastAll(startPoint, Vector3.one, direction, transform.rotation, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.BoxNonAlloc:
                Physics.BoxCastNonAlloc(startPoint, Vector3.one, direction, results, transform.rotation, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;

            case PhysicsType.CapsuleSingle:
                Physics.CapsuleCast(startPoint - transform.up * 0.5f, startPoint + transform.up * 0.5f, 1, direction, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CapsuleAll:
                Physics.CapsuleCastAll(startPoint - transform.up * 0.5f, startPoint + transform.up * 0.5f, 1, direction, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.CapsuleNonAlloc:
                Physics.CapsuleCastNonAlloc(startPoint - transform.up * 0.5f, startPoint + transform.up * 0.5f, 1, direction, results, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;

            case PhysicsType.Line:
                Physics.Linecast(startPoint, endPoint, preview, drawDuration, hitColor, noHitColor);
                break;

            case PhysicsType.RaySingle:
                if (useRay)
                    Physics.Raycast(ray, distance, preview, drawDuration, hitColor, noHitColor);
                else
                    Physics.Raycast(startPoint, direction, distance, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.RayAll:
                if (useRay)
                    Physics.RaycastAll(ray, distance, preview, drawDuration, hitColor, noHitColor);
                else
                    Physics.RaycastAll(startPoint, direction, distance, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.RayNonAlloc:
                if (useRay)
                    Physics.RaycastNonAlloc(ray, results, distance, preview, drawDuration, hitColor, noHitColor);
                else
                    Physics.RaycastNonAlloc(startPoint, direction, results, distance, preview, drawDuration, hitColor, noHitColor);
                break;

            case PhysicsType.SphereSingle:
                if (useRay)
                    Physics.SphereCast(ray, 1, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                else
                    Physics.SphereCast(startPoint, 1, direction, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.SphereAll:
                if (useRay)
                    Physics.SphereCastAll(ray, 1, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                else
                    Physics.SphereCastAll(startPoint, 1, direction, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;
            case PhysicsType.SphereNonAlloc:
                if (useRay)
                    Physics.SphereCastNonAlloc(ray, 1, results, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                else
                    Physics.SphereCastNonAlloc(startPoint, 1, direction, results, distance, preview, drawDuration, hitColor, noHitColor, drawType: castDrawType);
                break;

            case PhysicsType.CheckBox:
                Physics.CheckBox(startPoint, Vector3.one * 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.CheckCapsule:
                Physics.CheckCapsule(startPoint, endPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.CheckSphere:
                Physics.CheckSphere(startPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;

            case PhysicsType.OverlapBox:
                Physics.OverlapBox(startPoint, Vector3.one * 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapBoxNonAlloc:
                Physics.OverlapBoxNonAlloc(startPoint, Vector3.one * 3, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCapsule:
                Physics.OverlapCapsule(startPoint, endPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapCapsuleNonAlloc:
                Physics.OverlapCapsuleNonAlloc(startPoint, endPoint, 3, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapSphere:
                Physics.OverlapSphere(startPoint, 3, preview, drawDuration, hitColor, noHitColor);
                break;
            case PhysicsType.OverlapSphereNonAlloc:
                Physics.OverlapSphereNonAlloc(startPoint, 3, colliderResults, preview, drawDuration, hitColor, noHitColor);
                break;
        }
    }
}
