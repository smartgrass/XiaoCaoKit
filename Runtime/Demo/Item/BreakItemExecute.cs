namespace XiaoCaoKit.Runtime.Demo.Item
{
    public class BreakItemExecute : MonoExecute
    {
        public override void Execute()
        {
            var itemCrystal = transform.GetComponentsInChildren<ItemCrystal>();
            foreach (var crystal in itemCrystal)
            {
                if (!crystal.isDead)
                {
                    crystal.ToDead();
                }
            }
        }
    }
}