using System;
using System.Collections;
using System.Collections.Generic;
using status;
using UnityEngine;

/// <summary>
/// HTML game.js + combat.js 플레이어 파트 대응
/// </summary>
public class PlayerEntity : Entity
{
    // 스킬 로직 분리를 위한 전투 이벤트
    public event Action<Enemy, float> OnAfterAttackHit;     // 근접/광역 타격 성공 직후
    public event Action<Projectile> OnProjectileSpawned;    // 투사체 생성 직후

    // ─────────────────────────────────────────────
    // 외부 참조
    // ─────────────────────────────────────────────

    [SerializeField] private PlayerInput _input;

    // ─────────────────────────────────────────────
    // 직업/스킬 상태
    // ─────────────────────────────────────────────

    public JobDataSO CurrentJob { get; private set; }

    // ─────────────────────────────────────────────
    // 이동 상태
    // ─────────────────────────────────────────────

    /// <summary> 현재 이동 중 여부 (CONDITION 패시브 체크용) </summary>
    public bool IsMoving { get; private set; }

    /// <summary> 마을 내 여부 (app_p3 휴식의 지혜) </summary>
    public bool IsInTown { get; set; }

    // ─────────────────────────────────────────────
    // 회복 관련
    // ─────────────────────────────────────────────

    /// <summary> 회복량 배율 (app_p3, prs_p2) </summary>
    public float RegenMultiplier { get; set; } = 1f;

    private float _lastCombatTime;
    private const float REGEN_COMBAT_COOLDOWN = 5f; // 비전투 회복 대기시간
    private float _regenTimer;
    private const float REGEN_TICK = 1f;            // 회복 주기 (초)
    private const float TOWN_REGEN_TICK = 1f;       // 마을 회복 주기

    // ─────────────────────────────────────────────
    // 자동 공격
    // ─────────────────────────────────────────────

    private float _lastAttackTime;
    private int _attackCount;           // CriticalEvery 카운터
    private GameObject _projectilePrefab; // 현재 직업 투사체 프리팹 (ChangeJob에서 갱신)

    // ─────────────────────────────────────────────
    // 대시
    // ─────────────────────────────────────────────

    private bool _isDashing;

    /// <summary> 조이스틱 방향으로 대시 </summary>
    public void Dash(float distance)
    {
        if (_isDashing) return;
        StartCoroutine(DashCoroutine(GetMoveDir(), distance));
    }

    /// <summary> 조이스틱 반대 방향으로 대시 (후퇴사격, 후퇴난사) </summary>
    public void DashBackward(float distance)
    {
        if (_isDashing) return;
        StartCoroutine(DashCoroutine(-GetMoveDir(), distance));
    }

    private IEnumerator DashCoroutine(Vector2 dir, float distance)
    {
        _isDashing = true;
        float moved = 0f;
        float speed = distance / 0.15f; // 0.15초 안에 이동

        if (dir == Vector2.zero)
            dir = Vector2.down; // 방향 없으면 아래

        while (moved < distance)
        {
            float step = speed * Time.deltaTime;
            transform.Translate(dir * step);
            moved += step;
            yield return null;
        }
        _isDashing = false;
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    public override void Initialize(EntityStatDataSO data)
    {
        base.Initialize(data);
        CurrentJob = null; // Stat이 초기화되었으므로 기존 직업 보너스도 날아감
        ResetFlags();
    }

    public void ChangeJob(JobDataSO newJob)
    {
        if (newJob == null) return;
        if (CurrentJob == newJob) return; // 중복 전직 방지

        // 이전 직업 보너스/패시브 제거 (재전직 스탯 누적 방지)
        if (CurrentJob != null)
        {
            Stat.RemoveModifier(StatType.MaxHp, CurrentJob.BonusMaxHp);
            Stat.RemoveModifier(StatType.Attack, CurrentJob.BonusAtk);
            Stat.RemoveModifier(StatType.Defense, CurrentJob.BonusDef);
            Stat.RemoveModifier(StatType.MoveSpeed, CurrentJob.BonusMoveSpeed);
            Stat.RemoveModifier(StatType.Regen, CurrentJob.BonusRegen);
            Stat.RemoveModifier(StatType.AttackRange, CurrentJob.BonusRange);
        }

        CurrentJob = newJob;
        ResetFlags();

        // 새 직업 스탯 보너스 적용
        if (newJob == null) return;
        Stat.AddModifier(StatType.MaxHp, newJob.BonusMaxHp);
        Stat.AddModifier(StatType.Attack, newJob.BonusAtk);
        Stat.AddModifier(StatType.Defense, newJob.BonusDef);
        Stat.AddModifier(StatType.MoveSpeed, newJob.BonusMoveSpeed);
        Stat.AddModifier(StatType.Regen, newJob.BonusRegen);
        Stat.AddModifier(StatType.AttackRange, newJob.BonusRange);

        // PERMANENT 패시브 적용
        SkillManager.Instance?.ApplyJobSkills(this, newJob);

        // 투사체 프리팹 갱신
        _projectilePrefab = newJob.ProjectilePrefab;
    }

    private void ResetFlags()
    {
        RegenMultiplier = 1f;
        _attackCount = 0;
    }

    // ─────────────────────────────────────────────
    // 이동
    // ─────────────────────────────────────────────

    public override void Move(Vector2 dir)
    {
        if (_isDashing) return;

        IsMoving = dir.sqrMagnitude > 0.01f;

        if (IsMoving)
            transform.Translate(dir * Stat.FinalMoveSpd * Time.deltaTime);
    }

    private Vector2 GetMoveDir()
    {
        if (_input == null) return Vector2.zero;
        return new Vector2(_input.MoveInputMap.x, _input.MoveInputMap.y).normalized;
    }

    // ─────────────────────────────────────────────
    // Update
    // ─────────────────────────────────────────────

    private void Update()
    {
        Move(GetMoveDir());
        UpdateAutoAttack();
        UpdateRegen();
    }

    // ─────────────────────────────────────────────
    // 자동 공격 업데이트
    // ─────────────────────────────────────────────

    private void UpdateAutoAttack()
    {
        float atkInterval = 1f / Mathf.Max(0.01f, Stat.FinalAtkSpd);

        bool canAttack = Time.time - _lastAttackTime >= atkInterval;

        var target = EnemyManager.Instance?.GetNearestInRange(
            transform.position, Stat.FinalAtkRange);
        if (target == null) return;

        bool isRanged = CurrentJob != null &&
            (CurrentJob.AttackType == AttackType.RANGED_SINGLE ||
             CurrentJob.AttackType == AttackType.RANGED_AREA);

        if (canAttack)
        {
            _lastAttackTime = Time.time;
            _lastCombatTime = Time.time;

            if (isRanged) FireProjectile(target);
            else          ExecuteAttack(target);
        }
    }

    // ─────────────────────────────────────────────
    // 근접 공격
    // ─────────────────────────────────────────────

    private void ExecuteAttack(Enemy target)
    {
        float dmg = CalcMeleeDamage();

        // MELEE_AREA (버서커) — 범위 내 전체 적 타격
        if (CurrentJob != null && CurrentJob.AttackType == AttackType.MELEE_AREA)
        {
            var allEnemies = EnemyManager.Instance?.GetAllEnemies() ?? new List<Enemy>();
            for (int i = allEnemies.Count - 1; i >= 0; i--)
            {
                var e = allEnemies[i];
                if (!e.IsAlive) continue;
                if (Vector2.Distance(transform.position, e.transform.position)
                    <= Stat.FinalAtkRange)
                {
                    e.TakeDamage(this, dmg);
                    OnAfterAttackHit?.Invoke(target, dmg);
                }
            }
        }
        // MELEE_SINGLE — 단일 타겟
        else
        {
            target.TakeDamage(this, dmg);
            OnAfterAttackHit?.Invoke(target, dmg);
        }
    }

    private float CalcMeleeDamage()
    {
        bool isCrit = Stat.ForceCrit;

        if (!isCrit && Stat.CriticalEvery > 0)
        {
            _attackCount++;
            if (_attackCount >= Stat.CriticalEvery)
            {
                isCrit = true;
                _attackCount = 0;
            }
        }

        if (!isCrit)
            isCrit = UnityEngine.Random.value <= Stat.FinalCritChance;

        return Stat.FinalAtk * (isCrit ? Stat.CriticalMultiplier : 1f);
    }

    // ─────────────────────────────────────────────
    // 원거리 공격
    // ─────────────────────────────────────────────

    /// <summary>
    /// 원거리 자동 공격 투사체 발사
    /// angleOffset: 산탄(ran_p3) 각도 오프셋(도)
    /// </summary>
    private void FireProjectile(Enemy target, float angleOffset = 0f)
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning("PlayerEntity_FireProjectile: ProjectilePrefab 미설정");
            return;
        }

        var go = PoolManager.SpawnOrInstance(
            _projectilePrefab, transform.position, Quaternion.identity);

        var proj = go.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError($"PlayerEntity_FireProjectile: {_projectilePrefab.name}에 Projectile 컴포넌트 없음");
            PoolManager.ReleaseOrDestroy(_projectilePrefab, go);
            return;
        }

        // 산탄 각도 적용
        Vector3 targetPos = target.transform.position;
        if (angleOffset != 0f)
        {
            Vector2 dir = (targetPos - transform.position).normalized;
            float rad = angleOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            dir = new Vector2(dir.x * cos - dir.y * sin,
                              dir.x * sin + dir.y * cos);
            targetPos = transform.position + (Vector3)(dir * 10f);
        }

        proj.Init(
            attacker:       this,
            targetPos:      targetPos,
            damage:         Stat.FinalAtk,
            piercing:       Stat.Piercing
        );

        // 투사체 생성 직후 방송
        OnProjectileSpawned?.Invoke(proj);
    }

    // ─────────────────────────────────────────────
    // 회복 업데이트
    // ─────────────────────────────────────────────

    private void UpdateRegen()
    {
        if (IsFullHealth) return;

        _regenTimer += Time.deltaTime;

        bool inCombat = Time.time - _lastCombatTime < REGEN_COMBAT_COOLDOWN;

        if (IsInTown)
        {
            // 마을 회복 — 매 1초
            if (_regenTimer >= TOWN_REGEN_TICK)
            {
                _regenTimer = 0f;
                Stat.ChangeHealth(Stat.FinalRegen * RegenMultiplier);
            }
        }
        else if (!inCombat)
        {
            // 자연 회복 — 매 1초
            if (_regenTimer >= REGEN_TICK)
            {
                _regenTimer = 0f;
                Stat.ChangeHealth(Stat.FinalRegen * RegenMultiplier);
            }
        }
        else
        {
            _regenTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────
    // 사망/리스폰
    // ─────────────────────────────────────────────

    public void Respawn(Vector3 spawnPos)
    {
        _isDead = false;
        transform.position = spawnPos;

        Stat.ClearInvincible();
        Stat.ForceCrit = false;
        ResetFlags(); // 공격 예약 플래그 + _attackCount 초기화

        // 직업 보너스 / PERMANENT 패시브는 ChangeJob 시점에 이미 Additional에 적재됨
        // Respawn은 Stat을 new로 재생성하지 않으므로 재적용 불필요 — 여기서 AddModifier 하면 리스폰마다 2중 누적됨
        Stat.SetHealth(Stat.FinalMaxHealth);
    }

    // [클래스 내부 추가]
    // 직업 변경, 사망, 혹은 씬 전환 시 기존 구독을 날리기 위한 메서드
    public void ClearCombatEvents()
    {
        OnAfterAttackHit = null;
        OnProjectileSpawned = null;
    }

    // [클래스 내부에 추가]
    // ─────────────────────────────────────────────
    // 외부 스킬 강제 실행용 (SkillManager에서 호출)
    // ─────────────────────────────────────────────

    public void ForceExecuteAttack(Enemy target)
    {
        if (target != null && target.IsAlive) ExecuteAttack(target);
    }

    public void ForceFireProjectile(Enemy target)
    {
        if (target != null && target.IsAlive) FireProjectile(target);
    }
}
