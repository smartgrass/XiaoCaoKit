using System.ComponentModel.Design;
using UnityEngine;

namespace MFPC.Input
{
    public enum InputType
    {
        Mobile,
        KeyboardMouse
    }

    [CreateAssetMenu(fileName = "InputConfig", menuName = "MFPC/InputConfig", order = 0)]
    public class InputConfig : ScriptableObject
    {
        [SerializeField] private InputType initialInputType;
        [SerializeField] private bool adaptationInput;

        public InputType GetCurrentInputType()
        {
            if (adaptationInput)
            {
                if (Application.isMobilePlatform) return InputType.Mobile;
                return InputType.KeyboardMouse;
            }

            return initialInputType;
        }
    }
}