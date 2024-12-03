using System;
using UnityEngine;

namespace MFPC.Input
{
    [System.Serializable]
    public class SensitiveData
    {
        public const float MaxSensitivity = 20f;
        public const float MaxRotateSpeedSmooth = 30f;

        /// <summary>
        /// Camera rotation force
        /// </summary>
        [field: SerializeField, Range(0, MaxSensitivity)]
        public float Sensitivity { get; private set; } = 1f;

        /// <summary>
        /// Smoothing the force of camera movement in Y
        /// </summary>
        [field: SerializeField, Range(0.0f, MaxRotateSpeedSmooth)]
        public float RotateSpeedSmoothVertical { get; private set; } = 10f;

        /// <summary>
        /// Smoothing the force of camera movement in X
        /// </summary>
        [field: SerializeField, Range(0.0f, MaxRotateSpeedSmooth)]
        public float RotateSpeedSmoothHorizontal { get; private set; } = 10f;

        public void SetSensitivity(float sensitivity)
        {
            if (InRange(sensitivity, 0, MaxSensitivity))
            {
                Sensitivity = sensitivity;
            }
        }

        public void SetRotateSpeedSmoothVertical(float rotateSpeedSmoothVertical)
        {
            if (InRange(rotateSpeedSmoothVertical, 0, MaxRotateSpeedSmooth))
            {
                RotateSpeedSmoothVertical = rotateSpeedSmoothVertical;
            }
        }

        public void SetRotateSpeedSmoothHorizontal(float rotateSpeedSmoothHorizontal)
        {
            if (InRange(rotateSpeedSmoothHorizontal, 0, MaxRotateSpeedSmooth))
            {
                RotateSpeedSmoothHorizontal = rotateSpeedSmoothHorizontal;
            }
        }

        private bool InRange(float x, float min, float max)
        {
            if (x >= min && x <= max)
            {
                return true;
            }

            throw new Exception("Value out of range");
        }
    }
}