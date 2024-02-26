using Flux;
using FluxEditor;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using XiaoCaoEditor;
using Debug = UnityEngine.Debug;
using SerializationUtility = OdinSerializer.SerializationUtility;
using Task = System.Threading.Tasks.Task;

public class SaveXCTask
{
    public const string SavaAllSeqName = XCEditorTools.XiaoCaoFlux + "SavaAllSeq";
    public const string ReadSkillDataName = XCEditorTools.AssetCheck + "Log XCTaskData";

    public const string LoadLubanExcelName = XCEditorTools.XiaoCaoLuban + "导入表格数据";
    public const string LoadLubanExcelCodeName = XCEditorTools.XiaoCaoLuban + "导入表格数据&代码";
    
    public const string SavaCurSeqName = XCEditorTools.XiaoCaoGameObject + "保存Sequence技能&代码";

    public static XCSeqSetting fSeqSetting;
    public static FSequence curSequence;

    [MenuItem(SavaAllSeqName)]
    private static void Sava()
    {
        var Scene = SceneManager.GetSceneByName("SkillEditor");
        GameObject root = Scene.GetRootGameObjects().First((o) => o.name == "Editor");

        var seqs = root.GetComponentsInChildren<FSequence>(true);
        seqs.LogListStr();

        foreach (var seq in seqs)
        {
            SavaOneSeq(seq);
        }
    }
    
    [MenuItem(SavaCurSeqName)]
    private static void  SavaSelectSeq()
    {
        foreach (var i in Selection.objects)
        {
            GameObject go = i as GameObject;;
            if (go.TryGetComponent<FSequence>(out FSequence seq))
            {
                SavaOneSeq(seq);
            }
        }
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
        int timelineId = 0;
        foreach (var _timeline in Sequence.Containers[0].Timelines)
        {
            //一个_timeline->一个Object->一个XCTaskData
            bool isMain = timelineId == 0;
            XCTaskData data = GetTaskData(ref mainData, timelineId);
            bool hasObjectData = false;
            foreach (var _track in _timeline.Tracks)
            {
                if (!isMain && !hasObjectData)
                {
                    hasObjectData = true;
                    data.objectData = MakeObjectData(_track);
                    data.objectData.index = timelineId;
                }
                ReadTrack(_track, data);
            }

            timelineId++;
        }


        string savaPath = XCPathConfig.GetSkillDataPath(fSeqSetting.type, Sequence._skillId);

        Debug.Log($"FLog sava skill{Sequence._skillId} to {savaPath}");

        FileTool.SerializeWrite(savaPath, mainData);
    }

    private static XCTaskData GetTaskData(ref XCTaskData mainData, int timelineId)
    {
        XCTaskData data = new XCTaskData();

        if (timelineId == 0)
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
        if (!AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".bytes"))
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


    private static void ReadTrack(FTrack _track, XCTaskData taskData)
    {
        var eventType = _track.GetEventType();
        if (eventType == typeof(FMoveEvent))
        {
            _track.Events.ForEach((e) =>
            {
                FMoveEvent moveEvent = (FMoveEvent)e;
                taskData._events.AddRange(moveEvent.ToXCEventList());
            });
        }
        else if (eventType == typeof(FMsgEvent))
        {
            _track.Events.ForEach((e) =>
            {
                FMsgEvent fe = (FMsgEvent)e;
                taskData._events.AddRange(fe.ToXCEventList());
                //XCMsgEvent lastEvent = taskData._events[taskData._events.Count - 1] as XCMsgEvent;
            });
        }
        else if (eventType == typeof(FPlayAnimationEvent))
        {
            ReadAnimTrack(_track, taskData);
        }
        else if (eventType == typeof(FPlayParticleEvent))
        {
            taskData.objectData.isPs = true;
            //taskData.start = e
            Debug.Log($"--- {taskData.objectData.ObjectPath} isPs");
            return;
        }
        else if (DefaultXCEvents.Contains(eventType))
        {
            _track.Events.ForEach((e) => taskData._events.Add(e.ToXCEvent()));
        }

        //else if (eventType == typeof(FObjectEvent)){}
        //else if (eventType == typeof(FTriggerRangeEvent)){}

        //排序 _events
        taskData.SortEvents();

    }

    private static void ReadAnimTrack(FTrack _track, XCTaskData taskData)
    {
        int length = _track.Events.Count;
        Dictionary<string, AnimationClip> animDic = new Dictionary<string, AnimationClip>();
        for (int i = 0; i < length; i++)
        {
            FPlayAnimationEvent animEvent = (FPlayAnimationEvent)_track.Events[i];
            XCEvent xcEvent = animEvent.ToXCEvent();
            xcEvent.eName = $"{curSequence._skillId}_{i}";
            animDic.Add(xcEvent.eName, animEvent._animationClip);
            taskData._events.Add(xcEvent);
        }
        //检测动画机连线
        XCAnimatorTool.CheckAnim(fSeqSetting.targetAnimtorController, animDic, curSequence._skillId);
    }




    static Type[] DefaultXCEvents = {
        typeof(FTweenRotationEvent)  ,
        typeof(FTweenScaleEvent)};


    private static ObjectData MakeObjectData(FTrack _track)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_track.Owner);

        if (!path.StartsWith(XCPathConfig.PerfabDir))
        {
            FileInfo info = new FileInfo(path);

            string newPath = Path.Combine(XCPathConfig.GetSkillPrefabDir(fSeqSetting.type), info.Name);

            Debug.LogWarning($"FLog {info.Name} CopyTo {newPath}");

            PrefabUtility.UnpackPrefabInstance(_track.Owner.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAssetAndConnect(_track.Owner.gameObject, newPath, InteractionMode.AutomatedAction);

            path = newPath;
        }

        ObjectData objectData = new ObjectData();
        objectData.ObjectPath = path;
        objectData.scale = _track.Owner.localScale;
        objectData.eulerAngle = _track.Owner.localEulerAngles;
        objectData.position = _track.Owner.localPosition;
        objectData.transfromType = _track.transfromType;
        var range = _track.GetFrameRange();
        objectData.startFrame = range.Start;
        objectData.endFrame = range.End;
        return objectData;
    }


    [MenuItem(LoadLubanExcelName)]
    public static void LoadLubanExcel()
    {
        string path = $"{PathTool.GetUpperDir(Application.dataPath)}/Tools/gen_code_data.bat";
        CommandHelper.ExecuteBatCommand(path);
    }


}