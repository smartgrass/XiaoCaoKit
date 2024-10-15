using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoCao.UI;

namespace XiaoCao
{
    public class SkillDetalUI : MonoBehaviour
    {
        //名称,描述 全部使用实时拼接模式
        public TextMeshProUGUI titleText;//lv在在
        public TextMeshProUGUI desText;
        
        
        public Button upgradeBtn;
        public Button colseBtn; //区域外关闭
        


        public int SkillId {  get;  set; }
        //ObjPool
        //List<ItemUI> 显示所需材料

        public void SetSkillInfo(int skillId)
        {
            SkillId = skillId;
        }


        public void SetTitle()
        {
            int lv = 1;
            //满级
            titleText.text = $"{LocalizeKey.GetSkillNameKey(SkillId).ToLocalizeStr()} lv{lv}";
            desText.text = LocalizeKey.GetSkillDesc(SkillId,lv);


        }


        //描述是固定的,但效果由数值拼接
        //如"跳起,在空中快速进行4次斩击" +
        //扩展描述:靠配置决定几个样式
        //"分别造成攻击120%*3+160%的伤害" (可先忽略不写)


    }

}