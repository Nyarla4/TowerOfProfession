using status;
using System;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IMovable, IHealable
{
    protected EntityStatDataSO _baseDataSO;
    public RuntimeStat Stat { get; protected set; }

    public bool IsFullHealth => Stat.CurrentHealth >= Stat.FinalMaxHealth;

    protected bool _isDead;

    public event Action<Entity> OnAttacked;
    public event Action<Entity, float> OnDamaged;
    public event Action<float> OnHeal;
    public event Action OnDead;

    public virtual void Initialize(EntityStatDataSO baseData)
    {
        _baseDataSO = baseData;
        Stat = new RuntimeStat(_baseDataSO);
        _isDead = false;
    }

    public void Attack(Entity defender)
    {
        if (defender == null || defender == this)
        {
            Debug.LogWarning("Entity_Attack: defender 누락 혹은 defender가 본인임");
            return;
        }

        if (_isDead)
            return;

        defender.TakeDamage(this, Stat.FinalAtk);
        OnAttacked?.Invoke(defender);
    }

    public void TakeDamage(Entity attacker, float rawDamage)
    {
        if (attacker == null || attacker == this)
        {
            Debug.LogWarning("Entity_TakeDamage: attacker 누락 혹은 attacker가 본인임");
            return;
        }

        if (_isDead) return;
        if (Stat.IsInvincible) return; // ← 추가

        float damage = Mathf.Max(0, rawDamage - Stat.FinalDef);

        Stat.ChangeHealth(-damage);
        OnDamaged?.Invoke(attacker, damage);

        if (Stat.CurrentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        if (_isDead)
            return;
        _isDead = true;
        OnDead?.Invoke();
    }

    public void TakeHeal(float amount)
    {
        if (IsFullHealth)
            return;
        Stat.ChangeHealth(amount);
        OnHeal?.Invoke(amount);
    }

    public virtual void Move(Vector2 dir)
    {

    }

    public virtual void Move(float dirX, float dirY)
    {

    }

    protected void InvokeOnAttacked(Entity defender)
    {
        OnAttacked?.Invoke(defender);
    }
}