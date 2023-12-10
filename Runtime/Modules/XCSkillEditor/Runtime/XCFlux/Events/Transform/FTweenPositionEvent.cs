using UnityEngine;

namespace Flux
{
	[FEvent("Transform/Tween Position", typeof(FTransformTrack))]
	public class FTweenPositionEvent : FTransformEvent 
	{
		private Vector3 _startPosition;

		protected override void OnTrigger( float timeSinceTrigger )
		{
			_startPosition = Owner.localPosition;
			base.OnTrigger( timeSinceTrigger );
		}

		protected override void OnStop()
		{
			base.OnStop();
			Owner.localPosition = _startPosition;
		}

		protected override void SetDefaultValues()
		{
			_tween = new FTweenVector3( Vector3.zero, Vector3.forward );
		}

		protected override void ApplyProperty( float t )
		{
			Owner.localPosition = _tween.GetValue( t );
		}
	}
}
