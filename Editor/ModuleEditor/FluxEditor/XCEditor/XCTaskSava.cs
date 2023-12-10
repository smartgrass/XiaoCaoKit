using DG.Tweening;
using Flux;
using FluxEditor;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;
using XiaoCao;
using SerializationUtility = OdinSerializer.SerializationUtility;

public class XCTaskSava
{
    public const string SavaAllSeqName = "XiaoCao/Flux/SavaAllSeq";
    public const string ReadSkillDataName = "Assets/XiaoCao/ReadSkillData";

    public static XCSeqSetting fSeqSetting;

    public static void SavaCurSeq()
    {
        var editor = FSequenceEditorWindow.instance.GetSequenceEditor();
        FSequence Sequence = editor.Sequence;
        if (Sequence == null )
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

        Transform playerTF = Sequence.Containers[0].Timelines[0].transform;

        List<SkillEventData> skillEvents = new List<SkillEventData>();
        int timelineIndex = 0;
        XCTaskData mainData = null;
        foreach (var _timeline in Sequence.Containers[0].Timelines)
        {
            bool isMain = timelineIndex == 0;
            //一个Object分配一个XCTaskData
            XCTaskData data = new XCTaskData();
            foreach (var _track in _timeline.Tracks)
            {
                //对于disactive的轨道不保存
                //if (_track.enabled)
                ReadTrack(_track, data, isMain);
            }
            if (isMain)
            {
                mainData = data;
            }
            else
            {
                mainData.AddSubData(data);
            }
        }

        string savaPath = XCSetting.GetSkillDataPath(fSeqSetting.type, fSeqSetting.index, Sequence._skillId);
        Debug.Log($"FLog sava skill{Sequence._skillId} to {savaPath}");
        byte[] bytes = SerializationUtility.SerializeValue(mainData, DataFormat.Binary);
        FileTool.WriteToFile(bytes, savaPath, true);
        File.WriteAllBytes(savaPath, bytes);
    }


    [MenuItem(SavaAllSeqName)]
    private static void Sava()
    {
        var Scene = SceneManager.GetSceneByName("SkillEditor");
        GameObject root = Scene.GetRootGameObjects().First( (o)=> o.name == "Editor");

        var seqs = root.GetComponentsInChildren<FSequence>(true);
        seqs.LogListStr();

    }

    [MenuItem(ReadSkillDataName)]
    private static void Read()
    {
        if (!AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".data"))
        {
            Debug.Log($"yns no .data file {AssetDatabase.GetAssetPath(Selection.activeObject)}");
            return;
        }
        string filePath = AssetDatabase.GetAssetPath(Selection.activeObject);

        byte[] bytes = File.ReadAllBytes(filePath);
        XCTaskData data = OdinSerializer.SerializationUtility.DeserializeValue<XCTaskData>(bytes, DataFormat.Binary);

        LogObjectTool.LogObjectAll(data, typeof(XCTaskData));

        //OdinSerializer.SerializationUtility.DeserializeValueWeak(data);
    }


    private static XCTaskData ReadTrack(FTrack _track, XCTaskData taskData, bool isMain)
    {
        if (!isMain)
        {
            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_track.Owner);

            if (!path.StartsWith(XCSetting.PerfabDir))
            {
                FileInfo info = new FileInfo(path);

                string newPath = Path.Combine(XCSetting.PerfabDir, fSeqSetting.type.ToString() + fSeqSetting.index, info.Name);

                Debug.LogWarning($"FLog {info.Name} MoveTo {newPath}");

                AssetDatabase.MoveAsset(path, newPath);

                path = newPath;
            }


            ObjectData objectData = new ObjectData();
            taskData.objectData = objectData;
            objectData.ObjectPath = path;
            objectData.scale = _track.Owner.localScale;
            objectData.eulerAngle = _track.Owner.localEulerAngles;
            objectData.position = _track.Owner.localPosition;
            objectData.transfromType = _track.transfromType;
        }


        return taskData;
    }

}