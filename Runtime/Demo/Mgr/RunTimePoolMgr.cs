using System;
using UnityEngine;
using XiaoCao;
namespace XiaoCao
{
    /// <summary>
    /// 封装业务对象池
    /// PoolMgr做为底层代码, 一般不新增内容
    /// </summary>
    public class RunTimePoolMgr : Singleton<RunTimePoolMgr>
    {
        public StaticResSoUsing staticResSoUsing;

        protected override void Init()
        {
            base.Init();
            staticResSoUsing = ResMgr.LoadAseet<StaticResSoUsing>("Assets/_Res/UI/Setting/StaticResSoUsing.asset");
        }

        internal GameObject GetHitEffect(string hitEffect)
        {
            string path = XCPathConfig.GetHitEffectPath(hitEffect);
            GameObject hit = PoolMgr.Inst.Get(path, 3);
            return hit;
        }
    }
}