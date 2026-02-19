using status;
using System;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IMovable
{
    protected EntityStatDataSO _baseDataSO;
    protected RuntimeStat _stat;
    protected bool _isDead;

    public event Action<Entity> OnAttacked;
    public event Action<Entity, float> OnDamaged;
    public event Action OnDead;

    public virtual void Initialize(EntityStatDataSO baseData)
    {
        _baseDataSO = baseData;
        _stat = new RuntimeStat(_baseDataSO);
        _isDead = false;
    }

    public void Attack(Entity defender)
    {
        if (defender == null || defender == this || _isDead)
            return;

        defender.TakeDamage(this, _stat.FinalAtk);
        OnAttacked?.Invoke(defender);
    }

    public void TakeDamage(Entity attacker, float rawDamage)
    {
        if (attacker == null || _isDead)
            return;

        float damage = rawDamage - _stat.FinalDef;
        if(damage > 0)
        {
            _stat.CurrentHealth -= damage;
            OnDamaged?.Invoke(attacker, damage);

            if (_stat.CurrentHealth <= 0)
            {
                _stat.CurrentHealth = 0;
                Die();
            }
        }
    }

    public void Die()
    {
        if (_isDead)
            return;
        _isDead = true;
        OnDead?.Invoke();
    }

    public virtual void Move(Vector2 dir)
    {

    }

    public virtual void Move(float dirX, float dirY)
    {

    }
}