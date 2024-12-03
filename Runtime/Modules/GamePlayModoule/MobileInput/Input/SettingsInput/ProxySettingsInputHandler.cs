using System;
using UnityEngine;

namespace MFPC.Input.SettingsInput
{
    public class ProxySettingsInputHandler : ISettingsInput
    {
        public event Action OnOpenSettings;

        private ISettingsInput currentSettingsInput;

        public void UpdateUIInput() => currentSettingsInput.UpdateUIInput();

        public void SetSettingsField(GameObject settingsField) => currentSettingsInput.SetSettingsField(settingsField);

        public void SetSettingsInputHandler(ISettingsInput settingsInput)
        {
            if(settingsInput == null) return;

            if (currentSettingsInput != null) currentSettingsInput.OnOpenSettings -= OpenSettingsAction;

            currentSettingsInput = settingsInput;

            currentSettingsInput.OnOpenSettings += OpenSettingsAction;
        }

        public void OpenSettingsAction() => OnOpenSettings?.Invoke();
    }
}