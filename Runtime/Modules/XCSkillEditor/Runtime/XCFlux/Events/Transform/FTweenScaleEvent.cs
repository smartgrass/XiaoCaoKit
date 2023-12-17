using UnityEngine;
using XiaoCao;

namespace Flux
{
	[FEvent("Transform/Tween Scale", typeof(FTransformTrack))]
	public class FTweenScaleEvent : FTransformEvent
	{
		protected override void SetDefaultValues ()
		{
			_tween = new FTweenVector3( Vector3.zero, Vector3.one );
		}

		protected override void ApplyProperty( float t )
		{
			Owner.localScale = _tween.GetValue( t );
		}

        public override XCEvent ToXCEvent()
        {
            var xce = new XCScaleEvent();
            var fe = this;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.startVec = fe.Tween.From;
            xce.endVec = fe.Tween.To;
            xce.easeType = fe.Tween.EasingType.FEaseToEase();
            return xce;
        }
    }
}
