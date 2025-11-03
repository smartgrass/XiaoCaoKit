using System.Collections.Generic;
using TMPro;

namespace XiaoCao
{
    public class PlayerShowPanel : SubPanel
    {
        public PlayerShowView view;
        private PlayerAttr PlayerAttr => GameDataCommon.LocalPlayer.PlayerAttr;

        public Dictionary<string, TMP_Text> dic;

        public override void Init()
        {
            //显示lv,mp
            view = gameObject.GetComponent<PlayerShowView>();
            view.panel = this;
            dic = new Dictionary<string, TMP_Text>();
            dic["Lv"] = AddTextText("Lv", LvStrGetter());
            dic["Hp"] = AddTextText("Hp", HpStrGetter());
            dic["Atk"] = AddTextText("Atk", AtkStrGetter());
            dic["Crit"] = AddTextText("Crit", CritStrGetter());
            dic["Def"] = AddTextText("Def", LvStrGetter());

            view.Init();
        }


        public override void RefreshUI()
        {
            view.RefreshUI();

            SetTextValue("Lv", LvStrGetter());
            SetTextValue("Hp", HpStrGetter());
            SetTextValue("Atk", AtkStrGetter());
            SetTextValue("Def", LvStrGetter());
            SetTextValue("Crit", CritStrGetter());
        }

        void SetTextValue(string key, string str)
        {
            dic[key].text = str;
        }

        private string LvStrGetter()
        {
            return PlayerAttr.lv.ToString();
        }

        private string AtkStrGetter()
        {
            return PlayerAttr.Atk.ToString();
        }        
        private string CritStrGetter()
        {
            return PlayerAttr.Crit.ToString("P");
        }
        private string DefStrGetter()
        {
            return PlayerAttr.Def.ToString();
        }

        private string HpStrGetter()
        {
            return $"{(int)PlayerAttr.hp}/{(int)PlayerAttr.MaxHp}";
        }
    }
}