using UnityEngine;

using MFPC.Utils;

namespace MFPC.Input.SettingsInput
{
    public class SettingsInputTuner : MonoBehaviour
    {
        public ISettingsInput CurrentSettingsInput { get => _proxySettingsInputHandler; }

        [Utils.CenterHeader("Input Data")]
        [SerializeField] private GameObject firstSelectedObjectInSettings;
        [SerializeField] private MobileSettingsInputHandler mobileSettingsInputHandler;
        
        private ProxySettingsInputHandler _proxySettingsInputHandler = new ProxySettingsInputHandler();
        private ReactiveProperty<InputType> _currentInputType;
        private GameObject _settingsField;
        
        public void Initialize(ReactiveProperty<InputType> currentInputType, GameObject settingsField)
        {
            _settingsField = settingsField;
            _currentInputType = currentInputType;
            _currentInputType.Subscribe(OnInputChanged);

            ChangeInputHandler(currentInputType.Value);
        }
        
        private void OnDestroy() => _currentInputType.Unsubscribe(OnInputChanged);

        private void ChangeInputHandler(InputType inputType)
        {
            mobileSettingsInputHandler.gameObject.SetActive(false);

            switch (inputType)
            {
                case InputType.Mobile:
                    _proxySettingsInputHandler.SetSettingsInputHandler(mobileSettingsInputHandler);
                    mobileSettingsInputHandler.gameObject.SetActive(true);
                    break;
                case InputType.KeyboardMouse:
                    _proxySettingsInputHandler.SetSettingsInputHandler(GetKeyboardMouseInput());
                    break;
            }

            _proxySettingsInputHandler.SetSettingsField(_settingsField);
        }

        private ISettingsInput GetKeyboardMouseInput()
        {
            if (!this.TryGetComponent(out OldSettingsInputHandler oldSettingsInputHandler))
            {
                oldSettingsInputHandler = gameObject.AddComponent<OldSettingsInputHandler>();
            }

            return oldSettingsInputHandler;
        }
        
        private void OnInputChanged(InputType inputType)
        {
            ChangeInputHandler(inputType);
            CurrentSettingsInput.UpdateUIInput();
        }
    }
}