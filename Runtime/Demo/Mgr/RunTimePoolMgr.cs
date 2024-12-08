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
        


        internal GameObject GetHitEffect(int hitEffect)
        {
            string path = XCPathConfig.GetHitEffectPath(hitEffect);
            return PoolMgr.Inst.Get(path, 3);
        }
    }
}