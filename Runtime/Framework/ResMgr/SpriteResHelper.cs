using UnityEngine;
using XiaoCao;

/// <summary>
/// 精灵图标统一入口: 一律按图集内精灵名取图(ResMgr.AtlasSprite), 名字就是图片文件名去掉扩展名
/// </summary>
public static class SpriteResHelper
{
    /// <summary>兜底图标的图集名</summary>
    public const string DefaultItemSpriteName = "help";

    /// <summary>兜底图标路径, 仅编辑器未打包(AssetDatabase)时使用</summary>
    public const string DefaultItemSpritePath = "Assets/_Res/Sprite/UIIcon/help.png";

    /// <summary>
    /// 按技能 Id 取技能图标, 图集内名字为 skill_{id}, 缺失时用 skill_0 兜底
    /// </summary>
    public static Sprite LoadSkillIcon(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        Sprite icon = ResMgr.AtlasSprite(XCPathConfig.GetSkillIconName(skillId));
        return icon != null ? icon : ResMgr.AtlasSprite(XCPathConfig.GetSkillIconName("0"));
    }

    /// <summary>
    /// 按物品取图标, 遗物/支援角色/结晶有各自的命名规则
    /// </summary>
    public static Sprite GetItemSprite(this Item item)
    {
        if (item.type == ItemType.Buff)
        {
            return BuffItem.Create(item).GetBuffSprite();
        }
        else if (item.type == ItemType.HolyRelic)
        {
            //Relic/Weapon_4_30 -> Weapon_4_30 -> Weapon_4
            string relicShortName = item.typeId.RemoveHead("Relic/");

            relicShortName = relicShortName.Remove(relicShortName.LastIndexOf("_"));

            Sprite relic = ResMgr.AtlasSprite(relicShortName);
            if (relic != null)
            {
                return relic;
            }

            relicShortName = relicShortName.Remove(relicShortName.LastIndexOf("_"));

            return ResMgr.AtlasSprite(relicShortName);
        }
        else if (item.type == ItemType.ExtraItem && item.typeId == BattleExtraItemType.SupportRole)
        {
            Sprite supportRoleIcon = ResMgr.AtlasSprite(item.typeId);
            if (supportRoleIcon != null)
            {
                return supportRoleIcon;
            }

            return LoadRoleIcon(BattleExtraItemHelper.GetSupportRoleKey());
        }
        else if (item.type == ItemType.Consumable)
        {
            if (item.typeId.StartsWith(nameof(EConsumableSubType.Crystal)))
            {
                //Crystal_A    独立图片, 图集名 Crystal_A
                //Crystal_B_1  Crystal_B 的子图, 图集名 Crystal_B_1

                var strArray = item.typeId.Split("_");

                string oreName = $"Crystal_{strArray[1]}";
                return GetCrystalSprite(item.typeId, oreName);
            }
        }

        Sprite sprite = ResMgr.AtlasSprite(item.typeId);
        return sprite != null ? sprite : ResMgr.AtlasSprite(DefaultItemSpriteName);
    }

    /// <summary>
    /// 读取结晶图标: 先按完整 id(Crystal_B_1) 取, 再按矿石类型(Crystal_A) 取, 最后兜底
    /// </summary>
    private static Sprite GetCrystalSprite(string typeId, string oreName)
    {
        Sprite sprite = ResMgr.AtlasSprite(typeId);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = ResMgr.AtlasSprite(oreName);
        return sprite != null ? sprite : ResMgr.AtlasSprite(DefaultItemSpriteName);
    }

    /// <summary>
    /// 按角色 Id 取立绘/头像, 图集名就是 roleKey
    /// </summary>
    public static Sprite LoadRoleIcon(string roleKey)
    {
        return ResMgr.AtlasSprite(roleKey);
    }

    /// <summary>
    /// 取纹章 Buff 图标
    /// </summary>
    public static Sprite LoadEmblemBuffIcon(string buffName)
    {
        return ResMgr.AtlasSprite(buffName);
    }

    /// <summary>
    /// 取 Buff 状态栏图标
    /// </summary>
    public static Sprite LoadBuffStateUIIcon(string buffName)
    {
        return ResMgr.AtlasSprite(buffName);
    }
}
