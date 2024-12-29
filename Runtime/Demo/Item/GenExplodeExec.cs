using UnityEngine;
using XiaoCao;

public class GenExplodeExec : MonoExecute
{
    public GameObject prefab;

    public AudioClip clip;

    public override void Execute()
    {
        GameObject obj = PoolMgr.Inst.GetOrCreatPrefabPool(prefab).Get();
        obj.SetActive(true);
        obj.transform.position = transform.position;
        if (clip != null)
        {
            SoundMgr.Inst.PlayClip(clip);
        }
    }
}
