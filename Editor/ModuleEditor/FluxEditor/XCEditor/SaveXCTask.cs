using AssetEditor.Editor;
using cfg;
using Flux;
using FluxEditor;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using XiaoCaoEditor;
using Debug = UnityEngine.Debug;
using FUtility = FluxEditor.FUtility;
using Object = UnityEngine.Object;
using SerializationUtility = OdinSerializer.SerializationUtility;
public class SaveTask { }

public class SaveXCTask
{
    public const string SavaAllSeqName = XCEditorTools.XiaoCaoFlux + "SavaAllSeq";
    public const string HelpName = XCEditorTools.XiaoCaoFlux + "编辑器说明";
    public const string OpenWindowName = XCEditorTools.XiaoCaoFlux + "OpenWindow";
    public const string ReadSkillDataName = XCEditorTools.AssetCheck + "Log XCTaskData";

    public const string LoadLubanExcelName = XCEditorTools.XiaoCaoLuban + "生成数据";
    public const string LoadLubanExcelCodeName = XCEditorTools.XiaoCaoLuban + "生成数据&代码";

    public const string SavaCurSeqName = XCEditorTools.XiaoCaoGameObject + "保存Sequence技能&代码";
    public const string RepalcePrefabName = XCEditorTools.XiaoCaoGameObject + "打开替换工具(skillEditor)";

    public static XCSeqSetting fSeqSetting;
    public static FSequence curSequence;

    [MenuItem(HelpName)]
    public static void ShowHelp()
    {
        string path = FUtility.GetFluxDirectoryPath() + "/ReadMe.FluxEditor.md";
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
    }

    [MenuItem(OpenWindowName)]
    private static void OpenWindow()
    {
        FSequenceEditorWindow.Open();
    }

    [MenuItem(SavaAllSeqName)]
    public static void SavaAll()
    {
        //var Scene = EditorSceneManager.GetActiveScene();

        //GameObject root = Scene.GetRootGameObjects().First((o) => o.name == "Editor");

        FSequence[] seqs = GameObject.FindObjectsOfType<FSequence>();

        seqs.LogListStr();

        foreach (var seq in seqs)
        {
            if (seq.gameObject.activeInHierarchy)
            {
                SavaOneSeq(seq);
            }
        }
    }

    [MenuItem(SavaCurSeqName)]
    private static void SavaSelectSeq()
    {
        foreach (var i in Selection.objects)
        {
            GameObject go = i as GameObject; ;
            if (go.TryGetComponent<FSequence>(out FSequence seq))
            {
                SavaOneSeq(seq);
            }
        }
    }

    [MenuItem(RepalcePrefabName)]
    private static void RepalcePrefab()
    {
        XiaoCaoWindow.OpenWindow<XCRepalceGuidWin>("XCRepalceGuidWin");
        //var win = XCRepalceToolWin.Open();
        //win.instance = Selection.activeGameObject as GameObject;
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
        if (Sequence == null)
        {
            return;
        }
        Debug.Log($"--- SavaOneSeq {Sequence.name} ");
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

            if (curSequence.Speed < 0)
            {
                Debug.Log("--- Speed < 0 ");
            }
            if (curSequence.Speed == 0)
            {
                Debug.LogError("--- Speed = 0");
            }

            data.speed = Mathf.Abs(curSequence.Speed);
            bool hasObjectData = false;
            foreach (var _track in _timeline.Tracks.Where((t) => t.enabled))
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


        SortData(mainData);

        string savaPath = XCPathConfig.GetSkillDataPath(fSeqSetting.raceId, Sequence._skillId);

        Debug.Log($"FLog sava skill{Sequence._skillId} to {savaPath}");

        FileTool.SerializeWrite(savaPath, mainData);
    }

    private static void SortData(XCTaskData mainData)
    {
        if (mainData != null)
        {
            mainData.SortEvents();

            if (mainData.subDatas != null)
            {
                foreach (var item in mainData.subDatas)
                {
                    SortData(item);
                }
            }
        }
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
        else if (HasOverrideToXCEvent(eventType))
        {
            _track.Events.ForEach((e) => taskData._events.Add(e.ToXCEvent()));
        }
        else
        {
            Debug.LogWarning($"no eventType {eventType}");
        }

        //else if (eventType == typeof(FObjectEvent)){}
        //else if (eventType == typeof(FTriggerRangeEvent)){}

        //排序 _events
        //taskData.SortEvents();  

    }

    private static bool HasOverrideToXCEvent(Type fEventType)
    {
        BindingFlags all = (BindingFlags)~BindingFlags.Default;
        MethodInfo baseMethod = typeof(FEvent).GetMethod("ToXCEvent", all);
        MethodInfo derivedMethod = fEventType.GetMethod("ToXCEvent", all);

        if (baseMethod.MetadataToken == derivedMethod.MetadataToken)
        {
            return false;
        }
        else
        {
            Debug.Log($"--- {fEventType}");
            return true;
        }
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



    private static ObjectData MakeObjectData(FTrack _track)
    {
        if (_track.Owner == null)
        {
            throw new Exception($"--- {_track.name} owner is null ");
        }
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_track.Owner);

        if (!path.StartsWith(XCPathConfig.PerfabDir))
        {
            FileInfo info = new FileInfo(path);

            string newPath = Path.Combine(XCPathConfig.GetSkillPrefabDir(fSeqSetting.raceId), info.Name);

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
        string path = $"{PathTool.GetUpperDir(Application.dataPath)}/Tools/gen_data.bat";
        var msg = CommandHelper.ExecuteBatCommand(path);
        if (msg.Contains("== succ =="))
        {
            Debug.Log("Luban succ!");
            LubanTables.Inst.Reset();
        }
        else
        {
            Debug.LogError(msg);
        }
    }

    [MenuItem(LoadLubanExcelCodeName)]
    public static void LoadLubanExcelWithCode()
    {
        string path = $"{PathTool.GetUpperDir(Application.dataPath)}/Tools/gen_code_data.bat";
        var msg = CommandHelper.ExecuteBatCommand(path);
        if (msg.Contains("bye~"))
        {
            Debug.Log("Luban succ!");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError(msg);
        }
    }


}