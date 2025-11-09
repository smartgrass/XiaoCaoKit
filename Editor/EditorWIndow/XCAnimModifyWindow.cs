using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using XiaoCaoEditor;

namespace AssetEditor.Editor
{
    public class XCAnimModifyWindow : XiaoCaoWindow
    {
        [MenuItem(XCEditorTools.XCAnimModifyWindow)]
        public static XCAnimModifyWindow Open()
        {
            return OpenWindow<XCAnimModifyWindow>("Clip偏移编辑窗口");
        }

        public AnimationClip clip;

        public string rootName;

        public Vector3 offset;

        [Button("偏移")]
        void DoOffset()
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClip is null");
                return;
            }

            // 获取位置曲线
            EditorCurveBinding xPosBinding = EditorCurveBinding.FloatCurve(rootName, typeof(Transform), "m_LocalPosition.x");
            EditorCurveBinding yPosBinding = EditorCurveBinding.FloatCurve(rootName, typeof(Transform), "m_LocalPosition.y");
            EditorCurveBinding zPosBinding = EditorCurveBinding.FloatCurve(rootName, typeof(Transform), "m_LocalPosition.z");
            
            AnimationCurve xPosCurve = AnimationUtility.GetEditorCurve(clip, xPosBinding);
            AnimationCurve yPosCurve = AnimationUtility.GetEditorCurve(clip, yPosBinding);
            AnimationCurve zPosCurve = AnimationUtility.GetEditorCurve(clip, zPosBinding);

            // 如果曲线不存在，则创建默认曲线
            if (xPosCurve == null)
            {
                xPosCurve = new AnimationCurve();
                xPosCurve.AddKey(0f, 0f);
            }

            if (yPosCurve == null)
            {
                yPosCurve = new AnimationCurve();
                yPosCurve.AddKey(0f, 0f);
            }

            if (zPosCurve == null)
            {
                zPosCurve = new AnimationCurve();
                zPosCurve.AddKey(0f, 0f);
            }

            // 对每个关键帧应用偏移
            ApplyOffsetToCurve(xPosCurve, offset.x);
            ApplyOffsetToCurve(yPosCurve, offset.y);
            ApplyOffsetToCurve(zPosCurve, offset.z);

            // 将修改后的曲线设置回clip
            AnimationUtility.SetEditorCurve(clip, xPosBinding, xPosCurve);
            AnimationUtility.SetEditorCurve(clip, yPosBinding, yPosCurve);
            AnimationUtility.SetEditorCurve(clip, zPosBinding, zPosCurve);

            // 保存资产
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"成功对动画 '{clip.name}' 的 '{rootName}' 节点应用偏移 {offset}");
        }

        private void ApplyOffsetToCurve(AnimationCurve curve, float offsetValue)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                Keyframe keyframe = keyframes[i];
                keyframe.value += offsetValue;
                keyframes[i] = keyframe;
            }
            curve.keys = keyframes;
        }
    }
}