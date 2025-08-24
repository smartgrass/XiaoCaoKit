using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LongPressOptionSelector : MonoBehaviour
{
    public List<Button> optionButtons;
    public CanvasGroup optionCanvasGroup;
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    private int selectedIndex = -1;
    private ButtonLongPressListener longPressListener;
    private Vector2 initialPressPosition;
    private float optionWidth;

    private void Awake()
    {
        longPressListener = GetComponent<ButtonLongPressListener>();
        if (longPressListener == null)
        {
            Debug.LogError("ButtonLongPressListener component is missing on this GameObject.");
            return;
        }

        longPressListener.onLongPress.AddListener(ShowOptions);
        longPressListener.onDrag.AddListener(OnMove);
        HideOptions();
    }



    private void ShowOptions()
    {
        optionCanvasGroup.alpha = 1;
        optionCanvasGroup.interactable = true;
        optionCanvasGroup.blocksRaycasts = true;

        // 记录初始按下位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            optionCanvasGroup.GetComponent<RectTransform>(),
            Input.mousePosition,
            null,
            out initialPressPosition
        );

        selectedIndex = 0;
        HighlightOption(selectedIndex);

        // 计算每个选项的宽度（假设选项均匀分布）
        if (optionButtons.Count > 0)
        {
            RectTransform optionRect = optionButtons[0].GetComponent<RectTransform>();
            optionWidth = optionRect.rect.width;// + optionRect.GetComponent<HorizontalLayoutGroup>().spacing;
        }
    }

    private void HideOptions()
    {
        optionCanvasGroup.alpha = 0;
        optionCanvasGroup.interactable = false;
        optionCanvasGroup.blocksRaycasts = false;

        selectedIndex = -1;

        // 移除指针移动事件监听
        //EventSystem.current.GetComponent<StandaloneInputModule>().moveAxisEvent -= OnMove;
    }

    private void HighlightOption(int index)
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i == index)
            {
                optionButtons[i].image.color = selectedColor;
            }
            else
            {
                optionButtons[i].image.color = normalColor;
            }
        }
    }

    private void OnMove(PointerEventData eventData)
    {
        if (optionButtons.Count <= 1) return;

        // 获取当前拖拽位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            optionCanvasGroup.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentPosition
        );

        // 计算拖拽偏移量
        float dragOffset = currentPosition.x - initialPressPosition.x;

        // 根据偏移量计算应该选中的选项索引
        int newIndex = Mathf.RoundToInt(dragOffset / optionWidth);
        newIndex = Mathf.Clamp(newIndex, 0, optionButtons.Count - 1);

        // 如果索引变化了，更新选中状态
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            HighlightOption(selectedIndex);
        }
    }

    public void OnPointerUp()
    {
        if (selectedIndex >= 0)
        {
            // 执行选中选项的操作
            optionButtons[selectedIndex].onClick.Invoke();
        }

        HideOptions();
    }
}
