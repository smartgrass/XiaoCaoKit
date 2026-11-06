using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao;

namespace XiaoCao.UI
{
    public class ActiveSkillView : EnableShowUI
    {
        //所有技能
        public Transform allSkillParent;

        //已装备技能
        public Transform skillEquipParent;

        //prefab SkillItemCell
        public SkillDetailUI skillDetailUI;

        public static PlayerSaveData PlayerSaveData => GameAllData.playerSaveData;
        
        public Button editBtn;

        private Localizer editBtnText;

        public bool IsEditing { get; set; }

        private bool IsInit;

        private List<SkillItemCell> equipSkillCells;
        private List<SkillItemCell> allSkillCells;

        private void Awake()
        {
            editBtn.onClick.AddListener(() => { OnEditBtnClick(); });
            editBtnText = editBtn.transform.parent.GetComponentInChildren<Localizer>();
        }

        private void Start()
        {
            UICanvasMgr.Inst.EventSystem.AddEventListener(UIEventNames.SkillChange, UpdateUI);
        }

        private List<string> GetSkillIdList(List<SkillItemCell> cells)
        {
            return new List<string>(cells.ConvertAll(cell => cell.skillId));
        }
        
        public override void OnEnable()
        {
            //隐藏子界面 
            skillDetailUI.gameObject.SetActive(false);
            IsEditing = false;
            editBtnText.SetLocalize("Edit");
            UpdateUI();
        }

        public override void UpdateUI()
        {
            if (!ResMgr.IsLoadBaseFinish)
            {
                return;
            }

            //获取所有技能
            //PlayerSaveData.skillUnlockDic
            RefreshAllSkillState();
            RefreshEquipSkillCell();
            //保存技能配置
            equipSkillCells = new List<SkillItemCell>(skillEquipParent.GetComponentsInChildren<SkillItemCell>(false));
        }

        /// <summary>
        /// 刷新技能页显示，供新手引导主动重绘。
        /// </summary>
        public void RefreshGuideUI()
        {
            UpdateUI();
        }

        /// <summary>
        /// 获取技能详情面板实例。
        /// </summary>
        public SkillDetailUI GetSkillDetailUI()
        {
            return skillDetailUI;
        }

        /// <summary>
        /// 获取指定技能在技能总览中的格子。
        /// </summary>
        public SkillItemCell GetSkillCell(string skillId)
        {
            if (!IsInit || allSkillCells == null)
            {
                return null;
            }

            for (int i = 0; i < allSkillCells.Count; i++)
            {
                SkillItemCell cell = allSkillCells[i];
                if (cell != null && cell.skillId == skillId && cell.gameObject.activeInHierarchy)
                {
                    return cell;
                }
            }

            return null;
        }

        private void CheckLen(List<string> skillList)
        {
            int minLen = 6;
            if (skillList.Count < minLen)
            {
                skillList.AddRange(new string[minLen - skillList.Count]);
            }
        }


        public void SetCount(List<string> skillList, Transform parent, ESkillItemCellType cellType)
        {
            //获取技能数量
            int skillCount = skillList.Count;
            int childCount = parent.childCount;
            GameObject prefab = parent.GetChild(0).gameObject;
            int deltaCount = skillCount - childCount;
            if (deltaCount > 0)
            {
                for (int i = 0; i < deltaCount; i++)
                {
                    GameObject go = Instantiate(prefab, parent);
                    go.SetActive(true);
                }
            }


            childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject go = parent.GetChild(i).gameObject;
                if (i < skillCount)
                {
                    var cell = go.GetComponent<SkillItemCell>();
                    cell.ShowSkillUI(skillList[i], cellType);
                    cell.clickAct = () => { OnCellClick(cell); };
                }

                go.SetActive(i < skillCount);
            }
        }


        private void OnEditBtnClick()
        {
            IsEditing = !IsEditing;

            if (IsEditing)
            {
                editBtnText.SetLocalize("Save");
                RefreshAllSkillState();
            }
            else
            {
                PlayerSaveData.skillBarSetting = GetSkillIdList(equipSkillCells);
                editBtnText.SetLocalize("Edit");
                RefreshAllSkillState();
                PlayerSaveData.saveSkillBar = true;
                UICanvasMgr.Inst.EventSystem.SendEvent(UIEventNames.SkillChange);
                PlayerSaveData.SavaData();
            }
        }

        void OnCellClick(SkillItemCell cell)
        {
            var cellType = cell.cellType;
            if (cellType == ESkillItemCellType.SkillViewAll)
            {
                if (!IsEditing)
                {
                    ShowSkillDetail(cell.skillId);
                }
                else
                {
                    //快速装备 / 取消   
                    if (PlayerSaveData.skillBarSetting.Contains(cell.skillId))
                    {
                        UnEquipSkill(cell.skillId);
                    }
                    else
                    {
                        EquipSkill(cell.skillId);
                    }

                    RefreshAllSkillState();
                    RefreshEquipSkillCell();
                }
            }
            else if (cellType == ESkillItemCellType.SkillViewEquip)
            {
                if (IsEditing)
                {
                    //取消装备
                    UnEquipSkill(cell.skillId);

                    RefreshAllSkillState();
                    RefreshEquipSkillCell();
                }
            }
        }


        void ShowSkillDetail(string skillId)
        {
            skillDetailUI.Show(skillId);
        }

        void UnEquipSkill(string skillId)
        {
            PlayerSaveData.skillBarSetting.Remove(skillId);

            foreach (var cell in allSkillCells)
            {
                if (cell.skillId == skillId)
                {
                    cell.SetEquipState(false);
                }
            }
        }

        /// <summary>
        /// 把技能放进技能栏: 优先填第一个空位, 没有空位再追加到末尾;
        /// 已装备、未解锁或技能栏已满时不处理。
        /// </summary>
        public static void EquipSkill(string skillId)
        {
            //判断是是否解锁
            if (PlayerHelper.GetSkillLevel(skillId) <= 0)
            {
                return;
            }

            List<string> skillBar = PlayerSaveData.skillBarSetting;
            if (skillBar.Contains(skillId))
            {
                return;
            }

            //找第一个空位, 保持已装备技能的位置不变
            int emptyIndex = -1;
            for (int i = 0; i < skillBar.Count; i++)
            {
                if (string.IsNullOrEmpty(skillBar[i]))
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex >= 0)
            {
                skillBar[emptyIndex] = skillId;
            }
            else if (skillBar.Count < PlayerSaveData.MaxSkillBarSetting)
            {
                skillBar.Add(skillId);
            }
            else
            {
                //技能栏已满, 不覆盖玩家已排好的键位
                return;
            }

            //技能栏被改动过, 读档时不再被默认技能列表覆盖(否则新学会的技能重启就消失)
            PlayerSaveData.saveSkillBar = true;
        }

        void RefreshAllSkillState()
        {
            if (!IsInit)
            {
                var setting = Resources.Load<PlayerSkillSo>("PlayerSkillSo");
                var allSkill = setting.GetSkillList(0);
                SetCount(allSkill, allSkillParent, ESkillItemCellType.SkillViewAll);
                allSkillCells = new List<SkillItemCell>(allSkillParent.GetComponentsInChildren<SkillItemCell>(false));
                IsInit = true;
            }


            foreach (var cell in allSkillCells)
            {
                bool isEquip = PlayerSaveData.skillBarSetting.Contains(cell.skillId);

                cell.SetEquipState(isEquip && IsEditing);
                cell.UpdateUI();
            }
        }

        void RefreshEquipSkillCell()
        {
            CheckLen(PlayerSaveData.skillBarSetting);
            //根据skillList生成SkillItem, 并且多余的skillItem需要隐藏
            SetCount(PlayerSaveData.skillBarSetting, skillEquipParent, ESkillItemCellType.SkillViewEquip);
        }
    }
}
