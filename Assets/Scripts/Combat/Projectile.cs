using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원거리 공격 투사체
/// PoolManager 연동 (IPooledObject 구현)
/// 플레이어/적 공용 — attacker는 Entity 타입
/// </summary>
public class Projectile : MonoBehaviour, IPooledObject
{
    // ─────────────────────────────────────────────
    // IPooledObject
    // ─────────────────────────────────────────────

    public GameObject OriginPrefab { get; private set; }

    public void SetOrigin(GameObject origin)
    {
        OriginPrefab = origin;
    }

    // ─────────────────────────────────────────────
    // Inspector 설정값
    // ─────────────────────────────────────────────

    [Header("Projectile Settings")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _maxRange = 20f;

    // ─────────────────────────────────────────────
    // 런타임 초기화 값 (Init에서 주입)
    // ─────────────────────────────────────────────

    private Entity _attacker;
    private float _damage;
    private AttackType _attackType;
    private bool _piercing;
    private float _scale;
    private bool _chainExplosion;
    private float _areaRadius;

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────

    private Vector2 _dir;
    private float _traveledDist;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    /// <summary>
    /// 발사 시 PlayerEntity 또는 Enemy에서 호출
    /// </summary>
    public void Init(
        Entity attacker,
        Vector3 targetPos,
        float damage,
        AttackType attackType,
        bool piercing,
        float scale,
        bool chainExplosion,
        float areaRadius)
    {
        _attacker = attacker;
        _damage = damage;
        _attackType = attackType;
        _piercing = piercing;
        _scale = scale;
        _chainExplosion = chainExplosion;
        _areaRadius = areaRadius;
        _traveledDist = 0f;

        // 발사 방향
        _dir = (targetPos - transform.position).normalized;

        // 크기 배율 적용 (wiz_p3)
        transform.localScale = Vector3.one * _scale;

        // 투사체 회전 — 이동 방향으로
        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    // ─────────────────────────────────────────────
    // Update — 이동 + 최대 사거리 체크
    // ─────────────────────────────────────────────

    private void Update()
    {
        float step = _speed * Time.deltaTime;
        transform.Translate(Vector2.up * step);
        _traveledDist += step;

        if (_traveledDist >= _maxRange)
            ReturnToPool();
    }

    // ─────────────────────────────────────────────
    // 충돌 감지
    // ─────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_attacker == null) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy != null && enemy.IsAlive)
        {
            Hit(enemy);
            return;
        }

        // 플레이어 방향 투사체(적이 쏜 경우) — PlayerEntity 충돌
        var player = other.GetComponent<PlayerEntity>();
        if (player != null && !player.Stat.IsInvincible)
        {
            player.TakeDamage(_attacker, _damage);
            ReturnToPool();
        }
    }

    // ─────────────────────────────────────────────
    // 데미지 처리
    // ─────────────────────────────────────────────

    private void Hit(Enemy enemy)
    {
        // 범위 폭발 (RANGED_AREA — 메이지 계열)
        if (_attackType == AttackType.RANGED_AREA)
        {
            DealAreaDamage(enemy.transform.position);

            // 연쇄폭발 (mge_p3) — 추가 범위 폭발
            if (_chainExplosion)
            {
                DealAreaDamage(enemy.transform.position, _areaRadius * 1.5f);
                // ChainExplosionActive 리셋 — attacker가 PlayerEntity인 경우만
                if (_attacker is PlayerEntity playerEntity)
                    playerEntity.ChainExplosionActive = false;
            }

            ReturnToPool();
            return;
        }

        // 단일 타겟 데미지
        enemy.TakeDamage(_attacker, _damage);

        // 관통 (sni_p3) — 계속 진행
        if (_piercing) return;

        ReturnToPool();
    }

    private void DealAreaDamage(Vector3 center, float radius = -1f)
    {
        float r = radius > 0 ? radius : _areaRadius;
        var allEnemies = EnemyManager.Instance?.GetAllEnemies() ?? new List<Enemy>();
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            var e = allEnemies[i];
            if (!e.IsAlive) continue;
            if (Vector2.Distance(center, e.transform.position) <= r)
                e.TakeDamage(_attacker, _damage);
        }
    }

    // ─────────────────────────────────────────────
    // 풀 반환
    // ─────────────────────────────────────────────

    private void ReturnToPool()
    {
        transform.localScale = Vector3.one; // 스케일 초기화
        PoolManager.ReleaseOrDestroy(OriginPrefab, gameObject);
    }

    // ─────────────────────────────────────────────
    // 비활성화 시 자동 초기화
    // ─────────────────────────────────────────────

    private void OnDisable()
    {
        _attacker = null;
        _traveledDist = 0f;
    }
}