using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MFPC.Input;
using MFPC.Utils;
using MFPC.Utils.SaveLoad;

namespace MFPC
{
    [System.Serializable]
    public class InputTypeSetup
    {
        [SerializeField] private Dropdown inputTypeDropdown;
        [SerializeField] private bool isSaveInputType;

        private ReactiveProperty<InputType> _currentInputType;
        private ISaver _saver;

        public void Initialize(ReactiveProperty<InputType> currentInputType)
        {
            _saver = new PlayerPrefsSaver("InputTypeSetup");
            _currentInputType = currentInputType;

            FillDropdown(inputTypeDropdown);
            Load();

            inputTypeDropdown.value = (int) _currentInputType.Value;
            inputTypeDropdown.onValueChanged.AddListener(ChangeDropdownOption);
        }

        ~InputTypeSetup()
        {
            inputTypeDropdown.onValueChanged.RemoveListener(ChangeDropdownOption);
        }

        private void ChangeDropdownOption(int index)
        {
            _currentInputType.Value = (InputType) Enum.GetValues(typeof(InputType)).GetValue(index);
            
            Save();
        }

        private void FillDropdown(Dropdown dropdown)
        {
            dropdown.ClearOptions();

            InputType[] inputTypes = (InputType[]) Enum.GetValues(typeof(InputType));

            List<string> enumNames = new List<string>();
            foreach (InputType inputType in inputTypes)
            {
                enumNames.Add(inputType.ToString());
            }

            dropdown.AddOptions(enumNames);
        }

        #region Save&Load

        private void Save()
        {
            if (isSaveInputType) _saver.Save("currentInputType", (int) _currentInputType.Value);
        }

        private void Load()
        {
            if (isSaveInputType) _saver.Load<int>("currentInputType", _ => { _currentInputType.Value = (InputType) _; });
        }

        #endregion
    }
}