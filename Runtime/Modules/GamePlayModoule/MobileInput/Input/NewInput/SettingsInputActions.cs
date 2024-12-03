// GENERATED AUTOMATICALLY FROM 'Assets/MFPC/Scripts/Input/NewInput/SettingsInputActions.inputactions'

#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace MFPC.Input
{
    public class @SettingsInputActions : IInputActionCollection, IDisposable
    {
        public InputActionAsset asset { get; }
        public @SettingsInputActions()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""SettingsInputActions"",
    ""maps"": [
        {
            ""name"": ""Settings"",
            ""id"": ""344aebd3-415e-416e-a938-ad97548b4859"",
            ""actions"": [
                {
                    ""name"": ""Open"",
                    ""type"": ""Button"",
                    ""id"": ""16c2e2e1-895b-4b09-b58d-69daf7d38861"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""ea4ce14b-51f5-4b0e-8868-eeb98f6f85b6"",
                    ""path"": ""<Keyboard>/tab"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Open"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""85856994-4da4-431b-b46c-97c663075d5c"",
                    ""path"": ""<Gamepad>/start"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Open"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // Settings
            m_Settings = asset.FindActionMap("Settings", throwIfNotFound: true);
            m_Settings_Open = m_Settings.FindAction("Open", throwIfNotFound: true);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Enable()
        {
            asset.Enable();
        }

        public void Disable()
        {
            asset.Disable();
        }

        // Settings
        private readonly InputActionMap m_Settings;
        private ISettingsActions m_SettingsActionsCallbackInterface;
        private readonly InputAction m_Settings_Open;
        public struct SettingsActions
        {
            private @SettingsInputActions m_Wrapper;
            public SettingsActions(@SettingsInputActions wrapper) { m_Wrapper = wrapper; }
            public InputAction @Open => m_Wrapper.m_Settings_Open;
            public InputActionMap Get() { return m_Wrapper.m_Settings; }
            public void Enable() { Get().Enable(); }
            public void Disable() { Get().Disable(); }
            public bool enabled => Get().enabled;
            public static implicit operator InputActionMap(SettingsActions set) { return set.Get(); }
            public void SetCallbacks(ISettingsActions instance)
            {
                if (m_Wrapper.m_SettingsActionsCallbackInterface != null)
                {
                    @Open.started -= m_Wrapper.m_SettingsActionsCallbackInterface.OnOpen;
                    @Open.performed -= m_Wrapper.m_SettingsActionsCallbackInterface.OnOpen;
                    @Open.canceled -= m_Wrapper.m_SettingsActionsCallbackInterface.OnOpen;
                }
                m_Wrapper.m_SettingsActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @Open.started += instance.OnOpen;
                    @Open.performed += instance.OnOpen;
                    @Open.canceled += instance.OnOpen;
                }
            }
        }
        public SettingsActions @Settings => new SettingsActions(this);
        public interface ISettingsActions
        {
            void OnOpen(InputAction.CallbackContext context);
        }
    }
}
#endif