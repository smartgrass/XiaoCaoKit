namespace XiaoCao
{
    /// <summary>
    /// 执行行为选择
    /// </summary>
    ///<see cref="ActPoolFSM"/>
    ///<see cref="SequenceFSM"/>
    public class MainFSM : AIFSMBase
    {
        public bool IsLoaded { get; set; }

        private void OnEnable()
        {
            IsLoaded = false;
        }
    }


}
