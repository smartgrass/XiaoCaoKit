using UnityEngine;
using XiaoCao;

public class TriggerEndExplode : MonoBehaviour, ITriggerEnd
{
    public GameObject prefab;

    public AudioClip audioClip;

    public void OnTriggerEnd(BaseAtker atker)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"--- TriggerEndExplose prefab null {name}");
            return;
        }

        GameObject instance = PoolMgr.Inst.GetOrCreatPrefabPool(prefab).Get();
        SoundMgr.Inst.PlayClip(audioClip);
        Transform instanceTran = instance.transform;
        instanceTran.SetPositionAndRotation(transform.position, transform.rotation);

        BaseAtker triggerAtker = instance.GetComponentInChildren<BaseAtker>();
        if (triggerAtker == null)
        {
            return;
        }

        AtkInfo info = GetAtkInfo(atker);
        if (info == null)
        {
            return;
        }

        triggerAtker.InitAtkInfo(info);
        if (triggerAtker is Atker colliderAtker)
        {
            colliderAtker.AddTriggerByCollider();
        }
    }

    private static AtkInfo GetAtkInfo(BaseAtker atker)
    {
        if (atker?.ackInfo == null)
        {
            Debug.LogWarning("--- TriggerEndExplose atker ackInfo null");
            return null;
        }

        AtkInfo source = atker.ackInfo;
        return new AtkInfo()
        {
            team = source.team,
            atker = source.atker,
            beAtker = source.beAtker,
            atk = source.atk,
            baseAtk = source.baseAtk,
            isCrit = source.isCrit,
            skillId = source.skillId,
            subSkillId = source.subSkillId,
        };
    }
}