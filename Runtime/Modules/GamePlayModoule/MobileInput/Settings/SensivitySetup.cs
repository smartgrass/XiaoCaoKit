using UnityEngine;
using UnityEngine.UI;
using MFPC.Input;
using MFPC.Input.PlayerInput;
using MFPC.Utils.SaveLoad;

namespace MFPC
{
    [System.Serializable]
    public class SensivitySetup
    {
        [SerializeField] private Slider sensivitySlider;
        [SerializeField] private Slider cameraSpeedSmoothHorizontalSlider;
        [SerializeField] private Slider cameraSpeedSmoothVerticalSlider;

        private PlayerInputTuner _playerInputTuner;
        private ISaver _saver;

        public void Initialize(PlayerInputTuner playerInputTuner)
        {
            _playerInputTuner = playerInputTuner;
            _saver = new PlayerPrefsSaver("SensivitySetup");

            sensivitySlider.maxValue = SensitiveData.MaxSensitivity;
            cameraSpeedSmoothHorizontalSlider.maxValue = SensitiveData.MaxRotateSpeedSmooth;
            cameraSpeedSmoothVerticalSlider.maxValue = SensitiveData.MaxRotateSpeedSmooth;

            sensivitySlider.value = _playerInputTuner.SensitiveData.Sensitivity;
            cameraSpeedSmoothHorizontalSlider.value = _playerInputTuner.SensitiveData.RotateSpeedSmoothHorizontal;
            cameraSpeedSmoothVerticalSlider.value = _playerInputTuner.SensitiveData.RotateSpeedSmoothVertical;

            Load();
            
            sensivitySlider.onValueChanged.AddListener(ChangeSensivity);
            cameraSpeedSmoothHorizontalSlider.onValueChanged.AddListener(ChangeCameraSpeedSmoothHorizontal);
            cameraSpeedSmoothVerticalSlider.onValueChanged.AddListener(ChangeCameraSpeedSmoothVertical);
        }

        ~SensivitySetup()
        {
            cameraSpeedSmoothHorizontalSlider.onValueChanged.RemoveAllListeners();
            cameraSpeedSmoothVerticalSlider.onValueChanged.RemoveAllListeners();
        }

        private void ChangeSensivity(float value)
        {
            _playerInputTuner.SensitiveData.SetSensitivity(value);
            Save();
        }

        private void ChangeCameraSpeedSmoothHorizontal(float value)
        {
            _playerInputTuner.SensitiveData.SetRotateSpeedSmoothHorizontal(value);
            Save();
        }

        private void ChangeCameraSpeedSmoothVertical(float value)
        {
            _playerInputTuner.SensitiveData.SetRotateSpeedSmoothVertical(value);
            Save();
        }

        #region Save&Load
        
        private void Save()
        {
            _saver.Save("sensivitySlider", sensivitySlider.value)
                .Save("cameraSpeedSmoothHorizontalSlider", cameraSpeedSmoothHorizontalSlider.value)
                .Save("cameraSpeedSmoothVerticalSlider", cameraSpeedSmoothVerticalSlider.value);
        }

        private void Load()
        {
            _saver.Load<float>("sensivitySlider", _ => { sensivitySlider.value = _; })
                .Load<float>("cameraSpeedSmoothHorizontalSlider", _ => { cameraSpeedSmoothHorizontalSlider.value = _; })
                .Load<float>("cameraSpeedSmoothVerticalSlider", _ => { cameraSpeedSmoothVerticalSlider.value = _; });

            _playerInputTuner.SensitiveData.SetSensitivity(sensivitySlider.value);
            _playerInputTuner.SensitiveData.SetRotateSpeedSmoothHorizontal(cameraSpeedSmoothHorizontalSlider.value);
            _playerInputTuner.SensitiveData.SetRotateSpeedSmoothVertical(cameraSpeedSmoothVerticalSlider.value);
        }       
        
        #endregion
    }
}