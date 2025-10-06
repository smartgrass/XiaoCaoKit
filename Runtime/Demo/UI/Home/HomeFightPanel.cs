using System;
using NaughtyAttributes;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCaoKit
{
    public class HomeFightPanel : HomePanelBase
    {
        public GameObject linePrefab;
        public GameObject levelUIPrefab;
        public Transform lineParent;
        public Transform levelUIParent;
        public LevelDetailUI levelDetailUI;

        public Button backBtn;
        public Button enterBtn;

        public int curChapter = 0;
        public AssetPool levelBtnPool;

        public string SelectLevel
        {
            get => GameDataCommon.Current.selectLevel;
            set => GameDataCommon.Current.selectLevel = value;
        }

        public LevelUISettingSo levelUISettingSo;

        private void Awake()
        {
            levelBtnPool = new AssetPool(levelUIPrefab);
        }

        private void Start()
        {
            levelUISettingSo = ConfigMgr.LoadSoConfig<LevelUISettingSo>();
            LoadLevelView(0);
            backBtn.onClick.AddListener(() => { HomeHud.Inst.SwitchPanel(EHomePanel.MainPanel); });
            enterBtn.onClick.AddListener(() =>
            {
                if (string.IsNullOrEmpty(SelectLevel))
                {
                    return;
                }

                GameMgr.Inst.LoadLevelScene(SelectLevel);
            });
        }

        private void OnEnable()
        {
            levelDetailUI.Hide();
        }

        void LoadLevelView(int chapter)
        {
            curChapter = chapter;
            LevelUIInfo levelUIInfo = levelUISettingSo.GetOrDefault(chapter);

            // 清除现有的关卡UI
            foreach (Transform child in levelUIParent)
            {
                Destroy(child.gameObject);
            }

            // 清除现有的关卡UI
            foreach (Transform child in lineParent)
            {
                Destroy(child.gameObject);
            }

            // 根据posList生成对应数量的levelUIPrefab

            for (var i = 0; i < levelUIInfo.posList.Count; i++)
            {
                int temp = i + 1;
                var pos = levelUIInfo.posList[i];

                GameObject levelUIObj = levelBtnPool.Get();
                levelUIObj.transform.SetParent(levelUIParent);
                RectTransform rectTransform = levelUIObj.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = pos;
                LevelSelectUI levelSelectUI = levelUIObj.GetComponent<LevelSelectUI>();
                levelSelectUI.btn.onClick.RemoveAllListeners();
                levelSelectUI.btn.onClick.AddListener(() => { OnClickLevel(temp); });
                levelSelectUI.Show(curChapter, temp);
            }

            if (levelUIInfo.lines.Count > 0)
            {
                // 根据lines用linePrefab连线
                foreach (Vector2Int line in levelUIInfo.lines)
                {
                    Vector2 startPos = levelUIInfo.posList[line.x];
                    Vector2 endPos = levelUIInfo.posList[line.y];
                    DrawLine(startPos, endPos);
                }
            }
            else
            {
                // 若无连线配置, 则按顺序连接
                for (int i = 1; i < levelUIInfo.posList.Count; i++)
                {
                    Vector2 startPos = levelUIInfo.posList[i - 1];
                    Vector2 endPos = levelUIInfo.posList[i];
                    DrawLine(startPos, endPos);
                }
            }
        }

        private void OnClickLevel(int index)
        {
            SelectLevel = MapNames.GetLevelName(curChapter, index);
            levelDetailUI.Show();
        }

        /// <summary>
        /// 绘制连接线
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="endPos">结束位置</param>
        private void DrawLine(Vector2 startPos, Vector2 endPos)
        {
            GameObject lineObj = Instantiate(linePrefab, lineParent);
            RectTransform rectTransform = lineObj.GetComponent<RectTransform>();

            // 旋转位置连线
            Vector2 direction = endPos - startPos;
            float distance = direction.magnitude;
            Vector2 centerPos = (startPos + endPos) / 2;

            rectTransform.anchoredPosition = centerPos;

            float angle = direction.VectorToAngle();
            lineObj.transform.localRotation = Quaternion.Euler(0, 0, angle);

            // 假设linePrefab的默认长度为1，根据距离调整scale
            var rectTf = lineObj.GetComponent<RectTransform>();
            var sizeDelta = rectTf.sizeDelta;
            sizeDelta.x = distance;
            rectTf.sizeDelta = sizeDelta;
        }

        //保存当前坐标到配置文件
        [Button]
        public void SaveLevelPos()
        {
#if UNITY_EDITOR
            LevelUISettingSo so =
                AssetDatabase.LoadAssetAtPath<LevelUISettingSo>("Assets/XiaoCaoKit/Resources/LevelUISettingSo.asset");
            if (!so.map.ContainsKey(curChapter))
            {
                int newLen = so.array.Length + 1;
                var newArray = new LevelUIInfo[so.array.Length + 1];
                //数组扩容, 并保留原来数据
                Array.Copy(so.array, newArray, so.array.Length);
                newArray[newLen - 1] = new LevelUIInfo() { id = curChapter };
                so.array = newArray;
            }

            foreach (var levelUIInfo in so.array)
            {
                if (levelUIInfo.id == curChapter)
                {
                    // 清空旧的位置列表
                    levelUIInfo.posList.Clear();

                    // 遍历levelUIParent下的所有子对象，获取它们的位置
                    foreach (Transform child in levelUIParent)
                    {
                        RectTransform rectTransform = child.GetComponent<RectTransform>();
                        levelUIInfo.posList.Add(rectTransform.anchoredPosition);
                    }

                    break;
                }
            }


            // 在编辑器中保存配置文件
            UnityEditor.EditorUtility.SetDirty(so);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"关卡{curChapter}的坐标已保存");
#endif
        }
    }
}