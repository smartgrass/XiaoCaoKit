using System.Collections;
using UnityEngine;

namespace XiaoCao.UI
{
    /// <summary>
    /// HomeHud事件系统使用示例
    /// </summary>
    public class HomeHudEventExample : MonoBehaviour
    {
        void OnEnable()
        {
        //     // 添加事件监听
        //     HomeHud.EventSystem.AddEventListener(HomeHudEventNames.PanelSwitch, OnPanelSwitch);
        //     HomeHud.EventSystem.AddEventListener<string>(HomeHudEventNames.DataUpdate, OnDataUpdate);
        //     HomeHud.EventSystem.AddEventListener<string, int>(HomeHudEventNames.Notification, OnNotification);
        }

        void OnDisable()
        {
            // // 移除事件监听
            // HomeHud.EventSystem.RemoveEventListener(HomeHudEventNames.PanelSwitch, OnPanelSwitch);
            // HomeHud.EventSystem.RemoveEventListener<string>(HomeHudEventNames.DataUpdate, OnDataUpdate);
            // HomeHud.EventSystem.RemoveEventListener<string, int>(HomeHudEventNames.Notification, OnNotification);
        }

        // 示例方法：切换面板事件处理
        private void OnPanelSwitch()
        {
            Debug.Log("Home Panel Switched!");
        }

        // 示例方法：数据更新事件处理
        private void OnDataUpdate(string data)
        {
            Debug.Log($"Home Data Updated: {data}");
        }

        // 示例方法：通知事件处理
        private void OnNotification(string title, int type)
        {
            Debug.Log($"Home Notification: {title}, Type: {type}");
        }

        // 示例方法：触发事件
        public void TriggerEvents()
        {
            // // 发送无参事件
            // HomeHud.EventSystem.SendEvent(HomeHudEventNames.PanelSwitch);
            //
            // // 发送单参数事件
            // HomeHud.EventSystem.SendEvent(HomeHudEventNames.DataUpdate, "Player Info Updated");
            //
            // // 发送双参数事件
            // HomeHud.EventSystem.SendEvent(HomeHudEventNames.Notification, "New Message", 1);
        }

        // 用于测试的协程
        IEnumerator Start()
        {
            yield return new WaitForSeconds(2f);
            TriggerEvents();
        }
    }
}