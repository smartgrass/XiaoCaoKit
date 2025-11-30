using cfg;
using UnityEngine;
using XiaoCao;

/// <summary>
/// 一些攻击效果预设
/// </summary>
public static class HitHelper
{
    public static void ShowDamageText(Transform transform, int atk, AtkInfo atkInfo)
    {
        Vector3 textPos = transform.position;
        textPos.y = atkInfo.ackObjectPos.y;
        textPos = Vector3.Lerp(atkInfo.ackObjectPos, textPos, 0.8f);
        UIMgr.Inst.PlayDamageText(atk, transform.position);
        //TODO 文字颜色 , 大小
    }

    public static void ShowHitEffect(Transform transform, AtkInfo ackInfo)
    { 
        SkillSetting setting = ackInfo.GetSkillSetting;
        if (string.IsNullOrEmpty(setting.HitEffect))
        {
            return;
        }

        var effect = RunTimePoolMgr.Inst.GetHitEffect(setting.HitEffect);
        effect.SetActive(true);
        effect.transform.SetParent(transform, true);
        Vector3 tempAckObjectPos = ackInfo.ackObjectPos;
        tempAckObjectPos.y = ackInfo.hitPos.y;
        tempAckObjectPos = Vector3.Lerp(ackInfo.ackObjectPos, ackInfo.hitPos, 0.8f);
        effect.transform.position = tempAckObjectPos; //ackInfo.hitPos;
        effect.transform.forward = ackInfo.ackObjectPos - transform.position;
    }
}