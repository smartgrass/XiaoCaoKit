using UnityEngine;

using MFPC.Utils;

namespace MFPC.Input.PlayerInput
{
    public class PlayerInputTuner : MonoBehaviour
    {        
        [Utils.CenterHeader("Input Data")] 
        [SerializeField] private MobilePlayerInputHandler _mobilePlayerInputHandler;
        
        [field: SerializeField] 
        public SensitiveData SensitiveData { get; private set; }
        public IPlayerInput CurrentPlayerInputHandler { get => _proxyPlayerInputHandler; } 
        public bool IsLockInput { set => _proxyPlayerInputHandler.SetLockInput(value); }
        
        private ProxyPlayerInputHandler _proxyPlayerInputHandler;
        private ReactiveProperty<InputType> _currentInputType;

        public void Initialize(ReactiveProperty<InputType> currentInputType)
        {
            _proxyPlayerInputHandler = new ProxyPlayerInputHandler(SensitiveData);
            _currentInputType = currentInputType;
            _currentInputType.Subscribe(ChangeInputHandler);
            
            ChangeInputHandler(_currentInputType.Value);
        }
        
        private void OnDestroy() => _currentInputType.Unsubscribe(ChangeInputHandler);

        private void ChangeInputHandler(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.Mobile:
                    _proxyPlayerInputHandler.SetPlayerInputHandler(_mobilePlayerInputHandler);
                    _mobilePlayerInputHandler.gameObject.SetActive(true);
                    Cursor.lockState = CursorLockMode.None;
                    break;

                case InputType.KeyboardMouse:
                    _proxyPlayerInputHandler.SetPlayerInputHandler(GetCurrentPlayerInputHandler(inputType));
                    _mobilePlayerInputHandler.gameObject.SetActive(false);
                    Cursor.lockState = CursorLockMode.Locked;
                    break;
            }
        }

        private T GetPlayerInputHandler<T>() where T : PlayerInputHandler
        {
            if (!TryGetComponent(out T playerInputHandler))
            {
                playerInputHandler = gameObject.AddComponent<T>();
            }

            return playerInputHandler;
        }
        
        private PlayerInputHandler GetCurrentPlayerInputHandler(InputType inputType)
        {
            OldPlayerInputHandler playerInputHandler = GetPlayerInputHandler<OldPlayerInputHandler>();

            return playerInputHandler;
        } 
    }
}