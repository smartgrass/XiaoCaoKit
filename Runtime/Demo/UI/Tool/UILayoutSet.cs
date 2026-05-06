using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XiaoCaoKit.UI
{
    public enum UILayoutDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
    }

    public class UILayoutSet : MonoBehaviour
    {
        [Min(0)] public int rowCount = 0;
        [Min(0)] public int columnCount = 0;
        public UILayoutDirection direction = UILayoutDirection.LeftToRight;
        public Vector2 spacing = new Vector2(100f, 100f);

        [Button("Sort Children", 0, EButtonEnableMode.Editor)]
        public void SortChildren()
        {
            List<RectTransform> children = GetDirectChildren();
            if (children.Count <= 1)
            {
                return;
            }

            RectTransform firstChild = children[0];
            if (firstChild == null)
            {
                return;
            }

            Vector2 startPos = firstChild.anchoredPosition;
#if UNITY_EDITOR
            RecordUndo(children);
#endif

            for (int i = 1; i < children.Count; i++)
            {
                RectTransform child = children[i];
                if (child == null)
                {
                    continue;
                }

                Vector2 offset = GetOffset(i, children.Count);
                child.anchoredPosition = startPos + offset;
#if UNITY_EDITOR
                PrefabUtility.RecordPrefabInstancePropertyModifications(child);
                EditorUtility.SetDirty(child);
#endif
            }
        }

        private List<RectTransform> GetDirectChildren()
        {
            List<RectTransform> children = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child != null)
                {
                    children.Add(child);
                }
            }

            return children;
        }

        private Vector2 GetOffset(int layoutIndex, int totalCount)
        {
            int primaryCount = GetPrimaryCount(totalCount);
            int primaryIndex = layoutIndex % primaryCount;
            int secondaryIndex = layoutIndex / primaryCount;

            switch (direction)
            {
                case UILayoutDirection.RightToLeft:
                    return new Vector2(-primaryIndex * spacing.x, -secondaryIndex * spacing.y);
                case UILayoutDirection.TopToBottom:
                    return new Vector2(secondaryIndex * spacing.x, -primaryIndex * spacing.y);
                case UILayoutDirection.BottomToTop:
                    return new Vector2(secondaryIndex * spacing.x, primaryIndex * spacing.y);
                default:
                    return new Vector2(primaryIndex * spacing.x, -secondaryIndex * spacing.y);
            }
        }

        private int GetPrimaryCount(int totalCount)
        {
            if (direction == UILayoutDirection.LeftToRight || direction == UILayoutDirection.RightToLeft)
            {
                if (columnCount > 0)
                {
                    return columnCount;
                }

                if (rowCount > 0)
                {
                    return Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)rowCount));
                }
            }
            else
            {
                if (rowCount > 0)
                {
                    return rowCount;
                }

                if (columnCount > 0)
                {
                    return Mathf.Max(1, Mathf.CeilToInt(totalCount / (float)columnCount));
                }
            }

            return Mathf.Max(1, totalCount);
        }

#if UNITY_EDITOR
        private static void RecordUndo(List<RectTransform> children)
        {
            Object[] targets = new Object[children.Count];
            for (int i = 0; i < children.Count; i++)
            {
                targets[i] = children[i];
            }

            Undo.RecordObjects(targets, "Sort UI Layout");
        }
#endif
    }
}
