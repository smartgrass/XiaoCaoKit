using UnityEngine;
using XiaoCao;

public static class SpriteResHelper
{

    public static Sprite LoadSkillIcon(string skillName)
    {
        string path = XCPathConfig.GetSkillIconPath(skillName);
        string pathDefault = XCPathConfig.GetSkillIconPath("0");
        return ResMgr.LoadAseetOrDefault<Sprite>(path, pathDefault);
    }

}
