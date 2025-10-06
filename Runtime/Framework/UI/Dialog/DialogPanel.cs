using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 通用对话框面板，支持标题、内容和确认/取消回调
    /// </summary>
    public class DialogPanel : MonoBehaviour
    {
        [Header("UI Components")]
        public TMP_Text titleText;
        public TMP_Text contentText;
        public Button confirmButton;
        public Button cancelButton;

        private Action onConfirm;
        private Action onCancel;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClick);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClick);
        }

        /// <summary>
        /// 显示对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="content">对话框内容</param>
        /// <param name="onConfirmCallback">确认回调</param>
        /// <param name="onCancelCallback">取消回调</param>
        public void Show(string title, string content, Action onConfirmCallback = null, Action onCancelCallback = null)
        {
            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = title;

            if (contentText != null)
                contentText.text = content;

            onConfirm = onConfirmCallback;
            onCancel = onCancelCallback;
        }

        /// <summary>
        /// 隐藏对话框
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirmClick()
        {
            onConfirm?.Invoke();
            Hide();
        }

        private void OnCancelClick()
        {
            onCancel?.Invoke();
            Hide();
        }
    }
}