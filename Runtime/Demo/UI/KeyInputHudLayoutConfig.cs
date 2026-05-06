using System;
using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public enum ParentSet
    {
        NoChange,
        LeftDown,
        RightDown,
    }

    [CreateAssetMenu(menuName = "SO/KeyInputHudLayoutConfig", fileName = "KeyInputHudLayoutConfig")]
    public class KeyInputHudLayoutConfig : ScriptableObject
    {
        public KeyInputHudLayoutData pc = new KeyInputHudLayoutData();
        public KeyInputHudLayoutData mobile = new KeyInputHudLayoutData();

        public KeyInputHudLayoutData GetLayout(UserInputType inputType)
        {
            return inputType == UserInputType.Touch ? mobile : pc;
        }
    }

    [Serializable]
    public class KeyInputHudLayoutData
    {
        public bool isSaved;
        public KeyInputHudLocalPositionData joystick = new KeyInputHudLocalPositionData();
        public List<KeyInputHudLocalPositionData> slots = new List<KeyInputHudLocalPositionData>();
        public KeyInputHudLocalPositionData extraSkillBtn = new KeyInputHudLocalPositionData();
        public KeyInputHudLocalPositionData rollBtn = new KeyInputHudLocalPositionData();
        public KeyInputHudLocalPositionData jumpBtn = new KeyInputHudLocalPositionData();
        public KeyInputHudLocalPositionData norAtkBtn = new KeyInputHudLocalPositionData();
    }

    [Serializable]
    public class KeyInputHudLocalPositionData
    {
        public string name;
        public ParentSet parentSet = ParentSet.NoChange;
        public Vector2 anchoredPosition;
        public float localSize = 1f;
    }
}
