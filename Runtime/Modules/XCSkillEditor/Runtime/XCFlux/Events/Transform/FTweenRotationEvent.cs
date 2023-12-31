﻿using UnityEngine;
using XiaoCao;

namespace Flux
{
	[FEvent("Transform/Tween Rotation", typeof(FTransformTrack))]
	public class FTweenRotationEvent : FTransformEvent
	{
		private Quaternion _startRotation;

		protected override void OnTrigger( float timeSinceTrigger )
		{
			_startRotation = Owner.localRotation;
			base.OnTrigger(timeSinceTrigger);
		}

		protected override void SetDefaultValues ()
		{
			_tween = new FTweenVector3( Vector3.zero, new Vector3(0, 360, 0) );
		}

		protected override void ApplyProperty( float t )
		{
//			Owner.localRotation = _tween.GetValue( t );
			Owner.localRotation = Quaternion.Euler( _tween.GetValue( t ) );
		}

		protected override void OnStop ()
		{
			Owner.localRotation = _startRotation;
		}

        public override XCEvent ToXCEvent()
        {
			var xce = new XCRotateEvent();
			var fe = this;
            xce.range = new XCRange(fe.Start, fe.End);
            xce.startVec = fe.Tween.From;
            xce.endVec = fe.Tween.To;
            xce.easeType = fe.Tween.EasingType.FEaseToEase();
			return xce;
        }

    }
}
