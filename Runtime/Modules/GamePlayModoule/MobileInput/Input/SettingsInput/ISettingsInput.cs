using System;
using UnityEngine;

namespace MFPC.Input.SettingsInput
{
    public interface ISettingsInput
    {
        event Action OnOpenSettings;

        void SetSettingsField(GameObject settingsField);

        void UpdateUIInput();
    }
}