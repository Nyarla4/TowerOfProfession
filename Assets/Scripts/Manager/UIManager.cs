using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HP바 / 경험치바 / 레벨 / 스킬 쿨다운 / 스탯포인트 표시
/// HTML ui.js 의 updateHpBar / updateExpBar / updateSkillCooldowns 대응
/// 실제 UI 패널(JobChangeUI, StatAllocUI)은 별도 스크립트로 분리
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // HP
    // ─────────────────────────────────────────────

    [Header("HP")]
    [SerializeField] private Slider   _hpSlider;
    [SerializeField] private TMP_Text _hpText;      // "450 / 500"

    // ─────────────────────────────────────────────
    // 경험치 / 레벨
    // ─────────────────────────────────────────────

    [Header("Exp / Level")]
    [SerializeField] private Slider   _expSlider;
    [SerializeField] private TMP_Text _levelText;   // "Lv. 12"
    [SerializeField] private TMP_Text _expText;     // "340 / 500"

    // ─────────────────────────────────────────────
    // 스탯포인트
    // ─────────────────────────────────────────────

    [Header("Stat Points")]
    [SerializeField] private TMP_Text _statPointText;   // "스탯포인트: 3"
    [SerializeField] private GameObject _statPointAlert; // 포인트 있을 때 표시할 아이콘/뱃지

    // ─────────────────────────────────────────────
    // 스킬 슬롯 (액티브 2개)
    // ─────────────────────────────────────────────

    [Header("Skill Slots")]
    [SerializeField] private Image    _skill0Icon;
    [SerializeField] private Image    _skill0Cooldown; // 쿨다운 오버레이 (fillAmount)
    [SerializeField] private TMP_Text _skill0CooldownText;

    [SerializeField] private Image    _skill1Icon;
    [SerializeField] private Image    _skill1Cooldown;
    [SerializeField] private TMP_Text _skill1CooldownText;

    // ─────────────────────────────────────────────
    // 직업명
    // ─────────────────────────────────────────────

    [Header("Job")]
    [SerializeField] private TMP_Text _jobNameText;

    // ─────────────────────────────────────────────
    // 내부 참조
    // ─────────────────────────────────────────────

    private PlayerEntity  _player;
    private PlayerManager _playerManager;

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
        _player        = GameManager.Instance != null
            ? GameManager.Instance.GetPlayer()
            : FindFirstObjectByType<PlayerEntity>();

        _playerManager = PlayerManager.Instance;
    }

    // ─────────────────────────────────────────────
    // Update — 매 프레임 갱신
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (_player == null) return;

        UpdateHp();
        UpdateExp();
        UpdateStatPoints();
        UpdateSkillCooldowns();
    }

    // ─────────────────────────────────────────────
    // HP
    // ─────────────────────────────────────────────

    private void UpdateHp()
    {
        float cur = _player.Stat.CurrentHealth;
        float max = _player.Stat.FinalMaxHealth;
        float ratio = max > 0 ? cur / max : 0f;

        if (_hpSlider != null) _hpSlider.value = ratio;
        if (_hpText   != null) _hpText.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
    }

    // ─────────────────────────────────────────────
    // 경험치 / 레벨
    // ─────────────────────────────────────────────

    private void UpdateExp()
    {
        if (_playerManager == null) return;

        if (_expSlider  != null) _expSlider.value = _playerManager.ExpRatio;
        if (_levelText  != null) _levelText.text  = $"Lv. {_playerManager.Level}";
        if (_expText    != null) _expText.text =
            $"{Mathf.FloorToInt(_playerManager.Exp)} / {Mathf.FloorToInt(_playerManager.ExpToNext)}";
    }

    // ─────────────────────────────────────────────
    // 스탯포인트
    // ─────────────────────────────────────────────

    private void UpdateStatPoints()
    {
        if (_playerManager == null) return;

        int sp = _playerManager.StatPoints;
        if (_statPointText  != null) _statPointText.text = $"스탯포인트: {sp}";
        if (_statPointAlert != null) _statPointAlert.SetActive(sp > 0);
    }

    // ─────────────────────────────────────────────
    // 스킬 쿨다운
    // ─────────────────────────────────────────────

    private void UpdateSkillCooldowns()
    {
        if (_player?.CurrentJob == null) return;
        var actives = _player.CurrentJob.Actives;
        if (actives == null) return;

        UpdateSlot(0, actives, _skill0Icon, _skill0Cooldown, _skill0CooldownText);
        UpdateSlot(1, actives, _skill1Icon, _skill1Cooldown, _skill1CooldownText);
    }

    private void UpdateSlot(int index, ActiveData[] actives,
        Image icon, Image cooldownOverlay, TMP_Text cooldownText)
    {
        if (index >= actives.Length)
        {
            // 슬롯에 스킬 없음 → 비활성화
            if (icon            != null) icon.enabled = false;
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            if (cooldownText    != null) cooldownText.text = "";
            return;
        }

        var data = actives[index];

        // 아이콘
        if (icon != null)
        {
            icon.enabled = true;
            if (data.Icon != null) icon.sprite = data.Icon;
        }

        // 쿨다운 오버레이 (1→0 방향 fillAmount)
        float ratio = SkillManager.Instance != null
            ? 1f - SkillManager.Instance.GetCooldownRatio(data.SkillID, data.Cooldown)
            : 0f;

        if (cooldownOverlay != null) cooldownOverlay.fillAmount = ratio;

        // 쿨다운 텍스트
        if (cooldownText != null)
        {
            if (ratio > 0f)
            {
                float remaining = data.Cooldown * ratio;
                cooldownText.text = remaining >= 1f
                    ? $"{Mathf.CeilToInt(remaining)}"
                    : $"{remaining:F1}";
            }
            else
            {
                cooldownText.text = "";
            }
        }
    }

    // ─────────────────────────────────────────────
    // 직업명
    // ─────────────────────────────────────────────

    private void UpdateJobName()
    {
        if (_jobNameText == null || _player == null) return;
        _jobNameText.text = _player.CurrentJob != null
            ? _player.CurrentJob.DisplayName
            : "";
    }

    // ─────────────────────────────────────────────
    // 외부 호출 — 전직/레벨업 등 이벤트성 갱신
    // ─────────────────────────────────────────────

    /// <summary> 전직 시 직업명·스킬 아이콘 즉시 갱신 (JobChangeUI에서 호출) </summary>
    public void RefreshJobUI()
    {
        UpdateJobName();
        UpdateSkillCooldowns();
    }

    /// <summary> 전체 UI 즉시 갱신 (씬 로드·리스폰 후 호출) </summary>
    public void RefreshAll()
    {
        // 참조가 아직 없다면 갱신 시도
        if (_player == null)
            _player = GameManager.Instance?.GetPlayer() ?? FindFirstObjectByType<PlayerEntity>();
        if (_playerManager == null)
            _playerManager = PlayerManager.Instance;

        if (_player == null) return;
        UpdateHp();
        UpdateExp();
        UpdateStatPoints();
        UpdateSkillCooldowns();
        UpdateJobName();
    }
}
