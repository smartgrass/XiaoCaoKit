namespace XiaoCao
{
    public class XCAudioEvent: XCEvent {

        public override void OnTrigger(float startOffsetTime)
        {
            base.OnTrigger(startOffsetTime);

            SoundMgr.Inst.PlayClip(eName);
        }

    }

}