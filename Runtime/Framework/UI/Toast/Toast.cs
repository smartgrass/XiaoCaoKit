using UnityEngine;
using EasyUI.Helpers;

/* -------------------------------
   Created by : Hamza Herbou
   hamza95herbou@gmail.com
---------------------------------- */

namespace EasyUI.Toast
{
    public static class Toast
    {
        public static bool IsLoaded = false;

        private static ToastUI _toastUI;

        private static void Prepare()
        {
            if (IsLoaded)
            {
                return;
            }

            GameObject instance = MonoBehaviour.Instantiate(Resources.Load<GameObject>("ToastUI"));
            instance.name = "[ TOAST UI ]";
            _toastUI = instance.GetComponent<ToastUI>();
            IsLoaded = true;
        }


        public static void Show(string text, float duration = 1.5f, TextAnchor alignment = TextAnchor.UpperCenter,
            EToastType type = EToastType.Normal)
        {
            Prepare();
            _toastUI.Show(text, duration, alignment, type);
        }

        public static void Dismiss()
        {
            _toastUI.Dismiss();
        }
    }
}