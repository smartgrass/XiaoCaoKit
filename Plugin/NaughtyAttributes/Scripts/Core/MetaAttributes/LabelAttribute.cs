using System;
using UnityEngine;

namespace NaughtyAttributes
{
	/// <summary>
	/// 字段注释
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class LabelAttribute : MetaAttribute
	{
		public string label;

		public string getValue;

		public LabelAttribute(string label, string getValue = "")
		{
			this.label = label;
			this.getValue = getValue;
		}
	}
}
