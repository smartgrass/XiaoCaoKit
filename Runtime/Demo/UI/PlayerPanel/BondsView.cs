using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XiaoCao.UI
{
    /// <summary>
    /// 羁绊详情界面: 与主控角色区(UnFight)位置重叠, 打开时展示当前羁绊等级、经验进度与总效果。
    /// 三项子等级(攻击/生存/技巧)只作为换算过程, 玩家只能看到羁绊等级;
    /// 羁绊经验靠通关关卡积累, 界面内不提供主动升级, 预制体上的"提升"按钮保持关闭。
    /// </summary>
    public class BondsView : EnableShowUI
    {
        /// <summary>标题栏文本, 显示"羁绊 LvN"</summary>
        public TMP_Text bondsTitleText;

        /// <summary>总效果文本, 依次列出攻击继承 / 生命继承 / 技能解锁</summary>
        public TMP_Text effectText;

        /// <summary>羁绊经验条</summary>
        public ExpBar expBar;

        /// <summary>返回按钮</summary>
        public Button closeBtn;

        /// <summary>关闭通知, 界面显隐由拥有者(RoleTabView)接管</summary>
        public Action CloseAct;

        private void Awake()
        {
            closeBtn.onClick.AddListener(OnCloseBtnClick);
        }

        /// <summary>
        /// 按当前羁绊等级刷新标题、经验条与总效果。
        /// </summary>
        public override void UpdateUI()
        {
            int bondLevel = SupportRoleSetting.GetBondLevel();
            bondsTitleText.text = LocalizeKey.FormatWithArgs(LocalizeKey.BondsTitleLevel, bondLevel);
            effectText.text = GetEffectText();

            PlayerSaveData saveData = PlayerSaveData.LocalSavaData;
            expBar.SetExp(saveData.bondExp, SupportRoleSetting.GetNextLevelExp(bondLevel));
        }

        /// <summary>
        /// 拼装总效果描述: 攻击、生命按当前继承比例, 技巧按已解锁技能数, 技巧满级时追加技能等级加成。
        /// </summary>
        private string GetEffectText()
        {
            float atkRate = SupportRoleSetting.GetAtkRate();
            float hpRate = SupportRoleSetting.GetHpRate();
            int skillCount = SupportRoleSetting.GetUnlockSkillCount();
            int skillLevelAdd = SupportRoleSetting.GetSkillLevelAdd();

            StringBuilder text = new StringBuilder();
            text.AppendLine(LocalizeKey.FormatWithArgs(LocalizeKey.BondsEffectAtk, atkRate.ToString("P0")));
            text.AppendLine(LocalizeKey.FormatWithArgs(LocalizeKey.BondsEffectHp, hpRate.ToString("P0")));
            text.Append(skillLevelAdd > 0
                ? LocalizeKey.FormatWithArgs(LocalizeKey.BondsEffectSkillLevelUp, skillCount, skillLevelAdd)
                : LocalizeKey.FormatWithArgs(LocalizeKey.BondsEffectSkill, skillCount));

            return text.ToString();
        }

        private void OnCloseBtnClick()
        {
            CloseAct?.Invoke();
        }
    }
}
