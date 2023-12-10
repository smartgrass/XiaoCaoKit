using UnityEngine;

namespace Flux
{
	[FEvent("Sequence/Pause Sequence")]
	public class FPauseSequenceEvent : FEvent 
	{
		private FSequence _sequence = null;
		
		protected override void OnInit()
		{
			_sequence = Owner.GetComponent<FSequence>();
		}
		
		protected override void OnTrigger( float timeSinceTrigger )
		{
			_sequence.Pause();
		}
	}
}
