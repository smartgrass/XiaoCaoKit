using UnityEngine;

namespace MFPC.Utils
{
    public static class RotateHelper
    {
        public static Quaternion SmoothRotateHorizontal(Quaternion currentRotate, float rotateSpeedSmooth,
            float lookDirection,
            float offset = 0)
        {
            return (rotateSpeedSmooth > 0.0f)
                ? Quaternion.Slerp(currentRotate,
                    Quaternion.Euler(currentRotate.eulerAngles.x, lookDirection + offset, currentRotate.eulerAngles.z),
                    Time.deltaTime * rotateSpeedSmooth)
                : Quaternion.Euler(currentRotate.eulerAngles.x, lookDirection + offset, currentRotate.eulerAngles.z);
        }

        public static Quaternion SmoothRotateVertical(Quaternion currentRotate, float rotateSpeedSmooth,
            float lookDirection,
            float offset = 0)
        {
            return (rotateSpeedSmooth > 0.0f)
                ? Quaternion.Slerp(currentRotate,
                    Quaternion.Euler(lookDirection + offset, 0.0f, currentRotate.eulerAngles.z),
                    Time.deltaTime * rotateSpeedSmooth)
                : Quaternion.Euler(lookDirection + offset, 0.0f, currentRotate.eulerAngles.z);
        }
    }
}