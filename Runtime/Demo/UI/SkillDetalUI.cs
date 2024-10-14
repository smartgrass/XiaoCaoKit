using TMPro;
using UnityEngine;

namespace XiaoCao
{
    public class SkillDetalUI : MonoBehaviour
    {
        //名称,描述 全部使用实时拼接模式
        public TextMeshProUGUI titleText;//lv在在
        public TextMeshProUGUI desText;


        public int SkillId {  get;  set; }
        //ObjPool
        //List<ItemUI> 所需材料

        public void SetSkillInfo(int skillId)
        {
            SkillId = skillId;
        }


        public void SetTitle()
        {
            int lv = 1;
            //满级

        }


        //描述是固定的,但效果由数值拼接
        //如"跳起,在空中快速进行4次斩击" +
        //扩展描述:靠配置决定几个样式
        //"分别造成攻击120%*3+160%的伤害" (可先忽略不写)


    }

}