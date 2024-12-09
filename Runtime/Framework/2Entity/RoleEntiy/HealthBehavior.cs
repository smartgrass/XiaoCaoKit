namespace XiaoCao
{
    public class HealthBehavior : BehaviorEntity, IHealth
    {
        public virtual int Hp { get; set; }
        public virtual int MaxHp { get; set; }

        public virtual bool IsDie => Hp <= 0;

        public virtual void OnDie(AtkInfo atkInfo)
        {
        }
    }

    public interface IHealth
    {
        int Hp { get; set; }
        int MaxHp { get; set; }
        bool IsDie { get; }
        void OnDie(AtkInfo atkInfo);
    }
}