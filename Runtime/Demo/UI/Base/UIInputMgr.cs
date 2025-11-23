using UnityEngine;
using UnityEngine.EventSystems;

namespace XiaoCao
{
    public class UIInputMgr : MonoBehaviour
    {
        //Selectable
        //给ui写一套输入检测,包括标准的Move,Select,UnSelect和Submit等操作
        //检测到输入后使用GameEvent发送
        public static UIInputMgr Instance { get; private set; }
        private EventSystem eventSystem => EventSystem.current;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // 这里可以添加自定义的输入检测逻辑
            // 目前主要依靠Unity的EventSystem自动处理UI导航

            // 可以在这里添加额外的输入检测，比如键盘快捷键等
            DetectCustomInputs();
        }

        private void DetectCustomInputs()
        {
            // 示例：检测ESC键用于取消选择
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    eventSystem.SetSelectedGameObject(null);
                }
            }

            // 其他自定义输入检测可以在这里添加
        }

        /// <summary>
        /// 设置指定的GameObject为当前选中对象
        /// </summary>
        /// <param name="selectableObject">要选中的游戏对象</param>
        public void SetSelectedObject(GameObject selectableObject)
        {
            if (eventSystem != null && selectableObject != null)
            {
                eventSystem.SetSelectedGameObject(selectableObject);
            }
        }

        /// <summary>
        /// 获取当前选中的游戏对象
        /// </summary>
        /// <returns>当前选中的游戏对象</returns>
        public GameObject GetCurrentSelectedObject()
        {
            return eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        }
    }
}