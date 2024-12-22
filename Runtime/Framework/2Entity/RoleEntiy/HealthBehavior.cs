namespace XiaoCao
{
    public class HealthBehavior : BehaviorEntity, IHealth
    {
        public virtual int Hp { get; set; }
        public virtual float MaxHp { get; set; }

        public virtual bool IsDie => Hp <= 0;

        public virtual void OnDie(AtkInfo atkInfo)
        {
        }
    }

    public interface IHealth
    {
        int Hp { get; set; }
        float MaxHp { get; set; }
        bool IsDie { get; }
        void OnDie(AtkInfo atkInfo);
    }
}