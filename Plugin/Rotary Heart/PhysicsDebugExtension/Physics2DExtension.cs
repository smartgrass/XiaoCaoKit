using System.Collections.Generic;
using UnityEngine;
using UEPhysics2D = UnityEngine.Physics2D;

namespace RotaryHeart.Lib.PhysicsExtension
{
    /// <summary>
    /// This is an extension for UnityEngine.Physics2D, it has all the cast and overlap with an option to preview them.
    /// </summary>
    public static partial class Physics2D
    {
        #region Unity Engine Physics

        //Global variables for use on default values, this is left here so that it can be changed easily
        public static float M_maxDistance = Mathf.Infinity;
        public static int M_layerMask = -1;

        #region BoxCast

        #region BoxCast Single

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None,
            float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, LayerMask layerMask, 
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, LayerMask layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, LayerMask layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D hitInfo = UEPhysics2D.BoxCast(origin, size, angle, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                bool collided = hitInfo.collider != null;
                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (collided)
                {
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    distance = hitInfo.distance;
                }

                DebugExtensions.DebugBoxCast(origin, size, direction, distance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
                    rot, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.BoxCast(origin, size, angle, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                size /= 2;
                Quaternion rot = Quaternion.Euler(0, 0, angle);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(origin + direction * hit.distance, size, hitColor.Value, rot, drawDuration, preview, drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugBoxCast(origin, size, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    Quaternion.identity, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCast(origin, size, angle, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static int BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.BoxCast(origin, size, angle, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                size /= 2;
                Quaternion rot = Quaternion.Euler(0, 0, angle);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    
                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(origin + direction * hit.distance, size, hitColor.Value, rot, drawDuration, preview, drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugBoxCast(origin, size, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    Quaternion.identity, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region BoxCast All

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(origin, size, angle, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(origin, size, angle, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(origin, size, angle, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastAll(origin, size, angle, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D[] BoxCastAll(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D[] hitInfo = UEPhysics2D.BoxCastAll(origin, size, angle, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;

                size /= 2;
                Quaternion rot = Quaternion.Euler(0, 0, angle);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit2D hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(origin + direction * hit.distance, size, hitColor.Value, rot, drawDuration, preview, drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugBoxCast(origin, size, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    rot, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        #endregion

        #region BoxCast non alloc

        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(origin, size, angle, direction, results, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(origin, size, angle, direction, results, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(origin, size, angle, direction, results, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return BoxCastNonAlloc(origin, size, angle, direction, results, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int BoxCastNonAlloc(Vector2 origin, Vector2 size, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.BoxCastNonAlloc(origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;
                size /= 2;

                Quaternion rot = Quaternion.Euler(0, 0, angle);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];

                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugBox(origin + direction * hit.distance, size, hitColor.Value, rot, drawDuration, preview, drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugBoxCast(origin, size, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    rot, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region CapsuleCast

        #region CapsuleCast single

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D hitInfo = UEPhysics2D.CapsuleCast(origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (hitInfo.collider != null)
                {
                    collided = true;
                    distance = hitInfo.distance;
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                if (capsuleDirection == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + origin;
                point2 = (Vector2)(rot * point2) + origin;

                DebugExtensions.DebugOneSidedCapsuleCast(point1, point2, direction, distance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CapsuleCast(origin, size, capsuleDirection, angle, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (capsuleDirection == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + origin;
                point2 = (Vector2)(rot * point2) + origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;
                
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    
                    DebugExtensions.DebugOneSidedCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value,
                        radius, true, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugOneSidedCapsuleCast(point1, point2, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCast(origin, size, capsuleDirection, angle, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCast(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CapsuleCast(origin, size, capsuleDirection, angle, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (capsuleDirection == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + origin;
                point2 = (Vector2)(rot * point2) + origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;
                
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    
                    DebugExtensions.DebugOneSidedCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value,
                        radius, true, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugOneSidedCapsuleCast(point1, point2, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region CapsuleCast All

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(origin, size, capsuleDirection, angle, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance,
                preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(origin, size, capsuleDirection, angle, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(origin, size, capsuleDirection, angle, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastAll(origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CapsuleCastAll(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D[] hitInfo = UEPhysics2D.CapsuleCastAll(origin, size, capsuleDirection, angle, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                bool collided = false;
                float maxDistanceRay = 0;

                if (capsuleDirection == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + origin;
                point2 = (Vector2)(rot * point2) + origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit2D hit in hitInfo)
                {
                    collided = true;

                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugOneSidedCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value,
                        radius, true, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugOneSidedCapsuleCast(point1, point2, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        #endregion

        #region CapsuleCast non alloc

        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, results, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance,
                M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, results, distance, UEPhysics2D.AllLayers, -M_maxDistance,
                M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, results, distance, layerMask, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CapsuleCastNonAlloc(Vector2 origin, Vector2 size, CapsuleDirection2D capsuleDirection, float angle, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CapsuleCastNonAlloc(origin, size, capsuleDirection, angle, direction, results, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                float maxDistanceRay = 0;

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (capsuleDirection == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + origin;
                point2 = (Vector2)(rot * point2) + origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    
                    if (hit.distance > maxDistanceRay)
                        maxDistanceRay = hit.distance;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DebugOneSidedCapsule(point1 + direction * hit.distance, point2 + direction * hit.distance, hitColor.Value,
                        radius, true, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DebugOneSidedCapsuleCast(point1, point2, direction, distance, collided ? hitColor.Value : noHitColor.Value,
                    radius, drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region CircleCast

        #region CircleCast single

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D hitInfo = UEPhysics2D.CircleCast(origin, radius, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                bool collided = hitInfo.collider != null;

                if (collided)
                {
                    DebugExtensions.DebugPoint(hitInfo.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    distance = hitInfo.distance;
                }

                DebugExtensions.DebugCircleCast(origin, direction, distance, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius,
                    drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CircleCast(origin, radius, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                bool collided = false;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugCircle(origin + direction * hit.distance, Vector3.forward, hitColor.Value, radius, drawDuration, preview,
                        drawDepth);
                }

                DebugExtensions.DebugCircleCast(origin, direction, distance, collided ? hitColor.Value : noHitColor.Value, radius,
                    drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCast(origin, radius, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth,
                drawType);
        }

        public static int CircleCast(Vector2 origin, float radius, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CircleCast(origin, radius, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                bool collided = false;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugCircle(origin + direction * hit.distance, Vector3.forward, hitColor.Value, radius, drawDuration, preview,
                        drawDepth);
                }

                DebugExtensions.DebugCircleCast(origin, direction, distance, collided ? hitColor.Value : noHitColor.Value, radius,
                    drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region CircleCast All

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastAll(origin, radius, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastAll(origin, radius, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastAll(origin, radius, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastAll(origin, radius, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth, drawType);
        }

        public static RaycastHit2D[] CircleCastAll(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            RaycastHit2D[] hitInfo = UEPhysics2D.CircleCastAll(origin, radius, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit2D hit in hitInfo)
                {
                    collided = true;
                    
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugCircle(origin + direction * hit.distance, Vector3.forward, hitColor.Value, radius, drawDuration, preview,
                        drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugCircleCast(origin, direction, distance, collided ? hitColor.Value : noHitColor.Value, radius,
                    drawDuration, drawType, preview, drawDepth);
            }

            return hitInfo;
        }

        #endregion

        #region CircleCast non alloc

        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastNonAlloc(origin, radius, direction, results, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview,
                drawDuration, hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastNonAlloc(origin, radius, direction, results, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth, drawType);
        }

        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastNonAlloc(origin, radius, direction, results, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            return CircleCastNonAlloc(origin, radius, direction, results, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth, drawType);
        }

        public static int CircleCastNonAlloc(Vector2 origin, float radius, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false, CastDrawType drawType = CastDrawType.Minimal)
        {
            int count = UEPhysics2D.CircleCastNonAlloc(origin, radius, direction, results, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < count; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;

                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DebugCircle(origin + direction * hit.distance, Vector3.forward, hitColor.Value, radius, drawDuration, preview,
                        drawDepth);
                }

                distance = (distance == M_maxDistance ? 1000 * 1000 : distance);

                DebugExtensions.DebugCircleCast(origin, direction, distance, collided ? hitColor.Value : noHitColor.Value, radius,
                    drawDuration, drawType, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region LineCast

        #region LineCast single

        public static RaycastHit2D Linecast(Vector2 start, Vector2 end,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Linecast(start, end, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Linecast(start, end, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Linecast(start, end, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D Linecast(Vector2 start, Vector2 end, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit2D hitInfo = UEPhysics2D.Linecast(start, end, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;

                if (hitInfo.collider != null)
                {
                    collided = true;
                    end = hitInfo.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(start, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return hitInfo;
        }

        public static int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.Linecast(start, end, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;

                Vector2 previewOrigin = start;
                Vector2 sectionOrigin = start;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((start - hit.point).sqrMagnitude > (start - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(start, end, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }

        public static int Linecast(Vector2 start, Vector2 end, ContactFilter2D contactFilter, List<RaycastHit2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.Linecast(start, end, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;

                Vector2 previewOrigin = start;
                Vector2 sectionOrigin = start;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((start - hit.point).sqrMagnitude > (start - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(start, end, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }

        #endregion

        #region LineCast All

        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastAll(start, end, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastAll(start, end, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastAll(start, end, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D[] LinecastAll(Vector2 start, Vector2 end, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit2D[] hitInfo = UEPhysics2D.LinecastAll(start, end, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;

                Vector2 previewOrigin = start;
                Vector2 sectionOrigin = start;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit2D hit in hitInfo)
                {
                    collided = true;
                    
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((start - hit.point).sqrMagnitude > (start - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(start, end, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return hitInfo;
        }

        #endregion

        #region LineCast non alloc

        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastNonAlloc(start, end, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastNonAlloc(start, end, results, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return LinecastNonAlloc(start, end, results, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int LinecastNonAlloc(Vector2 start, Vector2 end, RaycastHit2D[] results, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.LinecastNonAlloc(start, end, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;

                Vector2 previewOrigin = start;
                Vector2 sectionOrigin = start;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((start - hit.point).sqrMagnitude > (start - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(start, end, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }

        #endregion

        #endregion

        #region OverlapArea

        #region OverlapArea single

        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapArea(pointA, pointB, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapArea(pointA, pointB, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapArea(pointA, pointB, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapArea(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D collider = UEPhysics2D.OverlapArea(pointA, pointB, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                Vector2 topLeft = pointA;
                Vector2 bottomLeft = new Vector2(pointA.x, pointB.y);
                Vector2 bottomRight = pointB;
                Vector2 topRight = new Vector2(pointB.x, pointA.y);

                Color color = collider != null ? (hitColor ?? Color.green) : (noHitColor ?? Color.red);

                //|
                DebugExtensions.DrawLine(topLeft, bottomLeft, color, drawDuration, preview, drawDepth);

                //_
                DebugExtensions.DrawLine(bottomLeft, bottomRight, color, drawDuration, preview, drawDepth);

                // |
                DebugExtensions.DrawLine(bottomRight, topRight, color, drawDuration, preview, drawDepth);

                //-
                DebugExtensions.DrawLine(topRight, topLeft, color, drawDuration, preview, drawDepth);
            }

            return collider;
        }
        
        public static int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.OverlapArea(pointA, pointB, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                Vector2 topLeft = pointA;
                Vector2 bottomLeft = new Vector2(pointA.x, pointB.y);
                Vector2 bottomRight = pointB;
                Vector2 topRight = new Vector2(pointB.x, pointA.y);

                Color color = size > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red);

                //|
                DebugExtensions.DrawLine(topLeft, bottomLeft, color, drawDuration, preview, drawDepth);

                //_
                DebugExtensions.DrawLine(bottomLeft, bottomRight, color, drawDuration, preview, drawDepth);

                // |
                DebugExtensions.DrawLine(bottomRight, topRight, color, drawDuration, preview, drawDepth);

                //-
                DebugExtensions.DrawLine(topRight, topLeft, color, drawDuration, preview, drawDepth);
            }

            return size;
        }
        
        public static int OverlapArea(Vector2 pointA, Vector2 pointB, ContactFilter2D contactFilter, List<Collider2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.OverlapArea(pointA, pointB, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                Vector2 topLeft = pointA;
                Vector2 bottomLeft = new Vector2(pointA.x, pointB.y);
                Vector2 bottomRight = pointB;
                Vector2 topRight = new Vector2(pointB.x, pointA.y);

                Color color = size > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red);

                //|
                DebugExtensions.DrawLine(topLeft, bottomLeft, color, drawDuration, preview, drawDepth);

                //_
                DebugExtensions.DrawLine(bottomLeft, bottomRight, color, drawDuration, preview, drawDepth);

                // |
                DebugExtensions.DrawLine(bottomRight, topRight, color, drawDuration, preview, drawDepth);

                //-
                DebugExtensions.DrawLine(topRight, topLeft, color, drawDuration, preview, drawDepth);
            }

            return size;
        }
        
        #endregion

        #region OverlapArea all

        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaAll(pointA, pointB, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaAll(pointA, pointB, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaAll(pointA, pointB, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapAreaAll(Vector2 pointA, Vector2 pointB, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D[] colliders = UEPhysics2D.OverlapAreaAll(pointA, pointB, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                Vector2 topLeft = pointA;
                Vector2 bottomLeft = new Vector2(pointA.x, pointB.y);
                Vector2 bottomRight = pointB;
                Vector2 topRight = new Vector2(pointB.x, pointA.y);

                Color color = colliders.Length > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red);

                //|
                DebugExtensions.DrawLine(topLeft, bottomLeft, color, drawDuration, preview, drawDepth);

                //_
                DebugExtensions.DrawLine(bottomLeft, bottomRight, color, drawDuration, preview, drawDepth);

                // |
                DebugExtensions.DrawLine(bottomRight, topRight, color, drawDuration, preview, drawDepth);

                //-
                DebugExtensions.DrawLine(topRight, topLeft, color, drawDuration, preview, drawDepth);
            }

            return colliders;
        }

        #endregion

        #region OverlapArea non alloc

        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaNonAlloc(pointA, pointB, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaNonAlloc(pointA, pointB, results, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapAreaNonAlloc(pointA, pointB, results, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapAreaNonAlloc(Vector2 pointA, Vector2 pointB, Collider2D[] results, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.OverlapAreaNonAlloc(pointA, pointB, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                Vector2 topLeft = pointA;
                Vector2 bottomLeft = new Vector2(pointA.x, pointB.y);
                Vector2 bottomRight = pointB;
                Vector2 topRight = new Vector2(pointB.x, pointA.y);

                Color color = size > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red);

                //|
                DebugExtensions.DrawLine(topLeft, bottomLeft, color, drawDuration, preview, drawDepth);

                //_
                DebugExtensions.DrawLine(bottomLeft, bottomRight, color, drawDuration, preview, drawDepth);

                // |
                DebugExtensions.DrawLine(bottomRight, topRight, color, drawDuration, preview, drawDepth);

                //-
                DebugExtensions.DrawLine(topRight, topLeft, color, drawDuration, preview, drawDepth);
            }

            return size;
        }

        #endregion

        #endregion

        #region OverlapBox

        #region OverlapBox single

        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(point, size, angle, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(point, size, angle, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBox(point, size, angle, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapBox(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D collider = UEPhysics2D.OverlapBox(point, size, angle, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                DebugExtensions.DebugBox(point, size, collider ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), Quaternion.Euler(0, 0, angle),
                    drawDuration, preview, drawDepth);
            }

            return collider;
        }

        public static int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapBox(point, size, angle, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                DebugExtensions.DebugBox(point, size, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), Quaternion.Euler(0, 0, angle),
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        public static int OverlapBox(Vector2 point, Vector2 size, float angle, ContactFilter2D contactFilter, List<Collider2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapBox(point, size, angle, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                DebugExtensions.DebugBox(point, size, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), Quaternion.Euler(0, 0, angle),
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region OverlapBox all

        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxAll(point, size, angle, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxAll(point, size, angle, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxAll(point, size, angle, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapBoxAll(Vector2 point, Vector2 size, float angle, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D[] colliders = UEPhysics2D.OverlapBoxAll(point, size, angle, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                DebugExtensions.DebugBox(point, size, colliders != null && colliders.Length > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red),
                    Quaternion.Euler(0, 0, angle), drawDuration, preview, drawDepth);
            }

            return colliders;
        }

        #endregion

        #region OverlapBox non alloc

        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(point, size, angle, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(point, size, angle, results, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapBoxNonAlloc(point, size, angle, results, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapBoxNonAlloc(Vector2 point, Vector2 size, float angle, Collider2D[] results, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapBoxNonAlloc(point, size, angle, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                DebugExtensions.DebugBox(point, size, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), Quaternion.Euler(0, 0, angle),
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region OverlapCapsule

        #region OverlapCapsule single

        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsule(point, size, direction, angle, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsule(point, size, direction, angle, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsule(point, size, direction, angle, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D collider = UEPhysics2D.OverlapCapsule(point, size, direction, angle, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                if (direction == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                Quaternion rot = Quaternion.Euler(0, 0, angle);
                point1 = (Vector2)(rot * point1) + point;
                point2 = (Vector2)(rot * point2) + point;

                DebugExtensions.DebugOneSidedCapsule(point1, point2, collider ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, true,
                    drawDuration, preview, drawDepth);
            }

            return collider;
        }

        public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCapsule(point, size, direction, angle, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (direction == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + point;
                point2 = (Vector2)(rot * point2) + point;

                DebugExtensions.DebugOneSidedCapsule(point1, point2, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, true,
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, ContactFilter2D contactFilter, List<Collider2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCapsule(point, size, direction, angle, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (direction == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + point;
                point2 = (Vector2)(rot * point2) + point;

                DebugExtensions.DebugOneSidedCapsule(point1, point2, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, true,
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region OverlapCapsule all

        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleAll(point, size, direction, angle, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleAll(point, size, direction, angle, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleAll(point, size, direction, angle, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapCapsuleAll(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D[] colliders = UEPhysics2D.OverlapCapsuleAll(point, size, direction, angle, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (direction == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + point;
                point2 = (Vector2)(rot * point2) + point;

                DebugExtensions.DebugOneSidedCapsule(point1, point2,
                    colliders != null && colliders.Length > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, true, drawDuration, preview,
                    drawDepth);
            }

            return colliders;
        }

        #endregion

        #region OverlapCapsule non alloc

        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleNonAlloc(point, size, direction, angle, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth);
        }

        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleNonAlloc(point, size, direction, angle, results, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask, float minDepth, 
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCapsuleNonAlloc(point, size, direction, angle, results, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int OverlapCapsuleNonAlloc(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, Collider2D[] results, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCapsuleNonAlloc(point, size, direction, angle, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                size /= 2;
                Vector2 point1;
                Vector2 point2;
                float radius;

                Quaternion rot = Quaternion.Euler(0, 0, angle);

                if (direction == CapsuleDirection2D.Vertical)
                {
                    if (size.y > size.x)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.x;
                }
                else
                {
                    if (size.x > size.y)
                    {
                        point1 = new Vector3(0, 0 - size.y + size.x);
                        point2 = new Vector3(0, 0 + size.y - size.x);
                    }
                    else
                    {
                        point1 = new Vector3(0 - .01f, 0);
                        point2 = new Vector3(0 + .01f, 0);
                    }

                    radius = size.y;
                }

                point1 = (Vector2)(rot * point1) + point;
                point2 = (Vector2)(rot * point2) + point;

                DebugExtensions.DebugOneSidedCapsule(point1, point2, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, true,
                    drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region OverlapCircle

        #region OverlapCircle single

        public static Collider2D OverlapCircle(Vector2 point, float radius,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircle(point, radius, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapCircle(Vector2 point, float radius, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircle(point, radius, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapCircle(Vector2 point, float radius, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircle(point, radius, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapCircle(Vector2 point, float radius, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D collider = UEPhysics2D.OverlapCircle(point, radius, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCircle(point, Vector3.forward, collider ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration,
                    preview, drawDepth);
            }

            return collider;
        }

        public static int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCircle(point, radius, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCircle(point, Vector3.forward, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration,
                    preview, drawDepth);
            }

            return count;
        }

        public static int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, List<Collider2D> results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCircle(point, radius, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCircle(point, Vector3.forward, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration,
                    preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region OverlapCircle all

        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleAll(point, radius, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleAll(point, radius, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleAll(point, radius, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapCircleAll(Vector2 point, float radius, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D[] colliders = UEPhysics2D.OverlapCircleAll(point, radius, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCircle(point, Vector3.forward,
                    colliders != null && colliders.Length > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration, preview,
                    drawDepth);
            }

            return colliders;
        }

        #endregion

        #region OverlapCircle non alloc

        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleNonAlloc(point, radius, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleNonAlloc(point, radius, results, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapCircleNonAlloc(point, radius, results, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapCircleNonAlloc(Vector2 point, float radius, Collider2D[] results, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapCircleNonAlloc(point, radius, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugCircle(point, Vector3.forward, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), radius, drawDuration,
                    preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region OverlapPoint

        #region OverlapPoint single

        public static Collider2D OverlapPoint(Vector2 point, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPoint(point, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapPoint(Vector2 point, int layerMask, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPoint(point, layerMask, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapPoint(Vector2 point, int layerMask, float minDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPoint(point, layerMask, minDepth, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D OverlapPoint(Vector2 point, int layerMask, float minDepth, float maxDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D collider = UEPhysics2D.OverlapPoint(point, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugPoint(point, collider ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), size, drawDuration, preview, drawDepth);
            }

            return collider;
        }

        public static int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, Collider2D[] results, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapPoint(point, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugPoint(point, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), size, drawDuration, preview, drawDepth);
            }

            return count;
        }

        public static int OverlapPoint(Vector2 point, ContactFilter2D contactFilter, List<Collider2D> results, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapPoint(point, contactFilter, results);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugPoint(point, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), size, drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #region OverlapPoint all

        public static Collider2D[] OverlapPointAll(Vector2 point, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointAll(point, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointAll(point, layerMask, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask, float minDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointAll(point, layerMask, minDepth, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static Collider2D[] OverlapPointAll(Vector2 point, int layerMask, float minDepth, float maxDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            Collider2D[] colliders = UEPhysics2D.OverlapPointAll(point, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugPoint(point, colliders != null && colliders.Length > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), size,
                    drawDuration, preview, drawDepth);
            }

            return colliders;
        }

        #endregion

        #region OverlapPoint non alloc

        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointNonAlloc(point, results, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointNonAlloc(point, results, layerMask, -M_maxDistance, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float minDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return OverlapPointNonAlloc(point, results, layerMask, minDepth, M_maxDistance, size, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int OverlapPointNonAlloc(Vector2 point, Collider2D[] results, int layerMask, float minDepth, float maxDepth, float size = 6,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int count = UEPhysics2D.OverlapPointNonAlloc(point, results, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                DebugExtensions.DebugPoint(point, count > 0 ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), size, drawDuration, preview, drawDepth);
            }

            return count;
        }

        #endregion

        #endregion

        #region RayCast

        #region RayCast single

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit2D hitInfo = UEPhysics2D.Raycast(origin, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (distance == M_maxDistance ? 1000 * 1000 : distance);
                bool collided = false;

                if (hitInfo.collider != null)
                {
                    collided = true;
                    end = hitInfo.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return hitInfo;
        }
        
        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return Raycast(origin, direction, contactFilter, results, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.Raycast(origin, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (distance == M_maxDistance ? 1000 * 1000 : distance);
                bool collided = false;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    end = hit.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return size;
        }

        public static int Raycast(Vector2 origin, Vector2 direction, ContactFilter2D contactFilter, List<RaycastHit2D> results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.Raycast(origin, direction, contactFilter, results, distance);

            if (preview != PreviewCondition.None)
            {
                Vector3 end = origin + direction * (distance == M_maxDistance ? 1000 * 1000 : distance);
                bool collided = false;

                for (int i = 0; i < size; i++)
                {
                    RaycastHit2D hit = results[i];
                    collided = true;
                    end = hit.point;

                    DebugExtensions.DebugPoint(end, Color.red, 0.5f, drawDuration, preview, drawDepth);
                }

                DebugExtensions.DrawLine(origin, end, collided ? (hitColor ?? Color.green) : (noHitColor ?? Color.red), drawDuration, preview, drawDepth);
            }

            return size;
        }

        #endregion

        #region RayCast all

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastAll(origin, direction, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor, drawDepth);
        }

        public static RaycastHit2D[] RaycastAll(Vector2 origin, Vector2 direction, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            RaycastHit2D[] raycastInfo = UEPhysics2D.RaycastAll(origin, direction, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                Vector2 previewOrigin = origin;
                Vector2 sectionOrigin = origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                foreach (RaycastHit2D hit in raycastInfo)
                {
                    collided = true;
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);

                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, origin + direction * distance, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return raycastInfo;
        }

        #endregion

        #region RayCast non alloc

        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, M_maxDistance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration,
                hitColor, noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, distance, UEPhysics2D.AllLayers, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor,
                noHitColor, drawDepth);
        }

        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, distance, layerMask, -M_maxDistance, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            return RaycastNonAlloc(origin, direction, results, distance, layerMask, minDepth, M_maxDistance, preview, drawDuration, hitColor, noHitColor,
                drawDepth);
        }

        public static int RaycastNonAlloc(Vector2 origin, Vector2 direction, RaycastHit2D[] results, float distance, int layerMask, float minDepth, float maxDepth,
            PreviewCondition preview = PreviewCondition.None, float drawDuration = 0, Color? hitColor = null, Color? noHitColor = null, bool drawDepth = false)
        {
            int size = UEPhysics2D.RaycastNonAlloc(origin, direction, results, distance, layerMask, minDepth, maxDepth);

            if (preview != PreviewCondition.None)
            {
                bool collided = false;
                Vector2 previewOrigin = origin;
                Vector2 sectionOrigin = origin;
                hitColor ??= Color.green;
                noHitColor ??= Color.red;

                for (int i = 0; i < size; i++)
                {
                    collided = true;
                    RaycastHit2D hit = results[i];
                    DebugExtensions.DebugPoint(hit.point, Color.red, 0.5f, drawDuration, preview, drawDepth);
                    DebugExtensions.DrawLine(sectionOrigin, hit.point, hitColor.Value, drawDuration, preview, drawDepth);

                    if ((origin - hit.point).sqrMagnitude > (origin - previewOrigin).sqrMagnitude)
                        previewOrigin = hit.point;

                    sectionOrigin = hit.point;
                }

                DebugExtensions.DrawLine(previewOrigin, origin + direction * distance, collided ? hitColor.Value : noHitColor.Value, drawDuration, preview, drawDepth);
            }

            return size;
        }

        #endregion

        #endregion

        #endregion
    }
}