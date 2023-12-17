using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XiaoCao
{
    internal class CameraController : MonoBehaviour
    {
        #region static
        public static CameraController instance;

        public static Camera Main
        {
            get
            {
                if (!_main)
                {
                    _main = Camera.main;
                }
                return _main;
            }
        }

        private static Camera _main;

        public static Vector3 Forword { get => Main.transform.forward; }

        public static void ReFindCamera()
        {
            _main = Camera.main;
        }
        #endregion




        //private CinemachineVirtualCamera VirtualCamera;

    }
}


