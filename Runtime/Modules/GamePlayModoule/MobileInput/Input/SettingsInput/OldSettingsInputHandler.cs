using System;
using UnityEngine;

namespace MFPC.Input.SettingsInput
{
    public class OldSettingsInputHandler : MonoBehaviour, ISettingsInput
    {
        public event Action OnOpenSettings;

        private GameObject _settingsField;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                OnOpenSettings?.Invoke();

                if (OnOpenSettings != null) UpdateUIInput();
            }
        }

        public void SetSettingsField(GameObject settingsField)
        {
            _settingsField = settingsField;
        }

        public void UpdateUIInput()
        {
            Cursor.lockState = (_settingsField.activeSelf)
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }
}