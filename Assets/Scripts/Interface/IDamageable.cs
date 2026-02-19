using UnityEngine;

public interface IDamageable
{
    public void TakeDamage(Entity attacker, float rawDamage);

    public void Die();
}
