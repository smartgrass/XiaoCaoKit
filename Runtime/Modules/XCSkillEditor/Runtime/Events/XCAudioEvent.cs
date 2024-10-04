namespace XiaoCao
{
    public class XCAudioEvent: XCEvent {

        public float volume;

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);
            if (isPlayerOnly && !task.IsPlayer)
            {
                return;
            }
            SoundMgr.Inst.PlayClip(eName, volume);
        }

    }

}