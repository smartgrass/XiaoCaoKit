using System;
using System.Collections.Generic;
using UnityEngine;
using XiaoCao;

//读取数据
public class TalkMgr : Singleton<TalkMgr>
{
    private ChapterTalkData _chapterTalkData;

    public string currentChapterId;

    public int currentTalkIndex;

    public bool isTalking;

    public TalkPanel TalkPanel => UIMgr.Inst.talkPanel;

    public void EnterTalk()
    {
        //进入对话模式,屏蔽操作
        UIMgr.Inst.MidCanvasEnable(false);
        isTalking = true;
    }

    public void EndTalk()
    {
        isTalking = false;
        currentTalkIndex = 0;
        currentChapterId = null;
        _chapterTalkData = null;
        TalkPanel.HidePanel();
        UIMgr.Inst.MidCanvasEnable(true);
    }

    public void StartTalk(string chapterId)
    {
        //TODO: 启动对话
        LoadChapterTalkData(chapterId);
        isTalking = true;
        ShowTalk(0);
    }

    public void MoveNextTalk()
    {
        ShowTalk(currentTalkIndex + 1);
    }

    public void ShowTalk(int index)
    {
        var talkData = GetTalkNode(index);
        currentTalkIndex = index;
        TalkPanel.ShowTalkNode(talkData);
    }


    public void LoadChapterTalkData(string chapterId)
    {
        string str = ConfigMgr.GetTalkChapter(chapterId);
        _chapterTalkData = new ChapterTalkData(str);
        currentChapterId = chapterId;
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

    [Header("说话人信息")] public string speakerName; // 说话人名字

    public string contentText; // 对话文本

    //TODO 第四字段: 特殊信息

    public int NodeID { get; set; }

    public void ReadStr(string str)
    {
        var array = str.Split(",");

        talkType = Enum.Parse<TalkType>(array[0]);

        if (array.Length > 1)
        {
            speakerName = array[1].TrimEnd();
        }

        if (array.Length > 2)
        {
            contentText = array[2].TrimEnd();
        }
    }

    public Texture GetSpeakerAvatar()
    {
        Texture texture = CharacterCaptureManager.Inst.GetSpeakerAvatar(speakerName);
        return texture;
    }
}