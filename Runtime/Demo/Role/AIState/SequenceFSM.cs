using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{

    /// <summary>
    /// 顺序池
    /// </summary>
    [CreateAssetMenu(fileName = "SequenceFSM", menuName = "SO/AI/SequenceFSM",order =10)]  
    public class SequenceFSM : MainFSM
    {
        //顺序保证执行,
        public List<AIFSMBase> subFsms;

        public List<AIFSMBase> SubFsmsInst { get; set; }

        public int Index { get; private set; }

        public override void OnStart()
        {
            FristLoad();
            Index = 0;
            State = FSMState.Update;

            foreach (var fs in SubFsmsInst)
            {
                fs.ResetFSM();
            }
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
            State = FSMState.Finish;
        }
    }
}
