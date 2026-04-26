using System.Drawing;
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

        if (item.type == ItemType.ExtraItem && item.typeId == BattleExtraItemType.SupportRole)
        {
            Sprite supportRoleIcon = ResMgr.LoadAseet<Sprite>($"Assets/_Res/Sprite/ItemIcon/{item.typeId}.png");
            if (supportRoleIcon != null)
            {
                return supportRoleIcon;
            }

            return LoadRoleIcon(BattleExtraItemHelper.GetSupportRoleKey());
        }

        string endPath = $"Assets/_Res/Sprite/ItemIcon/{item.typeId}.png";
        return ResMgr.LoadAseetOrDefault<Sprite>(endPath, DefaultItemSpritePath);
    }

    public static Sprite LoadRoleIcon(string roleKey)
    {
        return ResMgr.LoadAseet<Sprite>(XCPathConfig.GetRoleTexturePath(roleKey));
    }
}
