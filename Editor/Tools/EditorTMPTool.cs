using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoEditor
{
    /// <summary>
    /// TextMeshPro 工具
    /// </summary>
    public static class EditorTMPTool
    {
        [MenuItem("GameObject/XiaoCao/替换子物体TMP字体", priority = 50)]
        private static void CheckEnTMPFont_0() => CheckEnTMPFont();

        //["XiaoCao/"]
        [MenuItem(XCEditorTools.AssetCheck + "Text转Tmp", priority = 50)]
        private static void OnTextToTmp_0() => OnTextToTmp();

        private static void OnTextToTmp()
        {
            foreach (var item in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(item);
                Text2TextMeshPro(path);
            }
        }
        private static void Text2TextMeshPro(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root)
            {
                Text[] list = root.GetComponentsInChildren<Text>(true);
                for (int i = 0; i < list.Length; i++)
                {
                    Text text = list[i];
                    Transform target = text.transform;
                    Vector2 size = text.rectTransform.sizeDelta;
                    string strContent = text.text;
                    Color color = text.color;
                    int fontSize = text.fontSize;
                    FontStyle fontStyle = text.fontStyle;
                    TextAnchor textAnchor = text.alignment;
                    bool richText = text.supportRichText;

                    HorizontalWrapMode horizontalWrapMode = text.horizontalOverflow;
                    VerticalWrapMode verticalWrapMode = text.verticalOverflow;
                    bool raycastTarget = text.raycastTarget;
                    bool isBestFit = text.resizeTextForBestFit;
                    int minSize = text.resizeTextMinSize;
                    int maxSize = text.resizeTextMaxSize;
                    GameObject.DestroyImmediate(text);

                    TextMeshProUGUI textMeshPro = target.gameObject.AddComponent<TextMeshProUGUI>();
                    ComponentUtility.MoveComponentUp(textMeshPro); //上移

                    textMeshPro.rectTransform.sizeDelta = size;
                    textMeshPro.text = strContent;
                    textMeshPro.color = color;
                    textMeshPro.fontSize = fontSize;
                    textMeshPro.enableAutoSizing = isBestFit;
                    textMeshPro.fontSizeMax = maxSize;
                    textMeshPro.fontSizeMin = minSize;



                    textMeshPro.fontStyle = fontStyle == FontStyle.BoldAndItalic ? FontStyles.Bold : (FontStyles)fontStyle;
                    switch (textAnchor)
                    {
                        case TextAnchor.UpperLeft:
                            textMeshPro.alignment = TextAlignmentOptions.TopLeft;
                            break;
                        case TextAnchor.UpperCenter:
                            textMeshPro.alignment = TextAlignmentOptions.Top;
                            break;
                        case TextAnchor.UpperRight:
                            textMeshPro.alignment = TextAlignmentOptions.TopRight;
                            break;
                        case TextAnchor.MiddleLeft:
                            textMeshPro.alignment = TextAlignmentOptions.MidlineLeft;
                            break;
                        case TextAnchor.MiddleCenter:
                            textMeshPro.alignment = TextAlignmentOptions.Midline;
                            break;
                        case TextAnchor.MiddleRight:
                            textMeshPro.alignment = TextAlignmentOptions.MidlineRight;
                            break;
                        case TextAnchor.LowerLeft:
                            textMeshPro.alignment = TextAlignmentOptions.BottomLeft;
                            break;
                        case TextAnchor.LowerCenter:
                            textMeshPro.alignment = TextAlignmentOptions.Bottom;
                            break;
                        case TextAnchor.LowerRight:
                            textMeshPro.alignment = TextAlignmentOptions.BottomRight;
                            break;
                    }
                    textMeshPro.richText = richText;
                    if (verticalWrapMode == VerticalWrapMode.Overflow)
                    {
                        textMeshPro.enableWordWrapping = true;
                        textMeshPro.overflowMode = TextOverflowModes.Overflow;
                    }
                    else
                    {
                        textMeshPro.enableWordWrapping = horizontalWrapMode == HorizontalWrapMode.Overflow ? false : true;
                    }
                    textMeshPro.raycastTarget = raycastTarget;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                Debug.LogError($"预制体：{path} 保存失败!");
            }
            else
            {
                Debug.Log($"预制体：{path} 保存成功!");
            }
        }
        private static void CheckEnTMPFont()
        {
            string fontPath = "Assets/RawResources/Font/Aldrich-Regular SDF.asset";

            Transform tf = Selection.activeTransform;
            var tmps = tf.GetComponentsInChildren<TMP_Text>(true);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                Debug.Log("没有找到字体");
                return;
            }

            foreach (var item in tmps)
            {
                if (item.font != font)
                {
                    item.font = font;
                    EditorUtility.SetDirty(item);
                }
            }
        }
    }

}
