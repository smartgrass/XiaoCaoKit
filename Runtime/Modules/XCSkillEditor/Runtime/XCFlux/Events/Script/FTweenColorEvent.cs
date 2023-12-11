using UnityEngine;
using System;
using System.Reflection;

namespace Flux
{
	[FEvent(UnUsing+"Script/Tween Color")]
	public class FTweenColorEvent : FTweenVariableEvent<FTweenColor>  {

		protected override void SetDefaultValues ()
		{
			_tween = new FTweenColor(new Color(1, 1, 1, 0), new Color(1, 1, 1, 1));
		}

		protected override object GetValueAt( float t )
		{
			return _tween.GetValue( t );
		}
	}
}
