using System.Collections.Generic;
using TEngine;
using UnityEngine;
using UnityEngine.Networking.Types;

namespace XiaoCao
{
    public class BattleHud : MonoBehaviour
    {
        public Dictionary<int, HpBar> barDic = new Dictionary<int, HpBar>();

        public GameObject worldHpBarPrefab;

        public Transform worldHpBarParent;

        public Transform playerBar;

        public Canvas worldCanvas;

        private AssetPool pool;

        public void Init()
        {
            pool = new AssetPool(worldHpBarPrefab);
            GameEvent.AddEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
            worldCanvas.worldCamera = Camera.main;
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }

        private void Update()
        {
            DebugGUI.Log("gameState",GameDataCommon.Current.gameState);
            if (GameDataCommon.Current.gameState == GameState.Running)
            {
                NorHpBarUpdate();
            }
        }
        //每帧都执行, 检查增加
        private void NorHpBarUpdate()
        {
            foreach (var item in RoleMgr.Inst.roleDic.Values)
            {
                Role role = item;
                if (null != role)
                {
                    if (role.IsPlayer)
                    {
                        UpdataPlayerHpBar(role);
                    }
                    else
                    {
                        UpdataEnemyHpBar(role);
                    }
                }
            }
        }
        private void UpdataPlayerHpBar(Role role)
        {
            if (role.IsRuning)
            {

            }
        }
        private void UpdataEnemyHpBar(Role role)
        {
            if (!role.IsDie && !role.HasTag(RoleTagCommon.NoHpBar) && role.IsRuning)
            {
                barDic.TryGetValue(role.id, out var bar);
                if (bar == null)
                {
                    GameObject newBarGo = pool.Get();
                    bar = newBarGo.GetComponent<HpBar>();
                    bar.Init(role.RoleType);
                    barDic[role.id] = bar;
                    bar.transform.SetParent(worldHpBarParent, false);
                    bar.SetParent(role);
                }
                DebugGUI.Log($"{role.id}", role.Hp, role.MaxHp);
                bar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                bar.UpdatePostion();
            }
        }




        //只有变动时才执行, 隐藏多余
        void OnEntityChange(int id, RoleChangeType roleChangeType)
        {
            if (roleChangeType == RoleChangeType.Add)
            {
                return;
            }

            foreach (var item in barDic)
            {
                if (!RoleMgr.Inst.roleDic.ContainsKey(item.Key))
                {
                    var bar = item.Value;
                    pool.Release(bar.gameObject);
                    barDic.Remove(item.Key);
                }
            }
        }

        private void HpBarUpdate()
        {

        }
    }
}


