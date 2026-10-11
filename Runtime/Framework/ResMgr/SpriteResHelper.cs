using System.Drawing;
using System.Linq;
using UnityEngine;
using XiaoCao;

public static class SpriteResHelper
{
    public static string DefaultItemSpritePath = "Assets/_Res/Sprite/UIIcon/help.png";

    public static Sprite LoadSkillIcon(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return null;
        }

        string path = XCPathConfig.GetSkillIconPath(skillName);
        string pathDefault = XCPathConfig.GetSkillIconPath("0");
        return ResMgr.LoadAseetOrDefault<Sprite>(path, pathDefault);
    }

    public static Sprite GetItemSprite(this Item item)
    {
        if (item.type == ItemType.Buff)
        {
            var so = RunTimePoolMgr.Inst.staticResSoUsing.buffSpriteSo;
            return BuffItem.Create(item).GetBuffSprite();
        }
        else if (item.type == ItemType.HolyRelic)
        {
            //Relic/Weapon_4_30 -> Weapon_4_30
            string relicShortName = item.typeId.RemoveHead("Relic/");

            relicShortName = relicShortName.Remove(relicShortName.LastIndexOf("_"));

            Sprite relic = ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/Relic/{relicShortName}.png");
            if (relic)
            {
                return relic;
            }

            relicShortName = relicShortName.Remove(relicShortName.LastIndexOf("_"));

            return ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/Relic/{relicShortName}.png");
        }
        else if (item.type == ItemType.ExtraItem && item.typeId == BattleExtraItemType.SupportRole)
        {
            Sprite supportRoleIcon = ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/ItemIcon/{item.typeId}.png");
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
                //Crystal_A     独立图片: Assets/_Res/Sprite/ItemIcon/Crystal_A.png
                //Crystal_B_1   图集 Crystal_B.png 中的子图

                var strArray = item.typeId.Split("_");

                string shortName = $"Crystal_{strArray[1]}";
                string longName = item.typeId;

                return GetCrystalSprite(shortName, longName);
            }
        }

        string endPath = $"Assets/_Res/Sprite/ItemIcon/{item.typeId}.png";
        return ResMgr.LoadAseetOrDefault<Sprite>(endPath, DefaultItemSpritePath);
    }

    /// <summary>
    /// 读取结晶图标: 先取独立图片(Crystal_A_1.png), 再取图集(Crystal_B.png)里的同名子图, 最后退回图集整图
    /// </summary>
    private static Sprite GetCrystalSprite(string shortName, string longName)
    {
        string dirPath = "Assets/_Res/Sprite/ItemIcon/";
        string longPath = $"{dirPath}{longName}.png";
        string shortPath = $"{dirPath}{shortName}.png";

        if (ResMgr.Loader.CheckLocationValid(longPath))
        {
            return ResMgr.LoadAseet<Sprite>(longPath);
        }

        Sprite subSprite = ResMgr.LoadSubAseet<Sprite>(shortPath, longName);
        if (subSprite)
        {
            return subSprite;
        }

        return ResMgr.LoadAseetOrDefault<Sprite>(shortPath, DefaultItemSpritePath);
    }


    public static Sprite LoadRoleIcon(string roleKey)
    {
        return ResMgr.LoadAseet<Sprite>(XCPathConfig.GetRoleTexturePath(roleKey));
    }
    
    public static Sprite LoadEmblemBuffIcon(string buffName)
    {
        return ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/EmblemBuffIcon/{buffName}.png");
    }

    public static Sprite LoadBuffStateUIIcon(string buffName)
    {
        return ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/ItemIcon/{buffName}.png");
    }
}
