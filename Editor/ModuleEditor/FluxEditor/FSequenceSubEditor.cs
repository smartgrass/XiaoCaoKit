using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XiaoCao;

namespace FluxEditor
{

    public abstract class FSequenceSubEditor : XiaoCaoWindow
    {
        private FSequenceSubType type;

        public static void Show(FSequenceSubType subType)
        {

            Type type = GetSubEditorType(subType);
            if (type == null)
            {
                return;
            }
            FSequenceSubEditor win = GetWindow(type, false, subType.ToString()) as FSequenceSubEditor;
            win.Show();
            win.type = subType;
        }

        private static Type GetSubEditorType(FSequenceSubType subType)
        {
            var types = typeof(FSequenceSubEditor).Assembly.GetTypes()
                .Where(type => typeof(FSequenceSubEditor).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var type in types)
            {
                if (type.Name.Contains(subType.ToString()))
                {
                    Debug.Log($"--- {type.Name}");
                    return type;
                }
            }
            Debug.LogError($"--- no subType {subType}");
            return null;
        }


        ///类型于名字强相关 <see cref="FCreateSettingSubEditor"/>
        public enum FSequenceSubType
        {
            SwitchModel,
            CreateSetting
        }
    }
}
