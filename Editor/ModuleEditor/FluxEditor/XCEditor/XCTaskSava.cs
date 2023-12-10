using Flux;
using FluxEditor;
using OdinSerializer;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XiaoCao;
using SerializationUtility = OdinSerializer.SerializationUtility;

public class XCTaskSava
{
    public static XCSeqSetting fSeqSetting;
    public static void GetData()
    {
        var editor = FSequenceEditorWindow.instance.GetSequenceEditor();
        FSequence Sequence = editor.Sequence;
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
        foreach (var _timeline in Sequence.Containers[0].Timelines)
        {
            bool isMain = timelineIndex == 0;

            foreach (var _track in _timeline.Tracks)
            {
                //对于disactive的轨道不保存
                //if (_track.enabled)
                ReadTrack(_track, isMain);
            }
        }

    }



    [MenuItem("XiaoCao/Flux/Sava")]
    private static void Sava()
    {
        //XCObjectEvent objectEvent = new XCObjectEvent();
        //objectEvent.eName = "Sequence";
        //string filePath = "Assets/_Res/Player/1.data";

        //byte[] bytes = SerializationUtility.SerializeValue(objectEvent, DataFormat.Binary);
        //FileTool.WriteToFile(bytes, filePath, true);
        //File.WriteAllBytes(filePath, bytes);
    }
    [MenuItem("XiaoCao/Flux/Read")]
    private static void Read()
    {
        string filePath = "Assets/_Res/SkillData/1.data";
        byte[] bytes = File.ReadAllBytes(filePath);
        XCEvent data = OdinSerializer.SerializationUtility.DeserializeValue<XCEvent>(bytes, DataFormat.Binary);



        data.GetType().Name.LogStr();

        LogObjectTool.LogObjectAll(data, typeof(XCEvent));

        //OdinSerializer.SerializationUtility.DeserializeValueWeak(data);
    }


    private static void ReadTrack(FTrack _track, bool isMain)
    {
        XCTaskData taskData = new XCTaskData();

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



    }

}