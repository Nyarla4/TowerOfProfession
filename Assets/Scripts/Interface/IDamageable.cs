public interface IDamageable
{
    void TakeDamage(Entity attacker, float rawDamage);

    void Die();
}

public interface IHealable
{
    void TakeHeal(float amount);
    bool IsFullHealth { get; }
}