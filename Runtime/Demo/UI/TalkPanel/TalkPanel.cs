using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;
using XiaoCao.UI;

public class TalkPanel : MonoBehaviour
{
    [Header("UI 组件")] public GameObject dialoguePanel; // 对话面板
    public RawImage speakerImg; // 说话人头像
    public TextMeshProUGUI speakerNameText; // 说话人名字
    public TextMeshProUGUI dialogueText; // 对话文本
    public GameObject optionsPanel; // 选项面板
    public Button continueButton; // 继续按钮
    public GameObject optionButtonPrefab; // 选项按钮预制体

    private List<Button> currentOptionButtons = new List<Button>(); // 当前选项按钮列表
    private Coroutine textTypingCoroutine; // 文本打字协程

    private void Awake()
    {
        // 初始化 UI 状态
        dialoguePanel.SetActive(false);
        optionsPanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinueClick);
    }

    internal void Init()
    {
        gameObject.SetActive(false);
    }

    public void ShowTextData(TalkData node)
    {
        gameObject.SetActive(true);
        // 显示对话面板
        dialoguePanel.SetActive(true);
        // 更新头像和名字
        UpdateSpeakerInfo(node.GetSpeakerAvatar(), node.Str2);

        // 开始逐字显示文本
        //textTypingCoroutine = StartCoroutine(IETypeTextCoroutine(node.dialogueText, node.textSpeed, node.typeSound));
        dialogueText.text = node.Str1.ToLocalizeStr();

        // 无选项时显示继续按钮
        continueButton.gameObject.SetActive(true);
    }


    /// <summary>
    /// 更新说话人信息
    /// </summary>
    private void UpdateSpeakerInfo(Texture texture, string nameKey)
    {
        if (nameKey == "Null")
        {
            speakerImg.enabled = false;
            speakerNameText.text = "";
        }
        else
        {
            speakerImg.enabled = true;
            speakerImg.texture = texture;
            speakerNameText.text = nameKey.ToLocalizeStr();
        }
    }


    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    public void OnContinueClick()
    {
        TalkMgr.Inst.MoveNextTalk();
    }


    // private IEnumerator IETypeTextCoroutine(string text, float speed)
    // {
    //     for (int i = 0; i < text.Length; i++)
    //     {
    //         dialogueText.text += text[i];
    //         yield return new WaitForSeconds(speed);
    //     }
    //     // isTextComplete = true;
    // }

    internal void HidePanel()
    {
        gameObject.SetActive(false);
    }


    #region 选项相关

    /*
    /// <summary>
    /// 等待文本完成后显示选项
    /// </summary>
    private IEnumerator WaitForTextCompleteThenShowOptions(DialogueNode node)
    {
        while (!isTextComplete)
            yield return null;

        //ShowOptions(node.options);
    }

    /// <summary>
    /// 显示选项 TODO 未完善
    /// </summary>
    private void ShowOptions(List<DialogueOption> options)
    {
        optionsPanel.SetActive(true);
        foreach (var option in options)
        {
            GameObject optionObject = Instantiate(optionButtonPrefab, optionsPanel.transform);
            Button optionButton = optionObject.GetComponentInChildren<Button>();
            optionButton.GetComponentInChildren<Text>().text = option.optionText;
            string nextNodeID = option.nextNodeID;
            string eventName = option.optionEventName;
            optionButton.onClick.AddListener(() => OnOptionSelected(nextNodeID, eventName));
            currentOptionButtons.Add(optionButton);
        }
    }

    /// <summary>
    /// 隐藏对话面板
    /// </summary>
    public void HideDialoguePanel()
    {
        dialoguePanel.SetActive(false);
        ClearOptions();
    }
    /// <summary>
    /// 清除选项
    /// </summary>
    private void ClearOptions()
    {
        return;
        foreach (var button in currentOptionButtons)
            Destroy(button.gameObject);
        currentOptionButtons.Clear();
    }

    public class DialogueOption
{
    public string optionText; // 选项文本
    public string nextNodeID; // 选择后跳转的节点 ID
    public string optionEventName; // 选择后触发的事件
}


    /// <summary>
    /// 选项选择事件
    /// </summary>
    private void OnOptionSelected(string nextNodeID, string eventName)
    {
        // 触发选项事件 弃用..
        if (!string.IsNullOrEmpty(eventName))
        {
            DialogueEventManager.Instance.TriggerEvent(eventName);
        }
    }

    */

    #endregion
}

public enum TalkType
{
    Text = 0, // 对话
    End = 1,
    Choice = 2, // 选择
    Event = 3, // 事件
    ShowCanvas = 4,
    List = 5, //连续节点
    DelayNext = 6,
    JumpTo = 7,
    Task = 8 //暂时弃用
}