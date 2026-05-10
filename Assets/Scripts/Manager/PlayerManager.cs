using System.Collections;
using UnityEngine;
using status;

/// <summary>
/// 경험치·레벨업·스탯포인트·리스폰 딜레이 관리
/// HTML game.js 의 gainExp / levelUp / statPoint 파트 대응
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // 리스폰
    // ─────────────────────────────────────────────

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 3f;

    // ─────────────────────────────────────────────
    // 스탯 배분 설정
    // ─────────────────────────────────────────────

    [Header("Stat Allocation")]
    [SerializeField] private float _hpPerPoint       = 30f;
    [SerializeField] private float _atkPerPoint       = 3f;
    [SerializeField] private float _defPerPoint       = 2f;
    [SerializeField] private float _spdPerPoint       = 0.1f;

    // ─────────────────────────────────────────────
    // 내부 참조
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
        _player = GameManager.Instance != null
            ? GameManager.Instance.GetPlayer()
            : FindFirstObjectByType<PlayerEntity>();
    }

    // ─────────────────────────────────────────────
    // 스탯 포인트 배분 (StatAllocUI에서 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 스탯포인트 1개 소모 후 해당 스탯에 영구 적용
    /// HTML: allocateStat()
    /// </summary>
    public bool AllocateStat(StatType type)
    {
        if (LevelUpManager.Instance == null || LevelUpManager.Instance.StatPoints <= 0)
        {
            Debug.LogWarning("PlayerManager_AllocateStat: 스탯포인트 없음");
            return false;
        }

        if (_player == null)
        {
            Debug.LogError("PlayerManager_AllocateStat: PlayerEntity 없음");
            return false;
        }

        float bonus = type switch
        {
            StatType.MaxHp      => _hpPerPoint,
            StatType.Attack     => _atkPerPoint,
            StatType.Defense    => _defPerPoint,
            StatType.MoveSpeed  => _spdPerPoint,
            StatType.STR        => 1f,
            StatType.DEX        => 1f,
            StatType.INT        => 1f,
            _ => 0f
        };

        if (bonus <= 0f)
        {
            Debug.LogWarning($"PlayerManager_AllocateStat: 배분 불가 StatType — {type}");
            return false;
        }

        _player.Stat.AddPermanent(type, bonus, isPercent: false);
        
        LevelUpManager.Instance.ConsumeStatPoint();

        Debug.Log($"PlayerManager_AllocateStat: {type} +{bonus}, 남은포인트={LevelUpManager.Instance.StatPoints}");
        return true;
        }

        /// <summary>
        /// 여러 스탯을 한 번에 배분 (StatAllocUI의 Commit 로직 대응)
        /// </summary>
        public void AllocateStatsBulk(int addStr, int addDex, int addInt)
        {
        if (_player == null) return;

        int totalPoints = addStr + addDex + addInt;
        if (totalPoints <= 0) return;

        if (LevelUpManager.Instance == null || LevelUpManager.Instance.StatPoints < totalPoints)
        {
            Debug.LogWarning("PlayerManager_AllocateStatsBulk: 스탯포인트 부족");
            return;
        }

        if (addStr > 0) _player.Stat.AddPermanent(StatType.STR, addStr, false);
        if (addDex > 0) _player.Stat.AddPermanent(StatType.DEX, addDex, false);
        if (addInt > 0) _player.Stat.AddPermanent(StatType.INT, addInt, false);

        for (int i = 0; i < totalPoints; i++)
        {
            LevelUpManager.Instance.ConsumeStatPoint();
        }

        Debug.Log($"PlayerManager_AllocateStatsBulk: STR+{addStr}, DEX+{addDex}, INT+{addInt}. 남은포인트={LevelUpManager.Instance.StatPoints}");
        }

        // ─────────────────────────────────────────────
        // 리스폰
// ─────────────────────────────────────────────

    /// <summary>
    /// GameManager.OnPlayerDead에서 호출 — 딜레이 후 리스폰
    /// </summary>
    public void TriggerRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        Debug.Log($"PlayerManager_RespawnCoroutine: {_respawnDelay}초 후 리스폰");
        yield return new WaitForSeconds(_respawnDelay);
        GameManager.Instance?.RespawnPlayer();
    }

    // ─────────────────────────────────────────────
    // 저장/로드
    // ─────────────────────────────────────────────

    /// <summary>
    /// GameManager.LoadGame에서 호출
    /// </summary>
    public void LoadFromData(PlayData data)
    {
        if (LevelUpManager.Instance != null)
        {
            // 임시 MaxExp 계산 (기존 PlayerManager 로직 참조)
            float baseExp = 100f;
            float growth = 1.15f;
            float expToNext = Mathf.Floor(baseExp * Mathf.Pow(growth, data.level - 1));
            
            LevelUpManager.Instance.LoadFromData(data.level, data.exp, expToNext, data.statPoints);
        }

        Debug.Log($"PlayerManager_LoadFromData: Lv{data.level} Exp{data.exp} SP{data.statPoints}");
    }
}
