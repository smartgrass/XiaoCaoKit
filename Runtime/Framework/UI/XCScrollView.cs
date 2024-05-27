
using System;
using System.Collections.Generic;
using UnityEngine;
using EasingCore;
using FancyScrollView;
using UnityEngine.UIElements;

namespace XiaoCao.FancyScrollRect
{
    /// <summary>
    ///<see cref="XCCell"/> 
    ///源 Example07
    /// </summary>
    class XCScrollView : FancyScrollRect<ItemData, Context>
    {
        [SerializeField] float cellSize = 100f;
        [SerializeField] GameObject cellPrefab = default;

        protected override float CellSize => cellSize;
        protected override GameObject CellPrefab => cellPrefab;
        public int DataCount => ItemsSource.Count;

        public float PaddingTop
        {
            get => paddingHead;
            set
            {
                paddingHead = value;
                Relayout();
            }
        }

        public float PaddingBottom
        {
            get => paddingTail;
            set
            {
                paddingTail = value;
                Relayout();
            }
        }

        public float Spacing
        {
            get => spacing;
            set
            {
                spacing = value;
                Relayout();
            }
        }

        public void OnCellClicked(Action<int> callback)
        {
            Context.OnCellClicked = callback;
        }

        public void UpdateData(IList<ItemData> items, bool setScrollerCount = true)
        {
            UpdateContents(items);
            if (setScrollerCount)
            {
                Scroller.SetTotalCount(items.Count);
            }
        }

        public void ScrollTo(int index, float duration, Ease easing, Alignment alignment = Alignment.Middle)
        {
            UpdateSelection(index);
            ScrollTo(index, duration, easing, GetAlignment(alignment));
        }

        public void JumpTo(int index, Alignment alignment = Alignment.Middle)
        {
            UpdateSelection(index);
            JumpTo(index, GetAlignment(alignment));
        }

        float GetAlignment(Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.Upper: return 0.0f;
                case Alignment.Middle: return 0.5f;
                case Alignment.Lower: return 1.0f;
                default: return GetAlignment(Alignment.Middle);
            }
        }

        void UpdateSelection(int index)
        {
            if (Context.SelectedIndex == index)
            {
                return;
            }

            Context.SelectedIndex = index;
            Refresh();
        }
    }


    class ItemData
    {
        public string Message { get; }

        public ItemData(string message)
        {
            Message = message;
        }
    }

    class Context : FancyScrollRectContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }

    enum Alignment
    {
        Upper,
        Middle,
        Lower,
    }
}


