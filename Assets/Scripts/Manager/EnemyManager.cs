using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HTML ai.js + game.js 적 파트 대응
/// 스폰/디스폰, 전체 적 목록 관리, 범위 효과(독안개/성스러운 오라) 업데이트
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // 스폰 설정
    // ─────────────────────────────────────────────

    [System.Serializable]
    public class SpawnGroup
    {
        public string GroupID;
        public GameObject EnemyPrefab;
        public Transform[] SpawnPoints;
        public int MaxCount = 5;
        public float RespawnDelay = 10f;
        public float ExpReward = 20f; // 적 처치 시 지급 경험치
    }

    [SerializeField] private SpawnGroup[] _spawnGroups;

    // ─────────────────────────────────────────────
    // 적 목록
    // ─────────────────────────────────────────────

    private List<Enemy> _enemies = new();

    // 도발 상태 적 추적 (pal_a2)
    private List<Enemy> _forcedChaseEnemies = new();

    // ─────────────────────────────────────────────
    // 범위 효과 (독안개 / 성스러운 오라)
    // ─────────────────────────────────────────────

    [Header("Aura Settings")]
    [SerializeField] private float _poisonAuraRadius = 150f;
    [SerializeField] private float _poisonAuraTick = 1f;
    [SerializeField] private float _holyAuraRadius = 150f;
    [SerializeField] private float _holyAuraTick = 1f;

    private float _poisonAuraTimer;
    private float _holyAuraTimer;

    // ─────────────────────────────────────────────
    // 플레이어 참조
    // ─────────────────────────────────────────────

    private PlayerEntity _player;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerEntity>();
    }

    // ─────────────────────────────────────────────
    // Update
    // ─────────────────────────────────────────────

    private void Update()
    {
        UpdatePoisonAura();
        UpdateHolyAura();
    }

    // ─────────────────────────────────────────────
    // 스폰 / 디스폰
    // ─────────────────────────────────────────────

    /// <summary> 초기 스폰 — GameManager.Start()에서 호출 </summary>
    public void InitSpawn()
    {
        if (_spawnGroups == null) return;
        foreach (var group in _spawnGroups)
            StartCoroutine(SpawnGroupCoroutine(group));
    }

    private IEnumerator SpawnGroupCoroutine(SpawnGroup group)
    {
        int spawned = 0;
        foreach (var point in group.SpawnPoints)
        {
            if (spawned >= group.MaxCount) break;
            SpawnEnemy(group.EnemyPrefab, point.position, group.GroupID);
            spawned++;
            yield return null;
        }
    }

    private Enemy SpawnEnemy(GameObject prefab, Vector3 pos, string groupId = "")
    {
        var go = Instantiate(prefab, pos, Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning("EnemyManager_SpawnEnemy: Enemy 컴포넌트 없음");
            Destroy(go);
            return null;
        }

        enemy.SetGroupID(groupId);
        _enemies.Add(enemy);

        // 사망 이벤트 구독 — 목록 정리 후 ON_KILL 순서 보장
        enemy.OnDead += () => OnEnemyDead(enemy);

        return enemy;
    }

    private void OnEnemyDead(Enemy enemy)
    {
        _enemies.Remove(enemy);
        _forcedChaseEnemies.Remove(enemy);

        // 그룹 탐색 1회 후 리스폰/경험치에 공용 사용
        var group = FindGroupByPrefab(enemy);

        // 목록 정리 완료 후 ON_KILL 트리거 (순서 보장)
        NotifyEnemyDied(enemy, group);

        // 리스폰 처리
        if (group != null)
            StartCoroutine(RespawnAfterDelay(group, enemy.transform.position));

        // 오브젝트 지연 제거
        StartCoroutine(DespawnAfterDelay(enemy.gameObject, 2f));
    }

    private IEnumerator RespawnAfterDelay(SpawnGroup group, Vector3 pos)
    {
        yield return new WaitForSeconds(group.RespawnDelay);
        SpawnEnemy(group.EnemyPrefab, pos, group.GroupID);
    }

    private IEnumerator DespawnAfterDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }

    private SpawnGroup FindGroupByPrefab(Enemy enemy)
    {
        if (_spawnGroups == null || string.IsNullOrEmpty(enemy.GroupID)) return null;
        foreach (var group in _spawnGroups)
        {
            if (group.GroupID == enemy.GroupID) return group;
        }
        return null;
    }

    // ─────────────────────────────────────────────
    // ON_KILL 트리거 (Enemy.Die()에서 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 적 사망 시 호출 — PlayerEntity에 ON_KILL 이벤트 전달
    /// </summary>
    public void NotifyEnemyDied(Enemy deadEnemy, SpawnGroup group = null)
    {
        if (_player == null) return;

        // 경험치 지급
        if (group != null)
            PlayerManager.Instance?.GainExp(group.ExpReward);

        SkillManager.Instance?.TriggerEventPassive(
            _player, _player.CurrentJob, EventType.ON_KILL, deadEnemy);
    }

    // ─────────────────────────────────────────────
    // 도발 관리 (pal_a2)
    // ─────────────────────────────────────────────

    /// <summary> 도발 종료 후 전체 강제추적 적 RETURN </summary>
    public void ReturnForcedChaseEnemies()
    {
        foreach (var enemy in _forcedChaseEnemies)
        {
            if (enemy != null && enemy.IsAlive)
                enemy.ReturnFromForceChase();
        }
        _forcedChaseEnemies.Clear();
    }

    /// <summary> 도발 적 등록 (ForceChase 호출 시 자동 추적용) </summary>
    public void RegisterForcedChase(Enemy enemy)
    {
        if (!_forcedChaseEnemies.Contains(enemy))
            _forcedChaseEnemies.Add(enemy);
    }

    // ─────────────────────────────────────────────
    // 범위 효과 — 독안개 (vip_a1)
    // ─────────────────────────────────────────────

    private void UpdatePoisonAura()
    {
        if (_player == null || !_player.PoisonAuraActive) return;

        _poisonAuraTimer += Time.deltaTime;
        if (_poisonAuraTimer < _poisonAuraTick) return;
        _poisonAuraTimer = 0f;

        var pos = _player.transform.position;
        float dmg = _player.Stat.FinalAtk * 0.3f * _player.Stat.PoisonDamageMultiplier;
        float dur = _player.Stat.PoisonDuration > 0 ? _player.Stat.PoisonDuration : 3f;

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= _poisonAuraRadius)
                enemy.ApplyPoison(dmg, dur);
        }
    }

    // ─────────────────────────────────────────────
    // 범위 효과 — 성스러운 오라 (pal_p2)
    // ─────────────────────────────────────────────

    private void UpdateHolyAura()
    {
        if (_player == null || !_player.HolyAuraActive) return;

        _holyAuraTimer += Time.deltaTime;
        if (_holyAuraTimer < _holyAuraTick) return;
        _holyAuraTimer = 0f;

        var pos = _player.transform.position;
        float healAmount = _player.Stat.FinalRegen * 0.5f;

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= _holyAuraRadius)
            {
                // 근처 적에게 약화 효과 (ATK -10%)
                // ⚠️ 적 전투 디버프 시스템 구현 시 정교화 필요
                // 현재는 플레이어 회복으로 처리
            }
        }

        // 팔라딘 오라: 주변 적 존재 시 플레이어 회복
        bool hasNearby = false;
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= _holyAuraRadius)
            {
                hasNearby = true;
                break;
            }
        }

        if (hasNearby)
            _player.TakeHeal(healAmount);
    }

    // ─────────────────────────────────────────────
    // 조회 인터페이스
    // ─────────────────────────────────────────────

    /// <summary> 전체 적 목록 반환 (SkillManager / PlayerEntity 사용) </summary>
    public List<Enemy> GetAllEnemies() => _enemies;

    /// <summary> 플레이어 공격 사거리 내 가장 가까운 적 반환 </summary>
    public Enemy GetNearestInRange(Vector3 from, float range)
    {
        Enemy nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;
            float dist = Vector2.Distance(from, enemy.transform.position);
            if (dist <= range && dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }
}