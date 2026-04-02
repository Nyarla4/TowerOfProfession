using System;
using UnityEngine;

/// <summary>
/// 원거리 공격 투사체
/// PoolManager 연동 (IPooledObject 구현)
/// 플레이어/적 공용 — attacker는 Entity 타입
/// </summary>
public class Projectile : MonoBehaviour, IPooledObject
{
    // 📢 투사체가 적을 맞췄음을 알리는 이벤트
    public event Action<Projectile, Enemy> OnProjectileHit;

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
    private bool _piercing;

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
        bool piercing)
    {
        _attacker = attacker;
        _damage = damage;
        _piercing = piercing;
        _traveledDist = 0f;

        // 발사 방향
        _dir = (targetPos - transform.position).normalized;

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
            // 단일 데미지 처리
            enemy.TakeDamage(_attacker, _damage);

            // 💡 방송하기: "나 얘 맞췄어!" (광역/연쇄폭발은 밖에서 처리)
            OnProjectileHit?.Invoke(this, enemy);

            // 관통 속성이 없으면 풀 반환
            if (!_piercing) ReturnToPool();
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
    // 풀 반환
    // ─────────────────────────────────────────────

    private void ReturnToPool()
    {
        transform.localScale = Vector3.one; // 스케일 초기화
        // 💡 풀로 돌아가기 전에 기존 구독자 싹 날리기 (메모리 누수 및 다중 폭발 방지)
        OnProjectileHit = null;
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