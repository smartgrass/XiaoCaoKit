namespace Flux
{
	/**
	 * @brief Base of transform property changes.
	 * @sa FPositionEvent, FRotationEvent, FScaleEvent
	 */
	public abstract class FTransformEvent : FTweenEvent<FTweenVector3>
	{
        protected override void OnFrameRangeChanged(FrameRange oldFrameRange)
        {
			float CurrentTime = Sequence.CurrentTime;
			Sequence.SetCurrentTimeEditor(0);
			Sequence.SetCurrentTimeEditor(CurrentTime);
		}
    }
}
