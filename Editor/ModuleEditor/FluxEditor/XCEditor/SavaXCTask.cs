using DG.Tweening;
using Flux;
using FluxEditor;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using XiaoCaoEditor;
using static UnityEditor.Progress;
using SerializationUtility = OdinSerializer.SerializationUtility;

public class SavaXCTask
{
    public const string SavaAllSeqName = "XiaoCao/Flux/SavaAllSeq";
    public const string ReadSkillDataName = "Assets/XiaoCao/Log XCTask Data";

    public static XCSeqSetting fSeqSetting;
    public static FSequence curSequence;

    [MenuItem(SavaAllSeqName)]
    private static void Sava()
    {
        var Scene = SceneManager.GetSceneByName("SkillEditor");
        GameObject root = Scene.GetRootGameObjects().First((o) => o.name == "Editor");

        var seqs = root.GetComponentsInChildren<FSequence>(true);
        seqs.LogListStr();

    }
    public static void SavaCurSeq()
    {
        var editor = FSequenceEditorWindow.instance.GetSequenceEditor();
        FSequence Sequence = editor.Sequence;
        if (Sequence == null)
        {
            throw new Exception("no Seq!");
        }
        if (Sequence.SeqSetting == null)
        {
            throw new Exception("no SeqSetting!");
        }

        SavaOneSeq(Sequence);
        AssetDatabase.SaveAssets();     //保存改动的资源
        AssetDatabase.Refresh();
    }

    private static void SavaOneSeq(FSequence Sequence)
    {
        fSeqSetting = Sequence.SeqSetting;
        curSequence = Sequence;
        Transform playerTF = Sequence.Containers[0].Timelines[0].transform;
        List<SkillEventData> skillEvents = new List<SkillEventData>();
        XCTaskData mainData = null;
        bool isMain = true;
        foreach (var _timeline in Sequence.Containers[0].Timelines)
        {
            //一个Object分配一个XCTaskData
            XCTaskData data = GetTaskData(ref mainData, isMain);
            bool hasObjectData = false;
            foreach (var _track in _timeline.Tracks)
            {
                if (!isMain && !hasObjectData)
                {
                    hasObjectData = true;
                    data.objectData = MakeObjectData(_track);
                }
                ReadTrack(_track, data);
            }

            isMain = false;
        }


        string savaPath = XCSetting.GetSkillDataPath(fSeqSetting.type, Sequence._skillId);
        
        Debug.Log($"FLog sava skill{Sequence._skillId} to {savaPath}");

        FileTool.SerializeWrite(savaPath, mainData);
    }

    private static XCTaskData GetTaskData(ref XCTaskData mainData, bool isMain)
    {
        XCTaskData data = new XCTaskData();

        if (isMain)
        {
            mainData = data;
        }
        else
        {
            mainData.AddSubData(data);
        }

        return data;
    }

    [MenuItem(ReadSkillDataName)]
    private static void Read()
    {
        if (!AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".data"))
        {
            Debug.Log($" no .data file {AssetDatabase.GetAssetPath(Selection.activeObject)}");
            return;
        }
        string filePath = AssetDatabase.GetAssetPath(Selection.activeObject);

        XCTaskData data = FileTool.DeserializeRead<XCTaskData>(filePath);

        if (data != null)
        {
            var buffer = SerializationUtility.SerializeValue<XCTaskData>(data, DataFormat.JSON);
            string res = Encoding.UTF8.GetString(buffer);
            Debug.Log($"---  {res}");
        }
        else
        {
            Debug.Log($"---  null {filePath}");
        }
    }


    private static XCTaskData ReadTrack(FTrack _track, XCTaskData taskData)
    {
        var eventType = _track.GetEventType();
        if (eventType == typeof(FMoveEvent))
        {
            _track.Events.ForEach((e) => {
                FMoveEvent moveEvent = (FMoveEvent)e;
                taskData._events.AddRange(moveEvent.ToXCEventList());
            });
        }
        else if (eventType == typeof(FPlayAnimationEvent))
        {
            ReadAnimTrack(_track, taskData);
        }
        else if (DefaultXCEvents.Contains(eventType))
        {
            _track.Events.ForEach((e) => taskData._events.Add(e.ToXCEvent()));
        }

        //else if (eventType == typeof(FObjectEvent)){}
        //else if (eventType == typeof(FTriggerRangeEvent)){}

        //排序 _events
        taskData.SortEvents();

        return taskData;
    }

    private static void ReadAnimTrack(FTrack _track, XCTaskData taskData)
    {
        int length = _track.Events.Count;
        Dictionary<string,AnimationClip> animDic = new Dictionary<string,AnimationClip>();
        for (int i = 0; i < length; i++)
        {
            FPlayAnimationEvent animEvent = (FPlayAnimationEvent)_track.Events[i];
            XCEvent xcEvent = animEvent.ToXCEvent();
            xcEvent.eName = $"{curSequence._skillId}_{i}";
            animDic.Add(xcEvent.eName, animEvent._animationClip);
            taskData._events.Add(xcEvent);
        }
        //检测动画机连线
        XCAnimatorTool.CheckAnim(fSeqSetting.targetAnimtorController, animDic,curSequence._skillId);
    }

   


    static Type[] DefaultXCEvents = {
        typeof(FTweenRotationEvent)  ,
        typeof(FTweenScaleEvent) , typeof(FPlayParticleEvent) };


    private static ObjectData MakeObjectData(FTrack _track)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_track.Owner);

        if (!path.StartsWith(XCSetting.PerfabDir))
        {
            FileInfo info = new FileInfo(path);

            string newPath = Path.Combine(XCSetting.PerfabDir, fSeqSetting.type.ToString(), info.Name);

            Debug.LogWarning($"FLog {info.Name} MoveTo {newPath}");

            AssetDatabase.MoveAsset(path, newPath);

            path = newPath;
        }

        ObjectData objectData = new ObjectData();
        objectData.ObjectPath = path;
        objectData.scale = _track.Owner.localScale;
        objectData.eulerAngle = _track.Owner.localEulerAngles;
        objectData.position = _track.Owner.localPosition;
        objectData.transfromType = _track.transfromType;
        objectData.endFrame = _track.GetEndFrame();
        return objectData;
    }

}