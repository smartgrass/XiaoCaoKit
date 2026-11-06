using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCaoEditor
{
    /// <summary>
    /// 规避 Unity 6000.4.0 ~ 6000.4.4 编辑器缺陷 UUM-139557：
    /// 域重载时若 Inspector 正显示含 Image / Text 等 Graphic 的对象，PropertyEditor+Styles
    /// 静态构造会因 EditorStyles.toolbarButtonRight 取空抛 NullReferenceException，
    /// 随后 Styles 类型初始化失败，Inspector 大面积变黑，只能重启编辑器。
    /// 做法：在脚本编译、域重载、进入 Play 模式之前，把选中集合里的 Graphic 对象摘掉。
    /// 官方已在 6000.4.5f1 修复，升级编辑器后可直接删除本脚本。
    /// </summary>
    [InitializeOnLoad]
    public static class GraphicSelectionReloadGuard
    {
        static GraphicSelectionReloadGuard()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnCompilationStarted(object context)
        {
            DropGraphicSelection("脚本编译");
        }

        private static void OnBeforeAssemblyReload()
        {
            DropGraphicSelection("域重载");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                DropGraphicSelection("进入 Play 模式");
            }
        }

        /// <summary>
        /// 剔除选中集合中的 Graphic 相关对象（Image / RawImage / Text / TMP 等），
        /// 其余选中保持不动；若全部被剔除则清空选中。
        /// </summary>
        private static void DropGraphicSelection(string reason)
        {
            Object[] selection = Selection.objects;
            if (selection == null || selection.Length == 0)
            {
                return;
            }

            List<Object> remain = new List<Object>(selection.Length);
            bool dropped = false;
            foreach (Object obj in selection)
            {
                if (IsGraphicOwner(obj))
                {
                    dropped = true;
                    continue;
                }
                remain.Add(obj);
            }

            if (!dropped)
            {
                return;
            }

            Selection.objects = remain.ToArray();
            Debug.Log($"[GraphicSelectionReloadGuard] {reason}前已取消 Graphic 选中，规避 UUM-139557 引发的 Inspector 变黑。");
        }

        /// <summary>
        /// 判断该对象在 Inspector 中是否会把 Graphic 组件画出来：
        /// 直接选中 Image/Text 组件本身，或选中了挂着 Graphic 的 GameObject。
        /// </summary>
        private static bool IsGraphicOwner(Object obj)
        {
            if (obj is Graphic)
            {
                return true;
            }

            if (obj is GameObject go)
            {
                return go.GetComponent<Graphic>() != null;
            }

            return false;
        }
    }
}
