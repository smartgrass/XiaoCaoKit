using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;

namespace FluxEditor
{
    public class FSequenceSubEditor : XiaoCaoWindow
    {
		[ShowIf(nameof(IsSwitch))]
		public GameObject target;

		private FSequenceSubType type;

		private bool IsSwitch => type == FSequenceSubType.SwitchModel;

        public static void Show(FSequenceSubType type)
		{
            FSequenceSubEditor win = OpenWindow<FSequenceSubEditor>(type.ToString());
            win.type = type;
        }


		[Button]
		void Sure()
		{
			if (target == null)
			{
				Debug.LogError("--- target nul");
				return;
			}

			Debug.Log($"--- 切换模型");
		}

		public enum FSequenceSubType
		{
			SwitchModel
		}

    }
}
