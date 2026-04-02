using System.Collections;
using status;
using UnityEngine;

/// <summary>
/// HTML ai.js 대응
/// Entity 상속, AI 상태머신 + 상태이상 처리
/// </summary>
public class Enemy : Entity
{
    // ─────────────────────────────────────────────
    // AI 상태
    // ─────────────────────────────────────────────

    public enum AiState { PATROL, CHASE, ATTACK, RETURN, FLEE }

    public AiState CurrentState { get; private set; } = AiState.PATROL;

    [Header("AI Settings")]
    [SerializeField] private float _detectRange = 300f; // 감지 반경
    [SerializeField] private float _attackRange = 60f;  // 공격 반경
    [SerializeField] private float _returnRange = 600f; // 이탈 → RETURN 전환 거리
    [SerializeField] private float _fleeHpPercent = 0.15f; // FLEE 전환 HP 비율

    private Vector3 _spawnPos;       // 순찰 기준점
    private PlayerEntity _target;   // 추적 대상

    private bool _isForcedChase;     // 도발 상태 (pal_a2)

    /// <summary> 소속 스폰 그룹 ID (EnemyManager 리스폰 역참조용) </summary>
    public string GroupID { get; private set; }

    /// <summary> 원거리 공격 투사체 프리팹 (Initialize에서 _statData.ProjectilePrefab으로 세팅) </summary>
    private GameObject _projectilePrefab;

    // ─────────────────────────────────────────────
    // 공격 타이머
    // ─────────────────────────────────────────────

    private float _lastAttackTime;

    // ─────────────────────────────────────────────
    // 상태이상
    // ─────────────────────────────────────────────

    public bool IsPoison { get; private set; }
    public bool IsStunned { get; private set; }

    private float _poisonDamage;
    private float _poisonEndTime;
    private float _poisonTickTimer;
    private const float POISON_TICK = 1f; // 독 틱 주기 (초)

    private Coroutine _stunCoroutine;

    // ─────────────────────────────────────────────
    // 편의 프로퍼티
    // ─────────────────────────────────────────────

    public bool IsAlive => !_isDead;
    public float HpPercent => Stat.FinalMaxHealth > 0
        ? Stat.CurrentHealth / Stat.FinalMaxHealth : 0f;

    [SerializeField] private EntityStatDataSO _statData;

    private void Awake()
    {
        Initialize(_statData);
    }

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    public override void Initialize(EntityStatDataSO data)
    {
        base.Initialize(data);
        _spawnPos = transform.position;
        CurrentState = AiState.PATROL;
        _isForcedChase = false;
        _projectilePrefab = data.ProjectilePrefab; // 원거리 적 투사체 세팅
        ClearStatusEffects();
    }

    /// <summary> 스폰 시 그룹 ID 주입 (EnemyManager.SpawnEnemy에서 호출) </summary>
    public void SetGroupID(string groupId)
    {
        GroupID = groupId;
    }

    // ─────────────────────────────────────────────
    // Update
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (!IsAlive) return;

        UpdatePoison();

        if (IsStunned) return; // 스턴 중 AI 정지

        UpdateAI();
    }

    // ─────────────────────────────────────────────
    // AI 상태머신
    // ─────────────────────────────────────────────

    private void UpdateAI()
    {
        // 타겟이 없으면 플레이어 탐색
        if (_target == null)
            _target = FindPlayer();

        switch (CurrentState)
        {
            case AiState.PATROL: UpdatePatrol(); break;
            case AiState.CHASE: UpdateChase(); break;
            case AiState.ATTACK: UpdateAttack(); break;
            case AiState.RETURN: UpdateReturn(); break;
            case AiState.FLEE: UpdateFlee(); break;
        }
    }

    private void UpdatePatrol()
    {
        if (_target == null) return;

        float dist = Vector2.Distance(transform.position, _target.transform.position);

        if (_isForcedChase || dist <= _detectRange)
            TransitionTo(AiState.CHASE);
    }

    private void UpdateChase()
    {
        if (_target == null || _target.Stat.CurrentHealth <= 0)
        {
            TransitionTo(AiState.RETURN);
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, _target.transform.position);
        float distToSpawn = Vector2.Distance(transform.position, _spawnPos);

        // 이탈 거리 초과 (강제 추적 제외)
        if (!_isForcedChase && distToSpawn > _returnRange)
        {
            TransitionTo(AiState.RETURN);
            return;
        }

        // FLEE 전환
        if (HpPercent <= _fleeHpPercent)
        {
            TransitionTo(AiState.FLEE);
            return;
        }

        // 공격 범위 진입
        if (distToPlayer <= _attackRange)
        {
            TransitionTo(AiState.ATTACK);
            return;
        }

        // 플레이어 방향으로 이동
        MoveToward(_target.transform.position);
    }

    private void UpdateAttack()
    {
        if (_target == null || _target.Stat.CurrentHealth <= 0)
        {
            TransitionTo(AiState.RETURN);
            return;
        }

        float dist = Vector2.Distance(transform.position, _target.transform.position);

        // 공격 범위 벗어남
        if (dist > _attackRange * 1.2f)
        {
            TransitionTo(AiState.CHASE);
            return;
        }

        // FLEE 전환
        if (HpPercent <= _fleeHpPercent)
        {
            TransitionTo(AiState.FLEE);
            return;
        }

        // 공격 실행 — AttackType에 따라 근접/원거리 분기
        float atkInterval = 1f / Mathf.Max(0.01f, Stat.FinalAtkSpd);
        if (Time.time - _lastAttackTime >= atkInterval)
        {
            _lastAttackTime = Time.time;

            if (_statData.AttackType == AttackType.RANGED_SINGLE ||
                _statData.AttackType == AttackType.RANGED_AREA)
                FireProjectile(_target);
            else
                Attack(_target);
        }
    }

    // ─────────────────────────────────────────────
    // 원거리 공격
    // ─────────────────────────────────────────────

    /// <summary> 원거리 공격 투사체 발사 </summary>
    private void FireProjectile(PlayerEntity target)
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"Enemy_FireProjectile: {name} ProjectilePrefab 미설정 — 근접 공격으로 대체");
            Attack(target);
            return;
        }

        var go = PoolManager.SpawnOrInstance(
            _projectilePrefab, transform.position, Quaternion.identity);

        var proj = go.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError($"Enemy_FireProjectile: {_projectilePrefab.name}에 Projectile 컴포넌트 없음");
            PoolManager.ReleaseOrDestroy(_projectilePrefab, go);
            return;
        }

        proj.Init(
            attacker:       this,
            targetPos:      target.transform.position,
            damage:         Stat.FinalAtk,
            piercing:       false
        );
    }

    private void UpdateReturn()
    {
        float dist = Vector2.Distance(transform.position, _spawnPos);

        if (dist <= 5f)
        {
            // 스폰 지점 복귀 완료
            _isForcedChase = false;
            TransitionTo(AiState.PATROL);
            return;
        }

        MoveToward(_spawnPos);

        // 복귀 중 플레이어 감지
        if (_target != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, _target.transform.position);
            if (distToPlayer <= _detectRange * 0.5f)
                TransitionTo(AiState.CHASE);
        }
    }

    private void UpdateFlee()
    {
        if (_target == null)
        {
            TransitionTo(AiState.RETURN);
            return;
        }

        // HP 회복되면 CHASE 복귀
        if (HpPercent > _fleeHpPercent + 0.05f)
        {
            TransitionTo(AiState.CHASE);
            return;
        }

        // 플레이어 반대 방향으로 도주
        Vector3 fleeDir = (transform.position - _target.transform.position).normalized;
        transform.Translate(fleeDir * Stat.FinalMoveSpd * Time.deltaTime);
    }

    private void TransitionTo(AiState next)
    {
        CurrentState = next;
    }

    // ─────────────────────────────────────────────
    // 이동
    // ─────────────────────────────────────────────

    public override void Move(Vector2 dir)
    {
        if (IsStunned || !IsAlive) return;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.Translate(dir.normalized * Stat.FinalMoveSpd * Time.deltaTime);
    }

    private void MoveToward(Vector3 destination)
    {
        Vector2 dir = (destination - transform.position).normalized;
        Move(dir);
    }

    /// <summary> 블랙홀 끌어당기기 (mge_a1) </summary>
    public void PullToward(Vector3 destination, float speed)
    {
        if (!IsAlive) return;
        Vector3 dir = (destination - transform.position).normalized;
        transform.Translate(dir * speed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // 도발 (pal_a2)
    // ─────────────────────────────────────────────

    /// <summary> 강제 CHASE 전환 (pal_a2 도발) </summary>
    public void ForceChase(PlayerEntity target)
    {
        _target = target;
        _isForcedChase = true;
        TransitionTo(AiState.CHASE);
    }

    /// <summary> 도발 해제 후 RETURN (pal_a2 종료) </summary>
    public void ReturnFromForceChase()
    {
        _isForcedChase = false;
        TransitionTo(AiState.RETURN);
    }

    // ─────────────────────────────────────────────
    // 상태이상 — 독
    // ─────────────────────────────────────────────

    /// <summary> 독 부여 (rog_a2, vip_a1, vip_a2, vip_p3) </summary>
    public void ApplyPoison(float damagePerTick, float duration)
    {
        IsPoison = true;
        _poisonDamage = damagePerTick;
        _poisonEndTime = Time.time + duration;
        _poisonTickTimer = 0f;
    }

    private void UpdatePoison()
    {
        if (!IsPoison) return;

        if (Time.time >= _poisonEndTime)
        {
            IsPoison = false;
            return;
        }

        _poisonTickTimer += Time.deltaTime;
        if (_poisonTickTimer >= POISON_TICK)
        {
            _poisonTickTimer = 0f;
            // 독 데미지는 방어력 무시 (직접 체력 감소)
            Stat.ChangeHealth(-_poisonDamage);
            if (Stat.CurrentHealth <= 0) Die();
        }
    }

    // ─────────────────────────────────────────────
    // 상태이상 — 스턴
    // ─────────────────────────────────────────────

    /// <summary> 스턴 부여 (vip_a2 마비 단검) </summary>
    public void ApplyStun(float duration)
    {
        if (_stunCoroutine != null)
            StopCoroutine(_stunCoroutine);
        _stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        IsStunned = true;
        yield return new WaitForSeconds(duration);
        IsStunned = false;
        _stunCoroutine = null;
    }

    // ─────────────────────────────────────────────
    // 사망 처리
    // ─────────────────────────────────────────────

    public override void Die()
    {
        if (_isDead) return;
        base.Die();
        ClearStatusEffects();
        // ON_KILL 트리거는 EnemyManager가 OnDead 구독을 통해 처리
    }

    // ─────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────

    private PlayerEntity FindPlayer()
    {
        return FindFirstObjectByType<PlayerEntity>();
    }

    private void ClearStatusEffects()
    {
        IsPoison = false;
        IsStunned = false;
        _poisonDamage = 0f;
        _poisonEndTime = 0f;
        _poisonTickTimer = 0f;
        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
            _stunCoroutine = null;
        }
    }

    private void OnDrawGizmos()
    {
        // 감지 범위 — 노란색
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        // 공격 범위 — 빨간색
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        // 복귀 범위 — 파란색
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _returnRange);
    }
}
