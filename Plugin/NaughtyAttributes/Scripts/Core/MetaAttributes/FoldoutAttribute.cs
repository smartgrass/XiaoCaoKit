using System;
using XiaoCao;

namespace NaughtyAttributes
{
    //与其他layout无法同时使用, 如HorLayoutAttribute
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class FoldoutAttribute : MetaAttribute, IGroupAttribute, IXCDrawAttribute
	{
		public string Name { get; private set; }

		public FoldoutAttribute(string name)
		{
			Name = name;
		}

        //与其他layout无法同时使用, 如HorLayoutAttribute
        /// <summary>[Foldout] 字段统一由分组流程绘制, 逐字段循环里跳过</summary>
        public XCDrawStage Stage => XCDrawStage.Skip;

        public void OnDraw(UnityEngine.Object targetObject, Action action)
        {
        }
    }
}
