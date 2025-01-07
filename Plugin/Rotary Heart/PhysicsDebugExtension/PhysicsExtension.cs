using UnityEngine;
using UEPhysics = UnityEngine.Physics;

namespace RotaryHeart.Lib.PhysicsExtension
{
    public enum PreviewCondition
    {
        None, Editor, Game, Both
    }

    public enum CastDrawType
    {
        Minimal, Complete
    }

    /// <summary>
    /// This is an extension for UnityEngine.Physics, it has all the cast, overlap, and checks with an option to preview them.
    /// </summary>
    public static partial class Physics
    {
        #region Unity Engine Physics
        //Global variables for use on default values, this is left here so that it can be changed easily
        public static Quaternion M_orientation = default(Quaternion);
        public static float M_maxDistance = Mathf.Infinity;
        public static int M_layerMask = -1;
        public static QueryTriggerInteraction M_queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        internal static Color M_castColor = new Color(1, 0.5f, 0, 1);

        #region BoxCast

        #region Boxcast single
        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return BoxCast(center, halfExtents, direction, out rayInfo, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(center, halfExtents, direction, out rayInfo, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit rayInfo, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(center, halfExtents, direction, out rayInfo, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            bool collided = UEPhysics.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    maxDistance = hitInfo.distance;
                }
                
                DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
                    orientation, drawDuration, drawType, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Boxcast all
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(center, halfExtents, direction, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit[] hitInfo = UEPhysics.BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;
                
                foreach (RaycastHit hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(center + direction * hit.distance, halfExtents, hitColor.Value, orientation, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
                    orientation, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }
        #endregion

        #region Boxcast non alloc
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, M_orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int size = UEPhysics.BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit hit = results[i];
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(center + direction * hit.distance, halfExtents, hitColor.Value, orientation, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugBoxCast(center, halfExtents, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
                    orientation, drawDuration, drawType, preview, drawDepth);
            }

            return size;
        }
        #endregion

        #endregion

        #region Capsule Cast

        #region Capsulecast single
        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return CapsuleCast(point1, point2, radius, direction, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit rayInfo;
            return CapsuleCast(point1, point2, radius, direction, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            bool collided = UEPhysics.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    maxDistance = hitInfo.distance;
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Capsulecast all
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(point1, point2, radius, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit[] hitInfo = UEPhysics.CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value, radius, true, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                
                DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }
        #endregion

        #region Capsulecast non alloc
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int size = UEPhysics.CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    collided = true;

                    RaycastHit hit = results[i];

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value, radius, true, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                
                DebugExtensions.DebugCapsuleCast(point1, point2, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return size;
        }
        #endregion

        #endregion

        #region Check Box
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckBox(center, halfExtents, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckBox(center, halfExtents, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckBox(center, halfExtents, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.CheckBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Check Capsule
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckCapsule(start, end, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckCapsule(start, end, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.CheckCapsule(start, end, radius, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCapsule(start, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Check Sphere
        public static bool CheckSphere(Vector3 position, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckSphere(position, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckSphere(Vector3 position, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return CheckSphere(position, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool CheckSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.CheckSphere(position, radius, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Linecast
        public static bool Linecast(Vector3 start, Vector3 end, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Linecast(start, end, out rayInfo, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Linecast(Vector3 start, Vector3 end, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Linecast(start, end, out rayInfo, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Linecast(Vector3 start, Vector3 end, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Linecast(start, end, out rayInfo, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Linecast(start, end, out hitInfo, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Linecast(start, end, out hitInfo, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.Linecast(start, end, out hitInfo, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                if (collided)
                {
                    end = hitInfo.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(start, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Overlap Box
        #region OverlapBox alloc
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(center, halfExtents, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(center, halfExtents, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(center, halfExtents, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider[] colliders = UEPhysics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = colliders.Length > 0;

                DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
            }

            return colliders;
        }
        #endregion

        #region OverlapBox non alloc
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, M_orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = size > 0;

                DebugExtensions.DebugBox(center, halfExtents, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), orientation, drawDuration, preview, drawDepth);
            }

            return size;
        }
        #endregion
        #endregion

        #region Overlap Capsule
        #region OverlapCapsule alloc
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsule(point0, point1, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsule(point0, point1, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider[] colliders = UEPhysics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = colliders.Length > 0;

                DebugExtensions.DebugCapsule(point0, point1, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
            }

            return colliders;
        }
        #endregion

        #region OverlapCapsule non alloc
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = size > 0;

                DebugExtensions.DebugCapsule(point0, point1, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, false, drawDuration, preview, drawDepth);
            }

            return size;
        }
        #endregion
        #endregion

        #region Overlap Sphere
        #region OverlapSphere alloc
        public static Collider[] OverlapSphere(Vector3 position, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapSphere(position, radius, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapSphere(position, radius, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider[] colliders = UEPhysics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = colliders.Length > 0;

                DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
            }

            return colliders;
        }
        #endregion

        #region OverlapSphere non alloc
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapSphereNonAlloc(position, radius, results, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapSphereNonAlloc(position, radius, results, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics.OverlapSphereNonAlloc(position, radius, results, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = size > 0;

                DebugExtensions.DebugWireSphere(position, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview, drawDepth);
            }

            return size;
        }
        #endregion
        #endregion

        #region Raycast

        #region Raycast single
        #region Vector3
        public static bool Raycast(Vector3 origin, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(origin, direction, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(origin, direction, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(origin, direction, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(origin, direction, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    end = hitInfo.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Ray
        public static bool Raycast(Ray ray, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(ray, out rayInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(ray, out rayInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(ray, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(ray, out rayInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(ray, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit rayInfo;
            return Raycast(ray, out rayInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(ray, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            bool collided = UEPhysics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    end = hitInfo.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(ray.origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return collided;
        }

        #endregion
        #endregion

        #region Raycast all
        #region Vector3
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, maxDistance, (int)layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit[] raycastInfo = UEPhysics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                Vector3 previewOrigin = origin;
                Vector3 sectionOrigin = origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit hit in raycastInfo)
                {
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return raycastInfo;
        }
        #endregion

        #region Ray
        public static RaycastHit[] RaycastAll(Ray ray, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(ray, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(ray, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(ray, maxDistance, (int)layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit[] raycastInfo = UEPhysics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                Vector3 previewOrigin = ray.origin;
                Vector3 sectionOrigin = ray.origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit hit in raycastInfo)
                {
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((ray.origin - hit.point).sqrMagnitude > (ray.origin - previewOrigin).sqrMagnitude)
                    {
                        previewOrigin = hit.point;
                    }

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return raycastInfo;
        }
        #endregion
        #endregion

        #region Raycast non alloc
        #region Vector3
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                Vector3 previewOrigin = origin;
                Vector3 sectionOrigin = origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit hit = results[i];
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
                    {
                        previewOrigin = hit.point;
                    }

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }
        #endregion

        #region Ray
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(ray, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(ray, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, LayerMask layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(ray, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = ray.origin + ray.direction * (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                Vector3 previewOrigin = ray.origin;
                Vector3 sectionOrigin = ray.origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit hit = results[i];
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((ray.origin - hit.point).sqrMagnitude > (ray.origin - previewOrigin).sqrMagnitude)
                    {
                        previewOrigin = hit.point;
                    }

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, end, noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }
        #endregion
        #endregion

        #endregion

        #region Sphere Cast
        #region Spherecast single
        #region Vector3
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(origin, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(origin, radius, direction, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            bool collided = UEPhysics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    maxDistance = hitInfo.distance;
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, drawType, preview, drawDepth);
            }

            return collided;
        }
        #endregion

        #region Ray
        public static bool SphereCast(Ray ray, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(ray, radius, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(ray, radius, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(ray, radius, out hitInfo, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit hitInfo;
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            bool collided = UEPhysics.SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                if (collided)
                {
                    maxDistance = hitInfo.distance;
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }
                
                DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, drawType, preview, drawDepth);
            }

            return collided;
        }
        #endregion
        #endregion

        #region Spherecast all
        #region Vector3
        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(origin, radius, direction, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit[] hitInfo = UEPhysics.SphereCastAll(origin, radius, direction, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugWireSphere(origin + direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);

                DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }
        #endregion

        #region Ray
        public static RaycastHit[] SphereCastAll(Ray ray, float radius, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(ray, radius, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(ray, radius, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastAll(ray, radius, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit[] hitInfo = UEPhysics.SphereCastAll(ray, radius, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugWireSphere(ray.origin + ray.direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                
                DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }
        #endregion
        #endregion

        #region Spherecast non alloc
        #region Vector3
        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int size = UEPhysics.SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit hit = results[i];
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugWireSphere(origin + direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                
                DebugExtensions.DebugSphereCast(origin, direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
            }

            return size;
        }
        #endregion

        #region Ray
        public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(ray, radius, results, M_maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, M_layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, M_queryTriggerInteraction, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int size = UEPhysics.SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, queryTriggerInteraction);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit hit = results[i];
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugWireSphere(ray.origin + ray.direction * hit.distance, hitColor.Value, radius, drawDuration, preview, drawDepth);
                }

                maxDistance = (maxDistance == M_maxDistance ? 1000 * 1000 : maxDistance);
                
                DebugExtensions.DebugSphereCast(ray.origin, ray.direction, maxDistance, collided ? hitColor.Value : noHitColor.Value, radius, drawDuration, drawType, preview, drawDepth);
            }

            return size;
        }
        #endregion
        #endregion
        #endregion

        #endregion
    }
}