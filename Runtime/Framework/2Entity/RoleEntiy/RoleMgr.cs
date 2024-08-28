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
        //超过视觉范围 做插值剔除  maxDis = Mathf.Lerp(hearR, seeR, angleP);
        //距离越小分数越高 ds = 1/d  (d >0.1)
        //夹角越小分数越高 as = cos(x)
        //旧目标加分计算 暂无
        public Role SearchEnemyRole(Transform self, float seeR, float seeAngle, out float maxS, int team = TeamTag.Enemy)
        {
            float hearR = seeR * 0.4f;
            float angleP = 1;
            Role role = null;
            maxS = 0;
            foreach (var item in roleDic.Values)
            {
                if (item.team != team && !item.IsDie)
                {
                    GetAngleAndDistance(self, item.transform, out float curAngle, out float dis);
                    if (curAngle > seeAngle)
                    {
                        MathTool.ValueMapping(curAngle, seeAngle, 180, 1, 0);
                    }
                    float maxDis = Mathf.Lerp(hearR, seeR, angleP);
                    if (dis < maxDis)
                    {
                        float _ds = 1 / dis;
                        float _as = Mathf.Cos(curAngle / 2f * Mathf.Deg2Rad);
                        float end = _ds * _as;

                        //查找分数最高
                        if (end > maxS)
                        {
                            maxS = end;
                            role = item;
                        }
                    }
                }
            }
            return role;
        }

        //计算两个物体正前方夹角 和距离
        private void GetAngleAndDistance(Transform self, Transform target, out float curAngle, out float dis)
        {
            Vector3 dir = target.position - self.position;

            curAngle = Vector3.Angle(dir, target.forward);

            dis = Mathf.Max(0.1f, dir.magnitude);
        }


    }

}