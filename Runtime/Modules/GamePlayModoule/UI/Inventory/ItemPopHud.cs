using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XiaoCao;
using TEngine;
using System;

///<see cref="BattleHud"/>
public class ItemPopHud : MonoBehaviour, IMgr
{
    [Header("UI References")]
    [SerializeField] private RectTransform popupContainer;
    [SerializeField] private GameObject itemPopupPrefab;
    [SerializeField] private float popupSpacing = 10f;
    [SerializeField] private float popupDuration = 1.5f;
    [SerializeField] private float delayBetweenPopups = 0.2f;

    private List<ItemPopCell> activePopups = new List<ItemPopCell>();
    private Queue<Item> itemQueue = new Queue<Item>();
    private bool isProcessingQueue = false;

    private void OnEnable()
    {
        GameEvent.AddEventListener<Item>(EGameEvent.OnGetItem.ToInt(), OnGetItem);
    }

    private void OnDisable()
    {
        GameEvent.RemoveEventListener<Item>(EGameEvent.OnGetItem.ToInt(), OnGetItem);
    }

    private void OnGetItem(Item item)
    {
        itemQueue.Enqueue(item);
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessItemQueue());
        }
    }

    // 显示物品获得提示
    private void ShowItemGainPopup(Item item)
    {
        if (item == null || item.num <= 0) return;

        GameObject popupObj = Instantiate(itemPopupPrefab, popupContainer);
        ItemPopCell popup = popupObj.GetComponent<ItemPopCell>();

        if (popup == null)
        {
            Debug.LogError("ItemPopup component not found on prefab!");
            Destroy(popupObj);
            return;
        }

        // 设置物品信息
        Sprite icon = item.GetItemSprite();
        string itemName = item.GetItemName();
        popup.SetItemInfo(icon, itemName, item.num);

        // 添加到活动列表并调整位置
        activePopups.Add(popup);
        StartCoroutine(ManagePopupLifetime(popup));
        UpdatePopupPositions();
    }

    // 管理弹出窗口生命周期
    private IEnumerator ManagePopupLifetime(ItemPopCell popup)
    {
        yield return new WaitForSeconds(popupDuration);

        // 从列表移除并更新位置
        activePopups.Remove(popup);
        UpdatePopupPositions();

        // 淡出动画
        popup.FadeOut(() => Destroy(popup.gameObject));
    }

    // 更新所有弹出窗口的位置
    private void UpdatePopupPositions()
    {
        for (int i = 0; i < activePopups.Count; i++)
        {
            RectTransform rect = activePopups[i].GetComponent<RectTransform>();
            Vector2 newPosition = new Vector2(0, i * (rect.sizeDelta.y + popupSpacing));
            rect.anchoredPosition = newPosition;
        }
    }

    // 处理物品队列
    private IEnumerator ProcessItemQueue()
    {
        isProcessingQueue = true;
        while (itemQueue.Count > 0)
        {
            Item item = itemQueue.Dequeue();
            ShowItemGainPopup(item);
            yield return new WaitForSeconds(delayBetweenPopups);
        }
        isProcessingQueue = false;
    }
}
