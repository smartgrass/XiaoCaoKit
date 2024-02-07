using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace XiaoCao
{
    public class BattleView : MonoBehaviour
    {
        public Dictionary<int, HpBar> barDic = new Dictionary<int, HpBar>();

        public GameObject prefab;

        private AssetPool pool;


        public void Init()
        {
            pool = new AssetPool(prefab);
            GameEvent.AddEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int, RoleChangeType>(EventType.RoleChange.Int(), OnEntityChange);
        }

        private void Update()
        {
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
                    if (role.IsDie && !role.HasTag(RoleTagCommon.NoHpBar) && role.IsRuning)
                    {
                        barDic.TryGetValue(role.id, out var bar);
                        if (bar != null)
                        {
                            GameObject newBarGo = pool.Get();
                            bar = newBarGo.GetComponent<HpBar>();
                            bar.Init(role.RoleType);
                            barDic[role.id] = bar;
                        }
                        bar.UpdateHealthBar(role.Hp / (float)role.MaxHp);
                        bar.UpdatePostion(role.gameObject);
                    }
                }
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


