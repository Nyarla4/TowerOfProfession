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
    // 레벨 / 경험치
    // ─────────────────────────────────────────────

    public int   Level      { get; private set; } = 1;
    public float Exp        { get; private set; } = 0f;
    public float ExpToNext  { get; private set; }       // 현재 레벨업에 필요한 경험치
    public float ExpRatio   => ExpToNext > 0 ? Exp / ExpToNext : 0f; // UI용 0~1

    // ─────────────────────────────────────────────
    // 스탯 포인트
    // ─────────────────────────────────────────────

    public int StatPoints { get; private set; } = 0;

    // ─────────────────────────────────────────────
    // 리스폰
    // ─────────────────────────────────────────────

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 3f;

    // ─────────────────────────────────────────────
    // 레벨업 설정
    // ─────────────────────────────────────────────

    [Header("Level")]
    [SerializeField] private int   _maxLevel       = 100;
    [SerializeField] private float _baseExpRequired = 100f;  // Lv1→2 필요 경험치
    [SerializeField] private float _expGrowthRate   = 1.15f; // 레벨당 배율

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

        ExpToNext = CalcExpRequired(Level);
    }

    // ─────────────────────────────────────────────
    // 경험치 획득 (ExpOrb 수집 시 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 경험치 획득
    /// HTML: gainExp()
    /// </summary>
    public void GainExp(float amount)
    {
        if (Level >= _maxLevel) return;

        Exp += amount;

        // 연속 레벨업 처리
        while (Exp >= ExpToNext && Level < _maxLevel)
        {
            Exp -= ExpToNext;
            LevelUp();
        }

        // 최대 레벨 도달 시 경험치 고정
        if (Level >= _maxLevel)
            Exp = 0f;
    }

    private void LevelUp()
    {
        Level++;
        StatPoints += 3; // 레벨업마다 스탯포인트 3 지급
        ExpToNext = CalcExpRequired(Level);

        Debug.Log($"PlayerManager_LevelUp: Lv{Level} 달성, StatPoints={StatPoints}");
    }

    /// <summary>
    /// 레벨에 따른 필요 경험치 계산
    /// HTML: calcExpRequired(level)
    /// </summary>
    private float CalcExpRequired(int level)
    {
        // baseExp × growthRate^(level-1)
        return Mathf.Floor(_baseExpRequired * Mathf.Pow(_expGrowthRate, level - 1));
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
        if (StatPoints <= 0)
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
            _ => 0f
        };

        if (bonus <= 0f)
        {
            Debug.LogWarning($"PlayerManager_AllocateStat: 배분 불가 StatType — {type}");
            return false;
        }

        _player.Stat.AddPermanent(type, bonus, isPercent: false);
        StatPoints--;

        Debug.Log($"PlayerManager_AllocateStat: {type} +{bonus}, 남은포인트={StatPoints}");
        return true;
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
        Level      = Mathf.Max(1, data.level);
        Exp        = Mathf.Max(0, data.exp);
        StatPoints = Mathf.Max(0, data.statPoints);
        ExpToNext  = CalcExpRequired(Level);

        Debug.Log($"PlayerManager_LoadFromData: Lv{Level} Exp{Exp}/{ExpToNext} SP{StatPoints}");
    }
}
