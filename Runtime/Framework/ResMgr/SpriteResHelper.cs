using System.Drawing;
using UnityEngine;
using XiaoCao;

public static class SpriteResHelper
{
    public static string DefaultItemSpritePath = "Assets/_Res/Sprite/UIIcon/help.png";

    public static Sprite LoadSkillIcon(string skillName)
    {
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
            string path = $"Assets/_Res/Sprite/ItemIcon/{item.id}.png";
            ResMgr.LoadAseetOrDefault<Sprite>(path, DefaultItemSpritePath);
        }

        return null;
    }

    public static Sprite LoadRoleIcon(string roleKey)
    {
        return ResMgr.LoadAseet<Sprite>(XCPathConfig.GetRoleTexturePath(roleKey));
    }
}