namespace XiaoCao
{
    public class HealthBehavior : BehaviorEntity, IHealth
    {
        public int Hp { get; set; }
        public int MaxHp { get; set; }

        public bool IsAlive => Hp > 0;

        public virtual void OnDie()
        {
        }
    }

    public interface IHealth
    {
        int Hp { get; set; }
        int MaxHp { get; set; }
        bool IsAlive { get; }
        void OnDie();
    }
}