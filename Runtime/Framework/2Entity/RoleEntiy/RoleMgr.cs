using System.Collections.Generic;
using UnityEngine;

namespace XiaoCao
{
    public class RoleMgr : Singleton<RoleMgr>, IClearCache
    {
        public Dictionary<int, Role> roleDic = new Dictionary<int, Role>();


        //首先获取所有范围内敌人
        //获取最高分数
        //视觉范围为angle
        //动态边界, 角度越偏, 边界越短  limitDis = (angle,180) => (seeR,hearR)
        //距离越小分数越高 ds = 1/d  (d >0.1)
        //夹角越小分数越高 as = cos(x)
        //旧目标加分计算 暂无
        public Role SearchEnemyRole(Transform self, float seeR, float seeAngle, out float maxS, int team = TeamTag.Enemy)
        {
            float hearR = seeR * 0.4f;
            Role role = null;
            maxS = 0;
            foreach (var item in roleDic.Values)
            {
                if (item.team != team && !item.IsDie)
                {
                    GetAngleAndDistance(self, item.transform, out float curAngle, out float curDis);
                    
                    //动态边界, 角度越偏, 边界越短
                    float limitDis = MathTool.ValueMapping(curAngle, seeAngle, 180, seeR, hearR);
                    if (curDis > limitDis){
                        //超出距离的排除
                        continue;
                    }
                    
                    //距离越小 分数越高
                    float _ds = 1 / curDis;
                    //角度越小 分数越高
                    float _as = Mathf.Cos(curAngle / 2f * Mathf.Deg2Rad);
                    float score = _ds * _as;
                    
                    if (score > maxS)
                    {
                        maxS = score;
                        role = item;
                    }
                }
            }
            return role;
        }

        //计算两个物体正前方夹角 和距离
        private void GetAngleAndDistance(Transform self, Transform target, out float curAngle, out float curDis)
        {
            Vector3 dir = target.position - self.position;

            curAngle = Vector3.Angle(dir, target.forward);

            curDis = Mathf.Max(0.1f, dir.magnitude);
        }
    }

}