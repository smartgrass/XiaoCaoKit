#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;
 
namespace XiaoCaoEditor
{
    /// <summary>
    /// 按角色 raid 读取并展示角色相关资源引用。
    /// </summary>
    public class RoleReferenceWindow : EditorWindow
    {
        private const string MenuPath = "XiaoCao/资源工具/角色资源引用窗口";
        private const string WindowTitle = "角色资源引用窗口";
        private const string IdRoleDir = "Assets/_Res/Role/IdRole";
        private const string AttrSettingSoPath = "Assets/XiaoCaoKit/Resources/AttrSettingSo.asset";

        private string raid = "";
        private Vector2 scrollPosition;
        private bool hasRead;
        private string statusMessage = "输入 raid 后点击读取。";
        private MessageType statusType = MessageType.Info;

        private ReferenceItem animatorControllerItem;
        private ReferenceItem attrSettingSoItem;
        private readonly List<RoleReferenceItem> roleItems = new List<RoleReferenceItem>();

        private sealed class ReferenceItem
        {
            public string Label;
            public string Path;
            public Object Asset;
            public string Note;
            public MessageType NoteType;
        }

        private sealed class RoleReferenceItem
        {
            public ReferenceItem IdRole;
            public ReferenceItem Body;
            public ReferenceItem AiFSM;
            public string BodyName;
            public string AiId;
        }

        /// <summary>
        /// 打开角色引用查看窗口。
        /// </summary>
        [MenuItem(MenuPath, priority = 131)]
        public static void Open()
        {
            RoleReferenceWindow window = GetWindow<RoleReferenceWindow>(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        /// <summary>
        /// 打开角色引用查看窗口，并按指定 raid 自动读取资源引用。
        /// </summary>
        public static void Open(string raid)
        {
            RoleReferenceWindow window = GetWindow<RoleReferenceWindow>(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.raid = raid == null ? "" : raid.Trim();
            window.hasRead = false;

            if (!string.IsNullOrEmpty(window.raid))
            {
                window.ReadReferences();
            }

            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(560f, 420f);
        }

        private void OnGUI()
        {
            DrawSearchBar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (hasRead)
            {
                DrawReferences();
            }
            else
            {
                EditorGUILayout.HelpBox("读取后会显示动画机、IdRole、Body、AIFSM 和共用配置引用。", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制 raid 输入和读取按钮。
        /// </summary>
        private void DrawSearchBar()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("raid", GUILayout.Width(36f));

                EditorGUI.BeginChangeCheck();
                string nextRaid = EditorGUILayout.TextField(raid, GUILayout.MinWidth(120f));
                if (EditorGUI.EndChangeCheck())
                {
                    raid = nextRaid.Trim();
                    hasRead = false;
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(raid)))
                {
                    if (GUILayout.Button("读取", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    {
                        ReadReferences();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// 绘制当前 raid 读取到的全部引用。
        /// </summary>
        private void DrawReferences()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("独立引用", EditorStyles.boldLabel);
            DrawReferenceItem(animatorControllerItem);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(string.Format("IdRole 匹配结果 ({0})", roleItems.Count), EditorStyles.boldLabel);
            if (roleItems.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的 IdRole prefab。", MessageType.Warning);
            }

            for (int i = 0; i < roleItems.Count; i++)
            {
                DrawRoleReferenceItem(roleItems[i], i);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("共用配置引用", EditorStyles.boldLabel);
            DrawReferenceItem(attrSettingSoItem);
        }

        /// <summary>
        /// 绘制单个 IdRole 以及由它派生出的 Body 和 AIFSM 引用。
        /// </summary>
        private void DrawRoleReferenceItem(RoleReferenceItem roleItem, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(string.Format("IdRole #{0}", index + 1), EditorStyles.boldLabel);
                DrawReferenceItem(roleItem.IdRole);

                if (!string.IsNullOrEmpty(roleItem.BodyName))
                {
                    EditorGUILayout.LabelField("BodyName", roleItem.BodyName, EditorStyles.miniLabel);
                }

                DrawReferenceItem(roleItem.Body);

                if (!string.IsNullOrEmpty(roleItem.AiId))
                {
                    EditorGUILayout.LabelField("aiId", roleItem.AiId, EditorStyles.miniLabel);
                }

                DrawReferenceItem(roleItem.AiFSM);
            }
        }

        /// <summary>
        /// 绘制资源对象和缺失提示。
        /// </summary>
        private static void DrawReferenceItem(ReferenceItem item)
        {
            if (item == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                item.Asset = EditorGUILayout.ObjectField(item.Label, item.Asset, typeof(Object), false);
            }

            if (!string.IsNullOrEmpty(item.Note))
            {
                EditorGUILayout.HelpBox(item.Note, item.NoteType);
            }
        }

        /// <summary>
        /// 根据 raid 读取动画机、IdRole、Body、AIFSM 和共用配置。
        /// </summary>
        private void ReadReferences()
        {
            ClearReferences();

            string currentRaid = raid.Trim();
            animatorControllerItem = LoadReference(
                "动画机",
                XCPathConfig.GetAnimatorControllerPath(currentRaid),
                typeof(AnimatorController));

            string[] idRolePaths = FindIdRolePaths(currentRaid);
            for (int i = 0; i < idRolePaths.Length; i++)
            {
                roleItems.Add(ReadRoleReference(idRolePaths[i]));
            }

            attrSettingSoItem = LoadReference(
                "AttrSettingSo",
                AttrSettingSoPath,
                typeof(AttrSettingSo));

            hasRead = true;
            if (roleItems.Count == 0)
            {
                statusMessage = string.Format("读取完成，但没有找到 IdRole：{0}/E_{1}_*.prefab", IdRoleDir, currentRaid);
                statusType = MessageType.Warning;
            }
            else
            {
                statusMessage = string.Format("读取完成，找到 {0} 个 IdRole。", roleItems.Count);
                statusType = MessageType.Info;
            }
        }

        /// <summary>
        /// 从 IdRole prefab 读取 Body 和 AIFSM 引用。
        /// </summary>
        private static RoleReferenceItem ReadRoleReference(string idRolePath)
        {
            GameObject idRolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(idRolePath);
            RoleReferenceItem roleItem = new RoleReferenceItem
            {
                IdRole = CreateReferenceItem("IdRole", idRolePath, idRolePrefab)
            };

            if (idRolePrefab == null)
            {
                roleItem.IdRole.Note = "IdRole prefab 加载失败。";
                roleItem.IdRole.NoteType = MessageType.Error;
                return roleItem;
            }

            global::IdRole idRole = idRolePrefab.GetComponentInChildren<global::IdRole>(true);
            if (idRole == null)
            {
                roleItem.IdRole.Note = "Prefab 内没有 IdRole 组件。";
                roleItem.IdRole.NoteType = MessageType.Warning;
                roleItem.Body = CreateMissingReferenceItem("Body", "");
                roleItem.AiFSM = CreateMissingReferenceItem("AIFSM", "");
                return roleItem;
            }

            roleItem.BodyName = idRole.bodyName;
            string bodyPath = string.IsNullOrEmpty(idRole.bodyName)
                ? ""
                : XCPathConfig.GetRoleBodyPath(idRole.bodyName);
            roleItem.Body = LoadReference("Body", bodyPath, typeof(GameObject));

            global::AddEnemyData addEnemyData = idRole.gameObject.GetComponent<global::AddEnemyData>();
            if (addEnemyData == null)
            {
                roleItem.AiFSM = CreateMissingReferenceItem("AIFSM", "");
                roleItem.AiFSM.Note = "IdRole 所在 GameObject 没有 AddEnemyData 组件。";
                roleItem.AiFSM.NoteType = MessageType.Warning;
                return roleItem;
            }

            roleItem.AiId = addEnemyData.aiId;
            string aiPath = XCPathConfig.GetAIPath(addEnemyData.aiId);
            roleItem.AiFSM = LoadReference("AIFSM", aiPath, typeof(ActPoolFSM));
            if (roleItem.AiFSM.Asset == null && addEnemyData.debugAi != null)
            {
                roleItem.AiFSM.Asset = addEnemyData.debugAi;
                roleItem.AiFSM.Path = AssetDatabase.GetAssetPath(addEnemyData.debugAi);
                roleItem.AiFSM.Note = "路径加载失败，已显示 prefab 上现有的 debugAi 引用。";
                roleItem.AiFSM.NoteType = MessageType.Warning;
            }

            return roleItem;
        }

        /// <summary>
        /// 查找匹配 E_{raid}_*.prefab 的 IdRole 资源，兼容 E_{raid}.prefab。
        /// </summary>
        private static string[] FindIdRolePaths(string currentRaid)
        {
            string prefix = "E_" + currentRaid + "_";
            string exactFileName = "E_" + currentRaid + ".prefab";
            string[] guids = AssetDatabase.FindAssets("E_" + currentRaid + " t:Prefab", new[] { IdRoleDir });
            List<string> paths = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                bool isMatch = fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, exactFileName, StringComparison.OrdinalIgnoreCase);
                if (isMatch && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths.ToArray();
        }

        private static ReferenceItem LoadReference(string label, string path, Type assetType)
        {
            if (string.IsNullOrEmpty(path))
            {
                return CreateMissingReferenceItem(label, path);
            }

            Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
            ReferenceItem item = CreateReferenceItem(label, path, asset);
            if (asset == null)
            {
                item.Note = "未找到资源。";
                item.NoteType = MessageType.Warning;
            }

            return item;
        }

        private static ReferenceItem CreateReferenceItem(string label, string path, Object asset)
        {
            return new ReferenceItem
            {
                Label = label,
                Path = path,
                Asset = asset,
                NoteType = MessageType.Info
            };
        }

        private static ReferenceItem CreateMissingReferenceItem(string label, string path)
        {
            return new ReferenceItem
            {
                Label = label,
                Path = path,
                Note = string.IsNullOrEmpty(path) ? "没有可读取的路径。" : "未找到资源。",
                NoteType = MessageType.Warning
            };
        }

        private void ClearReferences()
        {
            roleItems.Clear();
            animatorControllerItem = null;
            attrSettingSoItem = null;
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using XiaoCao;
using Object = UnityEngine.Object;

namespace XiaoCaoEditor
{
    /// <summary>
    /// 按角色 raid 读取并展示角色相关资源引用。
    /// </summary>
    public class RoleReferenceWindow : EditorWindow
    {
        private const string MenuPath = "XiaoCao/资源工具/角色资源引用窗口";
        private const string WindowTitle = "角色资源引用窗口";
        private const string IdRoleDir = "Assets/_Res/Role/IdRole";
        private const string AttrSettingSoPath = "Assets/XiaoCaoKit/Resources/AttrSettingSo.asset";

        private string raid = "";
        private Vector2 scrollPosition;
        private bool hasRead;
        private string statusMessage = "输入 raid 后点击读取。";
        private MessageType statusType = MessageType.Info;

        private ReferenceItem animatorControllerItem;
        private ReferenceItem attrSettingSoItem;
        private readonly List<RoleReferenceItem> roleItems = new List<RoleReferenceItem>();

        private sealed class ReferenceItem
        {
            public string Label;
            public string Path;
            public Object Asset;
            public string Note;
            public MessageType NoteType;
        }

        private sealed class RoleReferenceItem
        {
            public ReferenceItem IdRole;
            public ReferenceItem Body;
            public ReferenceItem AiFSM;
            public string BodyName;
            public string AiId;
        }

        /// <summary>
        /// 打开角色引用查看窗口。
        /// </summary>
        [MenuItem(MenuPath, priority = 131)]
        public static void Open()
        {
            RoleReferenceWindow window = GetWindow<RoleReferenceWindow>(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        /// <summary>
        /// 打开角色引用查看窗口，并按指定 raid 自动读取资源引用。
        /// </summary>
        public static void Open(string raid)
        {
            RoleReferenceWindow window = GetWindow<RoleReferenceWindow>(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.raid = raid == null ? "" : raid.Trim();
            window.hasRead = false;

            if (!string.IsNullOrEmpty(window.raid))
            {
                window.ReadReferences();
            }

            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = new Vector2(560f, 420f);
        }

        private void OnGUI()
        {
            DrawSearchBar();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (hasRead)
            {
                DrawReferences();
            }
            else
            {
                EditorGUILayout.HelpBox("读取后会显示动画机、IdRole、Body、AIFSM 和共用配置引用。", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制 raid 输入和读取按钮。
        /// </summary>
        private void DrawSearchBar()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("raid", GUILayout.Width(36f));

                EditorGUI.BeginChangeCheck();
                string nextRaid = EditorGUILayout.TextField(raid, GUILayout.MinWidth(120f));
                if (EditorGUI.EndChangeCheck())
                {
                    raid = nextRaid.Trim();
                    hasRead = false;
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(raid)))
                {
                    if (GUILayout.Button("读取", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    {
                        ReadReferences();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// 绘制当前 raid 读取到的全部引用。
        /// </summary>
        private void DrawReferences()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("独立引用", EditorStyles.boldLabel);
            DrawReferenceItem(animatorControllerItem);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(string.Format("IdRole 匹配结果 ({0})", roleItems.Count), EditorStyles.boldLabel);
            if (roleItems.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的 IdRole prefab。", MessageType.Warning);
            }

            for (int i = 0; i < roleItems.Count; i++)
            {
                DrawRoleReferenceItem(roleItems[i], i);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("共用配置引用", EditorStyles.boldLabel);
            DrawReferenceItem(attrSettingSoItem);
        }

        /// <summary>
        /// 绘制单个 IdRole 以及由它派生出的 Body 和 AIFSM 引用。
        /// </summary>
        private void DrawRoleReferenceItem(RoleReferenceItem roleItem, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(string.Format("IdRole #{0}", index + 1), EditorStyles.boldLabel);
                DrawReferenceItem(roleItem.IdRole);

                if (!string.IsNullOrEmpty(roleItem.BodyName))
                {
                    EditorGUILayout.LabelField("BodyName", roleItem.BodyName, EditorStyles.miniLabel);
                }

                DrawReferenceItem(roleItem.Body);

                if (!string.IsNullOrEmpty(roleItem.AiId))
                {
                    EditorGUILayout.LabelField("aiId", roleItem.AiId, EditorStyles.miniLabel);
                }

                DrawReferenceItem(roleItem.AiFSM);
            }
        }

        /// <summary>
        /// 绘制资源对象和缺失提示。
        /// </summary>
        private static void DrawReferenceItem(ReferenceItem item)
        {
            if (item == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                item.Asset = EditorGUILayout.ObjectField(item.Label, item.Asset, typeof(Object), false);
            }

            if (!string.IsNullOrEmpty(item.Note))
            {
                EditorGUILayout.HelpBox(item.Note, item.NoteType);
            }
        }

        /// <summary>
        /// 根据 raid 读取动画机、IdRole、Body、AIFSM 和共用配置。
        /// </summary>
        private void ReadReferences()
        {
            ClearReferences();

            string currentRaid = raid.Trim();
            animatorControllerItem = LoadReference(
                "动画机",
                XCPathConfig.GetAnimatorControllerPath(currentRaid),
                typeof(AnimatorController));

            string[] idRolePaths = FindIdRolePaths(currentRaid);
            for (int i = 0; i < idRolePaths.Length; i++)
            {
                roleItems.Add(ReadRoleReference(idRolePaths[i]));
            }

            attrSettingSoItem = LoadReference(
                "AttrSettingSo",
                AttrSettingSoPath,
                typeof(AttrSettingSo));

            hasRead = true;
            if (roleItems.Count == 0)
            {
                statusMessage = string.Format("读取完成，但没有找到 IdRole：{0}/E_{1}_*.prefab", IdRoleDir, currentRaid);
                statusType = MessageType.Warning;
            }
            else
            {
                statusMessage = string.Format("读取完成，找到 {0} 个 IdRole。", roleItems.Count);
                statusType = MessageType.Info;
            }
        }

        /// <summary>
        /// 从 IdRole prefab 读取 Body 和 AIFSM 引用。
        /// </summary>
        private static RoleReferenceItem ReadRoleReference(string idRolePath)
        {
            GameObject idRolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(idRolePath);
            RoleReferenceItem roleItem = new RoleReferenceItem
            {
                IdRole = CreateReferenceItem("IdRole", idRolePath, idRolePrefab)
            };

            if (idRolePrefab == null)
            {
                roleItem.IdRole.Note = "IdRole prefab 加载失败。";
                roleItem.IdRole.NoteType = MessageType.Error;
                return roleItem;
            }

            global::IdRole idRole = idRolePrefab.GetComponentInChildren<global::IdRole>(true);
            if (idRole == null)
            {
                roleItem.IdRole.Note = "Prefab 内没有 IdRole 组件。";
                roleItem.IdRole.NoteType = MessageType.Warning;
                roleItem.Body = CreateMissingReferenceItem("Body", "");
                roleItem.AiFSM = CreateMissingReferenceItem("AIFSM", "");
                return roleItem;
            }

            roleItem.BodyName = idRole.bodyName;
            string bodyPath = string.IsNullOrEmpty(idRole.bodyName)
                ? ""
                : XCPathConfig.GetRoleBodyPath(idRole.bodyName);
            roleItem.Body = LoadReference("Body", bodyPath, typeof(GameObject));

            global::AddEnemyData addEnemyData = idRole.gameObject.GetComponent<global::AddEnemyData>();
            if (addEnemyData == null)
            {
                roleItem.AiFSM = CreateMissingReferenceItem("AIFSM", "");
                roleItem.AiFSM.Note = "IdRole 所在 GameObject 没有 AddEnemyData 组件。";
                roleItem.AiFSM.NoteType = MessageType.Warning;
                return roleItem;
            }

            roleItem.AiId = addEnemyData.aiId;
            string aiPath = XCPathConfig.GetAIPath(addEnemyData.aiId);
            roleItem.AiFSM = LoadReference("AIFSM", aiPath, typeof(ActPoolFSM));
            if (roleItem.AiFSM.Asset == null && addEnemyData.debugAi != null)
            {
                roleItem.AiFSM.Asset = addEnemyData.debugAi;
                roleItem.AiFSM.Path = AssetDatabase.GetAssetPath(addEnemyData.debugAi);
                roleItem.AiFSM.Note = "路径加载失败，已显示 prefab 上现有的 debugAi 引用。";
                roleItem.AiFSM.NoteType = MessageType.Warning;
            }

            return roleItem;
        }

        /// <summary>
        /// 查找匹配 E_{raid}_*.prefab 的 IdRole 资源，兼容 E_{raid}.prefab。
        /// </summary>
        private static string[] FindIdRolePaths(string currentRaid)
        {
            string prefix = "E_" + currentRaid + "_";
            string exactFileName = "E_" + currentRaid + ".prefab";
            string[] guids = AssetDatabase.FindAssets("E_" + currentRaid + " t:Prefab", new[] { IdRoleDir });
            List<string> paths = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                bool isMatch = fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, exactFileName, StringComparison.OrdinalIgnoreCase);
                if (isMatch && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths.ToArray();
        }

        private static ReferenceItem LoadReference(string label, string path, Type assetType)
        {
            if (string.IsNullOrEmpty(path))
            {
                return CreateMissingReferenceItem(label, path);
            }

            Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
            ReferenceItem item = CreateReferenceItem(label, path, asset);
            if (asset == null)
            {
                item.Note = "未找到资源。";
                item.NoteType = MessageType.Warning;
            }

            return item;
        }

        private static ReferenceItem CreateReferenceItem(string label, string path, Object asset)
        {
            return new ReferenceItem
            {
                Label = label,
                Path = path,
                Asset = asset,
                NoteType = MessageType.Info
            };
        }

        private static ReferenceItem CreateMissingReferenceItem(string label, string path)
        {
            return new ReferenceItem
            {
                Label = label,
                Path = path,
                Note = string.IsNullOrEmpty(path) ? "没有可读取的路径。" : "未找到资源。",
                NoteType = MessageType.Warning
            };
        }

        private void ClearReferences()
        {
            roleItems.Clear();
            animatorControllerItem = null;
            attrSettingSoItem = null;
        }
    }
}
#endif
