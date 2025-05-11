using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    /// <summary>
    /// 顺序池
    /// </summary>
    [CreateAssetMenu(fileName = "SequenceFSM", menuName = "SO/AI/SequenceFSM", order = 1)]
    public class SequenceFSM : MainFSM
    {
        //顺序保证执行,
        public List<AIFSMBase> subFsms;

        public List<AIFSMBase> SubFsmsInst { get; set; }

        public int Index { get; private set; }

        public override void OnStart()
        {
            FristLoad();
            foreach (var fs in SubFsmsInst)
            {
                fs.ResetFSM();
            }
            Index = 0;
            State = FSMState.Update;
        }

        private void FristLoad()
        {
            if (IsLoaded)
            {
                return;
            }
            IsLoaded = true;

            SubFsmsInst = new List<AIFSMBase>();
            foreach (var fs in subFsms)
            {
                var so = ScriptableObject.Instantiate(fs);
                so.name = fs.name;
                so.InitReset(control);
                SubFsmsInst.Add(so);
            }
        }

        public override void OnUpdate()
        {
            if (State == FSMState.None)
            {
                OnStart();
                return;
            }

            int count = SubFsmsInst.Count;
            if (Index < count)
            {
                var subFsm = SubFsmsInst[Index];
                if (subFsm.State != FSMState.Finish)
                {
                    subFsm.OnUpdate();
                }
                else
                {
                    Index++;
                }
            }
            else
            {
                OnExit();
            }
        }

        public override void OnExit()
        {
            Debug.Log($"--- SequenceFSM Finish");
            State = FSMState.Finish;
        }

        public override string GetStatePath()
        {
            var curState = GetCurState();
            if (curState == null)
            {
                return name;
            }
            string subPath = curState.GetStatePath();
            string path = $"{name}/{subPath}";
            return path;
        }

        private AIFSMBase GetCurState()
        {
            if (Index < SubFsmsInst.Count)
            {
                return SubFsmsInst[Index];
            }
            return null;
        }
    }
}
