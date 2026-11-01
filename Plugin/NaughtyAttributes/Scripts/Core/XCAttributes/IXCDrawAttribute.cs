
using System;
using UnityEditor;

namespace XiaoCao
{
    /// <summary>
    /// 特性在字段面板中的绘制阶段, 由 NaughtyInspector 按阶段统一调度, 不再依赖特性在数组中的位置
    /// </summary>
    public enum XCDrawStage
    {
        /// <summary>独占整行, 在水平行打开之前绘制(如标题), 传 null 的 action 调用</summary>
        Before,

        /// <summary>打开一个水平行, 之后的字段会画在同一行里</summary>
        BeginRow,

        /// <summary>关闭水平行</summary>
        EndRow,

        /// <summary>行内控件(如按钮), 紧跟在字段本体之后绘制</summary>
        Inline,

        /// <summary>不在逐字段循环里绘制, 由其他流程(如分组)负责</summary>
        Skip,
    }

    /// <summary>
    ///  AllowMultiple = false
    /// </summary>
    public interface IXCDrawAttribute
    {
        //是否需要开头结尾对称
        bool IsStartAndEnd { get => false; }

        /// <summary>绘制阶段, 默认作为行内控件处理</summary>
        XCDrawStage Stage { get => XCDrawStage.Inline; }

        void OnDraw(UnityEngine.Object targetObject, Action action);
    }
}
