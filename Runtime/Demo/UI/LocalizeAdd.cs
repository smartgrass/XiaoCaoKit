using XiaoCao.UI;

namespace XiaoCaoKit.UI
{
    public class LocalizeAdd : Localizer
    {
        public string addStr;

        public override void SetLocalize(string key)
        {
            base.SetLocalize(key);
            Text.text += addStr;
        }
    }
}