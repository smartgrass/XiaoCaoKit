using UnityEngine;

namespace XiaoCao
{
    public class HomeMapControl : MonoBehaviour
    {
        public Transform startPoint;

        public Vector3 GetStartPos()
        {
            if (startPoint)
            {
                return startPoint.position;
            }

            return Vector3.zero;
        }
    }
}