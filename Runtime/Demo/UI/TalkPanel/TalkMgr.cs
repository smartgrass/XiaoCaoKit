using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

//读取数据
public class TalkMgr : Singleton<TalkMgr>
{
    // 使用栈来保存对话状态，支持嵌套任务
    private Stack<ChapterTalkState> talkStateStack = new Stack<ChapterTalkState>();

    private ChapterTalkData _chapterTalkData;

    public int currentTalkIndex;

    public bool isTalking;

    // 对话状态类，用于保存对话进度
    private class ChapterTalkState
    {
        public ChapterTalkData chapterData;
        public string chapterId;
        public int talkIndex;
    }

    public TalkPanel TalkPanel => UIMgr.Inst.talkPanel;

    public void EnterTalk()
    {
        //进入对话模式,屏蔽操作
        UIMgr.Inst.MidCanvasEnable(false);
        isTalking = true;
    }

    public void EndTalk(bool isReShowCanvas = true)
    {
        // 检查是否有嵌套的对话任务
        if (talkStateStack.Count > 0)
        {
            // 恢复上一层对话状态
            var prevState = talkStateStack.Pop();
            _chapterTalkData = prevState.chapterData;
            currentTalkIndex = prevState.talkIndex;
            // 继续显示原来的对话
            ShowTalk(currentTalkIndex);
        }
        else
        {
            // 真正结束所有对话
            isTalking = false;
            currentTalkIndex = 0;
            _chapterTalkData = null;
            TalkPanel.HidePanel();
            if (isReShowCanvas)
            {
                UIMgr.Inst.MidCanvasEnable(true);
            }
        }
    }

    public void StartTalk(string chapterId)
    {
        LoadChapterTalkData(chapterId);
        isTalking = true;
        ShowTalk(0);
    }

    public void StartTask(string taskId)
    {
        string str = ConfigMgr.GetTalkChapter(taskId);
        var data = new ChapterTalkData(str);
        ShowTalkNode(data.talkNodes[0]);
    }

    /// <summary>
    /// 显示对话节点
    /// </summary>pu
    public void ShowTalkNode(TalkData node)
    {
        TalkType type = node.talkType;
        if (type == TalkType.Text)
        {
            TalkPanel.ShowTextData(node);
        }
        else if (type == TalkType.List)
        {
            int listLen = int.Parse(node.Str1);
            for (int i = 0; i < listLen; i++)
            {
                MoveNextTalk();
            }
        }
        else if (type == TalkType.Task)
        {
            StartTask(node.Str1);
        }
        else if (node.talkType == TalkType.End)
        {
            bool isHide = true;
            if (node.array.Length > 1)
            {
                isHide = bool.Parse(node.Str1);
            }

            EndTalk(isHide);
        }
        else if (node.talkType == TalkType.ShowCanvas)
        {
            bool isShow = true;
            if (node.array.Length > 0)
            {
                isShow = bool.Parse(node.Str1);
            }
            UIMgr.Inst.MidCanvasEnable(isShow);
        }
        else if (node.talkType == TalkType.Event)
        {
            //执行后直接继续
            // if (node.Str1 == "DoSkill")
            // {
            //     string skillId = node.Str2;
            //     string[] cmdList = skillId.Split("|");
            //     GameDataCommon.LocalPlayer.component.control.DoActCombol(cmdList);
            // }
        }

        // 如果有选项，等待文本完成后显示选项
        //if (node.hasOptions)
        //{
        //    continueButton.gameObject.SetActive(false);
        //    StartCoroutine(WaitForTextCompleteThenShowOptions(node));
        //}
    }

    IEnumerator IEActCombol(IRoleControl control, string[] cmdList)
    {
        foreach (string cmd in cmdList)
        {
            // control.AIMsg(ActMsgType.Skill, cmd);
            yield return null;
            // yield return new WaitUntil(NoBusy);
        }
    }


    public void MoveNextTalk()
    {
        ShowTalk(currentTalkIndex + 1);
    }

    public void ShowTalk(int index)
    {
        var talkData = GetTalkNode(index);
        currentTalkIndex = index;
        ShowTalkNode(talkData);
    }


    public void LoadChapterTalkData(string chapterId)
    {
        string str = ConfigMgr.GetTalkChapter(chapterId);
        _chapterTalkData = new ChapterTalkData(str);
    }

    public TalkData GetTalkNode(int index)
    {
        int len = _chapterTalkData.talkNodes.Count;
        if (index < 0 || index >= len)
        {
            Debug.LogWarning($"TalkNode index out of range: {index}, total nodes: {len}");
            return new TalkData() { talkType = TalkType.End };
        }

        return _chapterTalkData.talkNodes[index];
    }
}


public class ChapterTalkData
{
    public ChapterTalkData()
    {
    }

    public ChapterTalkData(string str)
    {
        ReadString(str);
    }

    public List<TalkData> talkNodes = new List<TalkData>();

    public void ReadString(string str)
    {
        talkNodes.Clear();
        var lines = str.Split("\n");
        for (int i = 0; i < lines.Length; i++)
        {
            TalkData talkData = new TalkData();
            talkData.ReadStr(lines[i]);
            talkData.NodeID = i;
            talkNodes.Add(talkData);
        }
    }
}

[Serializable]
public class TalkData
{
    public TalkType talkType;

    public string[] array;

    //[Header("说话人信息")] 

    public string Str1 => array[1];
    public string Str2 => array[2];


    //TODO 第四字段: 特殊信息

    public int NodeID { get; set; }

    public void ReadStr(string str)
    {
        str = str.Replace("\r", "").Replace("\n", "");
        var array = str.Split(",");
        // var array = Regex.Split(str, @"\s*,\s*");
        this.array = array;

        if (!Enum.TryParse<TalkType>(array[0], out talkType))
        {
            Debug.Log($"--  error type {array[0]}");
        }
    }

    public Texture GetSpeakerAvatar()
    {
        Texture texture = CharacterCaptureManager.Inst.GetSpeakerAvatar(Str2);
        return texture;
    }
}