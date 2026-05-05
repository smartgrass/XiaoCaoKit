namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class EnableHurtExec : MonoExecute
    {
        public bool isNoHurt;

        public bool autoDead;
        
        public bool checkChild;

        public override void Execute()
        {
            if (checkChild)
            {
                var childItems = transform.GetComponentsInChildren<ItemIdComponent>();
                foreach (var item in childItems)
                {
                    CheckItem(item);
                }
            }
            else
            {
                if (transform.TryGetComponent(out ItemIdComponent item))
                {
                    CheckItem(item);
                }
            }
        }

        private void CheckItem(ItemIdComponent item)
        {
            item.noHurt = isNoHurt;
            if (autoDead)
            {
                item.ToDead();
            }
        }
    }
}