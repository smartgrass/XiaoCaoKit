using System;
using UnityEngine;
using UnityEngine.UI;

namespace MFPC.Input.SettingsInput
{
    [RequireComponent(typeof(Button))]
    public class MobileSettingsInputHandler : MonoBehaviour, ISettingsInput
    {
        public event Action OnOpenSettings;

        private Button _openSettingsButton;
        private GameObject _settingsField;

        #region MONO

        private void Awake() => _openSettingsButton = this.GetComponent<Button>();
        private void OnEnable() => _openSettingsButton.onClick.AddListener(OpenSettings);
        private void OnDisable() => _openSettingsButton.onClick.RemoveListener(OpenSettings);

        #endregion

        public void SetSettingsField(GameObject settingsField)
        {
            _settingsField = settingsField;
        }

        private void OpenSettings()
        {
            OnOpenSettings?.Invoke();
            UpdateUIInput();
        }

        public void UpdateUIInput()
        {
            if (_settingsField.activeSelf) Cursor.lockState = CursorLockMode.None;
        }
    }
}