using RotaryHeart.Lib.UnityGLDebug;
using UnityEngine;

namespace RotaryHeart.Lib.PhysicsExtension
{
    /// <summary>
    /// Class used to draw additional debugs, this was based of the Debug Drawing Extension from the asset store (https://www.assetstore.unity3d.com/en/#!/content/11396)
    /// </summary>
    public static partial class DebugExtensions
    {
        public static void DebugSquare(Vector3 origin, Vector3 halfExtents, Color color, Quaternion orientation,
            float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 forward = orientation * Vector3.forward;
            Vector3 up = orientation * Vector3.up;
            Vector3 right = orientation * Vector3.right;

            Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

            DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
            DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
        }

        public static void DebugBox(Vector3 origin, Vector3 halfExtents, Vector3 direction, float maxDistance, Color color,
            Quaternion orientation, Color endColor, bool drawBase = true, float drawDuration = 0,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 end = origin + direction * (float.IsPositiveInfinity(maxDistance) ? 1000 * 1000 : maxDistance);

            Vector3 forward = orientation * Vector3.forward;
            Vector3 up = orientation * Vector3.up;
            Vector3 right = orientation * Vector3.right;

            #region Coords

            #region End coords

            Vector3 topMinX0 = end + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMaxX0 = end - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMinY0 = end + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 topMaxY0 = end - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

            Vector3 botMinX0 = end + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMaxX0 = end - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMinY0 = end + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 botMaxY0 = end - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

            #endregion

            #region Origin coords

            Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

            Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

            #endregion

            #endregion

            #region Draw lines

            #region Origin box

            if (drawBase)
            {
                DrawLine(topMinX1, botMinX1, color, drawDuration, preview, drawDepth);
                DrawLine(topMaxX1, botMaxX1, color, drawDuration, preview, drawDepth);
                DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
                DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);

                DrawLine(topMinX1, topMaxX1, color, drawDuration, preview, drawDepth);
                DrawLine(topMinX1, topMinY1, color, drawDuration, preview, drawDepth);
                DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
                DrawLine(topMaxY1, topMaxX1, color, drawDuration, preview, drawDepth);

                DrawLine(botMinX1, botMaxX1, color, drawDuration, preview, drawDepth);
                DrawLine(botMinX1, botMinY1, color, drawDuration, preview, drawDepth);
                DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
                DrawLine(botMaxY1, botMaxX1, color, drawDuration, preview, drawDepth);
            }

            #endregion

            #region Connection between boxes

            DrawLine(topMinX0, topMinX1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxX0, topMaxX1, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY0, topMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY0, topMaxY1, color, drawDuration, preview, drawDepth);

            DrawLine(botMinX0, botMinX1, color, drawDuration, preview, drawDepth);
            DrawLine(botMinX0, botMinX1, color, drawDuration, preview, drawDepth);
            DrawLine(botMinY0, botMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(botMaxY0, botMaxY1, color, drawDuration, preview, drawDepth);

            #endregion

            #region End box

            color = endColor;

            DrawLine(topMinX0, botMinX0, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxX0, botMaxX0, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY0, botMinY0, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY0, botMaxY0, color, drawDuration, preview, drawDepth);

            DrawLine(topMinX0, topMaxX0, color, drawDuration, preview, drawDepth);
            DrawLine(topMinX0, topMinY0, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY0, topMaxY0, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY0, topMaxX0, color, drawDuration, preview, drawDepth);

            DrawLine(botMinX0, botMaxX0, color, drawDuration, preview, drawDepth);
            DrawLine(botMinX0, botMinY0, color, drawDuration, preview, drawDepth);
            DrawLine(botMinY0, botMaxY0, color, drawDuration, preview, drawDepth);
            DrawLine(botMaxY0, botMaxX0, color, drawDuration, preview, drawDepth);

            #endregion

            #endregion
        }

        public static void DebugBoxCast(Vector3 origin, Vector3 halfExtents, Vector3 direction, float maxDistance, Color color, Quaternion orientation,
            float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            if (drawType == CastDrawType.Minimal)
            {
                DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }
            else
            {
                Vector3 forward = orientation * Vector3.forward;
                Vector3 up = orientation * Vector3.up;
                Vector3 right = orientation * Vector3.right;

                Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
                Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
                Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
                Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

                Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
                Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
                Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
                Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

                DrawLine(topMinX1, topMinX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(topMaxX1, topMaxX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(topMinY1, topMinY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(topMaxY1, topMaxY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(botMinX1, botMinX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(botMaxX1, botMaxX1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(botMinY1, botMinY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(botMaxY1, botMaxY1 + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }

            DebugBox(origin, halfExtents, Physics.M_castColor, orientation, drawDuration, preview, drawDepth);
            DebugBox(origin + direction * maxDistance, halfExtents, color, orientation, drawDuration, preview, drawDepth);
        }

        public static void DebugBox(Vector3 origin, Vector3 halfExtents, Color color, Quaternion orientation,
            float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 forward = orientation * Vector3.forward;
            Vector3 up = orientation * Vector3.up;
            Vector3 right = orientation * Vector3.right;

            Vector3 topMinX1 = origin + (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMaxX1 = origin - (right * halfExtents.x) + (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 topMinY1 = origin + (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 topMaxY1 = origin - (right * halfExtents.x) + (up * halfExtents.y) + (forward * halfExtents.z);

            Vector3 botMinX1 = origin + (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMaxX1 = origin - (right * halfExtents.x) - (up * halfExtents.y) - (forward * halfExtents.z);
            Vector3 botMinY1 = origin + (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);
            Vector3 botMaxY1 = origin - (right * halfExtents.x) - (up * halfExtents.y) + (forward * halfExtents.z);

            DrawLine(topMinX1, botMinX1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxX1, botMaxX1, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY1, botMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY1, botMaxY1, color, drawDuration, preview, drawDepth);

            DrawLine(topMinX1, topMaxX1, color, drawDuration, preview, drawDepth);
            DrawLine(topMinX1, topMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMinY1, topMaxY1, color, drawDuration, preview, drawDepth);
            DrawLine(topMaxY1, topMaxX1, color, drawDuration, preview, drawDepth);

            DrawLine(botMinX1, botMaxX1, color, drawDuration, preview, drawDepth);
            DrawLine(botMinX1, botMinY1, color, drawDuration, preview, drawDepth);
            DrawLine(botMinY1, botMaxY1, color, drawDuration, preview, drawDepth);
            DrawLine(botMaxY1, botMaxX1, color, drawDuration, preview, drawDepth);
        }

        public static void DebugOneSidedCapsuleCast(Vector3 baseSphere, Vector3 endSphere, Vector3 direction, float maxDistance,
            Color color, float radius = 1.0f, float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 midPoint = (baseSphere + endSphere) / 2f;

            DebugOneSidedCapsule(baseSphere, endSphere, Physics.M_castColor, radius, true, drawDuration, preview, drawDepth);

            if (drawType == CastDrawType.Minimal)
            {
                DrawLine(midPoint, midPoint + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }
            else
            {
                Vector3 up = (endSphere - baseSphere).normalized;
                if (up == Vector3.zero)
                {
                    up = Vector3.up;
                }
                Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
                Vector3 right = Vector3.Cross(up, forward).normalized;

                DrawLine(baseSphere + right * radius, baseSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere + right * radius, endSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(baseSphere - right * radius, baseSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere - right * radius, endSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(endSphere + up * radius, endSphere + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(baseSphere - up * radius, baseSphere - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }

            DebugOneSidedCapsule(baseSphere + direction * maxDistance, endSphere + direction * maxDistance, color, radius, true, drawDuration, preview,
                drawDepth);
        }

        public static void DebugOneSidedCapsule(Vector3 baseSphere, Vector3 endSphere, Color color, float radius = 1,
            bool colorizeBase = false, float drawDuration = 0,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 up = (endSphere - baseSphere).normalized * radius;
            if (up == Vector3.zero)
            {
                up = Vector3.up;
            }
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            //Side lines
            DrawLine(baseSphere + right, endSphere + right, color, drawDuration, preview, drawDepth);
            DrawLine(baseSphere - right, endSphere - right, color, drawDuration, preview, drawDepth);

            //Draw end caps
            for (int i = 1; i < 26; i++)
            {
                //Start endcap
                DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);

                //End endcap
                DrawLine(Vector3.Slerp(right, up, i / 25.0f) + endSphere, Vector3.Slerp(right, up, (i - 1) / 25.0f) + endSphere, color, drawDuration, preview,
                    drawDepth);
                DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + endSphere, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + endSphere, color,
                    drawDuration, preview, drawDepth);
            }
        }

        public static void DebugCapsuleCast(Vector3 baseSphere, Vector3 endSphere, Vector3 direction, float maxDistance,
            Color color, float radius = 1.0f, float drawDuration = 0, CastDrawType drawType = CastDrawType.Minimal,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 midPoint = (baseSphere + endSphere) / 2;

            DebugCapsule(baseSphere, endSphere, Physics.M_castColor, radius, true, drawDuration, preview, drawDepth);

            if (drawType == CastDrawType.Minimal)
            {
                DrawLine(midPoint, midPoint + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }
            else
            {
                Vector3 up = (endSphere - baseSphere).normalized;
                if (up == Vector3.zero)
                {
                    up = Vector3.up;
                }
                Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
                Vector3 right = Vector3.Cross(up, forward).normalized;

                DrawLine(baseSphere + right * radius, baseSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere + right * radius, endSphere + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(baseSphere - right * radius, baseSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere - right * radius, endSphere - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(baseSphere + forward * radius, baseSphere + forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere + forward * radius, endSphere + forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(baseSphere - forward * radius, baseSphere - forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(endSphere - forward * radius, endSphere - forward * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);

                DrawLine(endSphere + up * radius, endSphere + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(baseSphere - up * radius, baseSphere - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }

            DebugCapsule(baseSphere + direction * maxDistance, endSphere + direction * maxDistance, color, radius, true, drawDuration, preview, drawDepth);
        }

        public static void DebugCapsule(Vector3 baseSphere, Vector3 endSphere, Color color, float radius = 1,
            bool colorizeBase = true, float drawDuration = 0,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 up = (endSphere - baseSphere).normalized * radius;
            if (up == Vector3.zero)
            {
                up = Vector3.up;
            }
            Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
            Vector3 right = Vector3.Cross(up, forward).normalized * radius;

            //Radial circles
            DebugCircle(baseSphere, up, colorizeBase ? color : Color.red, radius, drawDuration, preview, drawDepth);
            DebugCircle(endSphere, -up, color, radius, drawDuration, preview, drawDepth);

            //Side lines
            DrawLine(baseSphere + right, endSphere + right, color, drawDuration, preview, drawDepth);
            DrawLine(baseSphere - right, endSphere - right, color, drawDuration, preview, drawDepth);

            DrawLine(baseSphere + forward, endSphere + forward, color, drawDuration, preview, drawDepth);
            DrawLine(baseSphere - forward, endSphere - forward, color, drawDuration, preview, drawDepth);

            //Draw end caps
            for (int i = 1; i < 26; i++)
            {
                //End endcap
                DrawLine(Vector3.Slerp(right, up, i / 25.0f) + endSphere, Vector3.Slerp(right, up, (i - 1) / 25.0f) + endSphere, color, drawDuration, preview,
                    drawDepth);
                DrawLine(Vector3.Slerp(-right, up, i / 25.0f) + endSphere, Vector3.Slerp(-right, up, (i - 1) / 25.0f) + endSphere, color,
                    drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(forward, up, i / 25.0f) + endSphere, Vector3.Slerp(forward, up, (i - 1) / 25.0f) + endSphere, color,
                    drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(-forward, up, i / 25.0f) + endSphere, Vector3.Slerp(-forward, up, (i - 1) / 25.0f) + endSphere, color,
                    drawDuration, preview, drawDepth);

                //Start endcap
                DrawLine(Vector3.Slerp(right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(right, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(-right, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-right, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(forward, -up, i / 25.0f) + baseSphere, Vector3.Slerp(forward, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
                DrawLine(Vector3.Slerp(-forward, -up, i / 25.0f) + baseSphere, Vector3.Slerp(-forward, -up, (i - 1) / 25.0f) + baseSphere,
                    colorizeBase ? color : Color.red, drawDuration, preview, drawDepth);
            }
        }

        public static void DebugCircleCast(Vector3 origin, Vector3 direction, float maxDistance, Color color, float radius, float drawDuration,
            CastDrawType drawType, PreviewCondition preview, bool drawDepth)
        {
            DebugCircle(origin, Vector3.forward, Physics.M_castColor, radius, drawDuration, preview, drawDepth);

            if (drawType == CastDrawType.Minimal)
            {
                DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }
            else
            {
                Vector3 up = origin.normalized * radius;
                if (up == Vector3.zero)
                {
                    up = Vector3.up;
                }
                Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
                Vector3 right = Vector3.Cross(up, forward).normalized * radius;

                DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin - right * radius, origin - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin + up * radius, origin + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin - up * radius, origin - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }

            DebugCircle(origin + direction * maxDistance, Vector3.forward, color, radius, drawDuration, preview, drawDepth);
        }

        public static void DebugCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f,
            float drawDuration = 0, PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            Vector3 upDir = up.normalized * radius;
            Vector3 forwardDir = Vector3.Slerp(upDir, -upDir, 0.5f);
            Vector3 rightDir = Vector3.Cross(upDir, forwardDir).normalized * radius;

            Matrix4x4 matrix = new Matrix4x4()
            {
                [0] = rightDir.x,
                [1] = rightDir.y,
                [2] = rightDir.z,

                [4] = upDir.x,
                [5] = upDir.y,
                [6] = upDir.z,

                [8] = forwardDir.x,
                [9] = forwardDir.y,
                [10] = forwardDir.z
            };

            Vector3 lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
            Vector3 nextPoint = Vector3.zero;

            color = (color == default(Color)) ? Color.white : color;

            for (var i = 0; i < 91; i++)
            {
                nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
                nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
                nextPoint.y = 0;

                nextPoint = position + matrix.MultiplyPoint3x4(nextPoint);

                DrawLine(lastPoint, nextPoint, color, drawDuration, preview, drawDepth);

                lastPoint = nextPoint;
            }
        }

        public static void DebugPoint(Vector3 position, Color color, float scale = 0.5f, float drawDuration = 0,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            color = (color == default(Color)) ? Color.white : color;

            DrawLine(position + (Vector3.up * (scale * 0.5f)), position - Vector3.up * scale, color, drawDuration, preview, drawDepth);
            DrawLine(position + (Vector3.right * (scale * 0.5f)), position - Vector3.right * scale, color, drawDuration, preview, drawDepth);
            DrawLine(position + (Vector3.forward * (scale * 0.5f)), position - Vector3.forward * scale, color, drawDuration, preview, drawDepth);
        }

        public static void DebugSphereCast(Vector3 origin, Vector3 direction, float maxDistance, Color color, float radius, float drawDuration,
            CastDrawType drawType, PreviewCondition preview, bool drawDepth)
        {
            DebugWireSphere(origin, Physics.M_castColor, radius, drawDuration, preview, drawDepth);

            if (drawType == CastDrawType.Minimal)
            {
                DrawLine(origin, origin + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }
            else
            {
                Vector3 up = origin.normalized * radius;
                if (up == Vector3.zero)
                {
                    up = Vector3.up;
                }
                Vector3 forward = Vector3.Slerp(up, -up, 0.5f);
                Vector3 right = Vector3.Cross(up, forward).normalized * radius;

                DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin - right * radius, origin - right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin + right * radius, origin + right * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin + up * radius, origin + up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
                DrawLine(origin - up * radius, origin - up * radius + direction * maxDistance, color, drawDuration, preview, drawDepth);
            }

            DebugWireSphere(origin + direction * maxDistance, color, radius, drawDuration, preview, drawDepth);
        }

        public static void DebugWireSphere(Vector3 position, Color color, float radius = 1.0f, float drawDuration = 0,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            float angle = 10.0f;

            Vector3 x = new Vector3(position.x, position.y + radius * Mathf.Sin(0), position.z + radius * Mathf.Cos(0));
            Vector3 y = new Vector3(position.x + radius * Mathf.Cos(0), position.y, position.z + radius * Mathf.Sin(0));
            Vector3 z = new Vector3(position.x + radius * Mathf.Cos(0), position.y + radius * Mathf.Sin(0), position.z);

            for (int i = 1; i < 37; i++)
            {
                Vector3 new_x = new Vector3(position.x, position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad),
                    position.z + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad));
                Vector3 new_y = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad), position.y,
                    position.z + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad));
                Vector3 new_z = new Vector3(position.x + radius * Mathf.Cos(angle * i * Mathf.Deg2Rad),
                    position.y + radius * Mathf.Sin(angle * i * Mathf.Deg2Rad), position.z);

                DrawLine(x, new_x, color, drawDuration, preview, drawDepth);
                DrawLine(y, new_y, color, drawDuration, preview, drawDepth);
                DrawLine(z, new_z, color, drawDuration, preview, drawDepth);

                x = new_x;
                y = new_y;
                z = new_z;
            }
        }

        private static void GetDrawStates(PreviewCondition preview, out bool drawEditor, out bool drawGame)
        {
            drawEditor = false;
            drawGame = false;

            switch (preview)
            {
                case PreviewCondition.Editor:
                    drawEditor = true;
                    break;

                case PreviewCondition.Game:
                    drawGame = true;
                    break;

                case PreviewCondition.Both:
                    drawEditor = true;
                    drawGame = true;
                    break;
            }
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color? color = null, float duration = 0f,
            PreviewCondition preview = PreviewCondition.Editor, bool drawDepth = false)
        {
            GetDrawStates(preview, out bool drawEditor, out bool drawGame);

            if (drawEditor)
            {
                Debug.DrawLine(start, end, color ?? Color.white, duration, drawDepth);
            }

            if (drawGame)
            {
                GLDebug.DrawLine(start, end, color ?? Color.white, duration, drawDepth);
            }
        }
    }
}