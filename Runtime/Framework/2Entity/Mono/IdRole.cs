using DG.Tweening.Plugins.Core.PathCore;
using NaughtyAttributes;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XiaoCao;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class IdRole : IdComponent
{
    public int raceId = 1;

    [Dropdown(nameof(GetDirAllFileName))]
    public string bodyName = "Body_P_0";
    public string animControllerName = "";

    [XCLabel("重量, 影响击飞程度")]
    public float weight = 1;
    public RuntimeAnimatorController LoadRuntimeAc
    {
        get
        {
            string path = string.IsNullOrEmpty(animControllerName) ? XCPathConfig.GetAnimatorControllerPath(raceId.ToString()) :
                XCPathConfig.GetAnimatorControllerPath(animControllerName);

            var loadAc = ResMgr.LoadAseet(path) as RuntimeAnimatorController;
            if (loadAc != null)
            {
                return loadAc;
            }
            Debug.LogError($"--- no RuntimeAnimatorController {path}");
            return null;
        }
    }


    public Rigidbody rb;
    public CharacterController cc;
    public Collider[] triggerCols;



    public Vector3 hpBarOffset = Vector3.up;
    public MoveSettingSo moveSetting;

    private Transform _follow;

    private Transform _lookAt;

    private List<string> GetDirAllFileName()
    {
        return PathTool.GetDirAllFileName("Assets/_Res/Role/Body");
    }


    public Transform GetFollow
    {
        get
        {
            if (_follow == null)
            {
                _follow = new GameObject("Follow").transform;
                _follow.SetParent(transform);
                _follow.localPosition = moveSetting.CamFollewOffset;
            }
            return _follow;
        }
    }

    public Transform GetLookAt
    {
        get
        {
            if (_lookAt == null)
            {
                _lookAt = GetFollow;
            }
            return _lookAt;
        }
    }


    //Debug View"
    [ReadOnly]
    public Animator animator;


    private void OnDestroy()
    {
        //防止EntityMgr自身已被销毁
        if (EntityMgr.IsOn)
        {
            EntityMgr.Inst.RemoveEntity(this.GetEntity());
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        CheckData();
    }

    public void CheckData()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (cc == null)
            cc = GetComponent<CharacterController>();
        if (triggerCols.Length == 0)
        {
            List<Collider> colliders = new List<Collider>();
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent<Collider>(out Collider col))
                {
                    colliders.Add(col);
                }
            }
            triggerCols = colliders.ToArray();
        }
    }

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    void MoveToImmediate()
    {
        (GetEntity() as Role).Movement.MoveToImmediate(transform.position);
    }

#endif
}
