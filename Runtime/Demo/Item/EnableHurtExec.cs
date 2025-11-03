namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class EnableHurtExec : MonoExecute
    {
        public bool isNoHurt;

        public bool checkChild;

        public override void Execute()
        {
            if (checkChild)
            {
                var childItems = transform.GetComponentsInChildren<ItemIdComponent>();
                foreach (var item in childItems)
                {
                    item.noHurt = isNoHurt;
                }
            }
            else
            {
                if (transform.TryGetComponent(out ItemIdComponent item))
                {
                    item.noHurt = isNoHurt;
                }
            }
        }
    }
}