using System.Collections.Generic;
using UnityEditor;

namespace RotaryHeart.Lib
{
    /// <summary>
    /// Class used to handle defines for the assets
    /// </summary>
    public class Definer
    {
        /// <summary>
        /// Applies the defines to the script symbols
        /// </summary>
        /// <param name="defines">List of defines to add</param>
        public static void ApplyDefines(List<string> defines)
        {
            if (defines == null || defines.Count == 0)
            {
                return;
            }

            List<string> definesSplit = new List<string>(GetDefines());

            foreach (string define in defines)
            {
                if (!definesSplit.Contains(define))
                {
                    definesSplit.Add(define);
                }
            }

            ApplyDefine(string.Join(";", definesSplit.ToArray()));
        }

        /// <summary>
        /// Removes the defines from the script symbols
        /// </summary>
        /// <param name="defines">List of defines to remove</param>
        public static void RemoveDefines(List<string> defines)
        {
            if (defines == null || defines.Count == 0)
                return;

            List<string> definesSplit = new List<string>(GetDefines());

            foreach (string define in defines)
            {
                definesSplit.Remove(define);
            }

            ApplyDefine(string.Join(";", definesSplit.ToArray()));
        }

        /// <summary>
        /// Returns true if a define is already defined
        /// </summary>
        /// <param name="define">Define to check</param>
        public static bool ContainsDefine(string define)
        {
            if (string.IsNullOrEmpty(define))
            {
                return false;
            }

            List<string> definesSplit = new List<string>(GetDefines());

            return definesSplit.Contains(define);
        }

        /// <summary>
        /// Returns the array of defines
        /// </summary>
        private static string[] GetDefines()
        {
#if UNITY_6000_0_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedGroup = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string availableDefines = PlayerSettings.GetScriptingDefineSymbols(namedGroup);
#else
            string availableDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            return availableDefines.Split(';');
        }

        /// <summary>
        /// Actual logic that applies the defines symbols
        /// </summary>
        /// <param name="define">List of defines to save, this includes the already defined ones</param>
        private static void ApplyDefine(string define)
        {
            if (string.IsNullOrEmpty(define))
            {
                return;
            }

#if UNITY_6000_0_OR_NEWER
            UnityEditor.Build.NamedBuildTarget namedGroup = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(namedGroup, define);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, define);
#endif
        }
    }

}