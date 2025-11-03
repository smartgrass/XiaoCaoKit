using System;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 对话框管理器，用于方便调用对话框
    /// </summary>
    public static class DialogManager
    {
        private static DialogPanel dialogPanelPrefab;
        private static DialogPanel currentDialog;

        /// <summary>
        /// 显示对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="content">对话框内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调</param>
        /// <param name="isOnly">只允许出现一个</param>
        public static DialogPanel ShowDialog(string title, string content, Action onConfirm = null,
            Action onCancel = null)
        {
            if (dialogPanelPrefab == null)
            {
                // 尝试加载预制体
                GameObject prefab = Resources.Load<GameObject>("DialogPanel");
                if (prefab != null)
                {
                    dialogPanelPrefab = prefab.GetComponent<DialogPanel>();
                }
                else
                {
                    Debug.LogError("DialogPanel prefab not found in Resources!");
                    return null;
                }
            }

            // 创建对话框实例
            DialogPanel dialog = GameObject.Instantiate(dialogPanelPrefab);
            dialog.transform.SetParent(GetCanvasRoot(), false);
            dialog.Show(title, content, onConfirm, onCancel);
            currentDialog = dialog;
            return dialog;
        }

        /// <summary>
        /// 隐藏当前对话框
        /// </summary>
        public static void HideDialog()
        {
            if (currentDialog != null)
            {
                currentDialog.Hide();
                GameObject.Destroy(currentDialog.gameObject);
                currentDialog = null;
            }
        }

        /// <summary>
        /// 获取画布根节点
        /// </summary>
        /// <returns>画布根节点</returns>
        private static Transform GetCanvasRoot()
        {
            return UICanvasMgr.Inst.GetCanvasParent();
        }
    }
}