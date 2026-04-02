using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HTML ai.js + game.js 적 파트 대응
/// 스폰/디스폰, 전체 적 목록 관리, 범위 효과(독안개/성스러운 오라) 업데이트
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public event Action<Enemy, SpawnGroup> OnEnemyDiedGlobal;

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
        if (group.SpawnPoints == null || group.SpawnPoints.Length == 0)
        {
            Debug.LogWarning($"EnemyManager: {group.GroupID} SpawnPoints 미설정");
            yield break;
        }

        for (int i = 0; i < group.MaxCount; i++)
        {
            var point = group.SpawnPoints[UnityEngine.Random.Range(0, group.SpawnPoints.Length)];
            SpawnEnemy(group.EnemyPrefab, point.position, group.GroupID);
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

        // 그룹 탐색 1회 후 리스폰/경험치에 공용 사용
        var group = FindGroupByPrefab(enemy);

        // 목록 정리 완료 후 ON_KILL 트리거 (순서 보장)
        NotifyEnemyDied(enemy, group);

        // 리스폰 처리
        if (group != null)
        {
            var point = group.SpawnPoints[UnityEngine.Random.Range(0, group.SpawnPoints.Length)];
            StartCoroutine(RespawnAfterDelay(group, point.position));
        }

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

        OnEnemyDiedGlobal?.Invoke(deadEnemy, group);
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