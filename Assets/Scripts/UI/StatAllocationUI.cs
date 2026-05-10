using UnityEngine;
using UnityEngine.UI;
using TMPro;
using status;

public class StatAllocationUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _statPanel;
    [SerializeField] private Button _closeBtn;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _strText;
    [SerializeField] private TextMeshProUGUI _dexText;
    [SerializeField] private TextMeshProUGUI _intText;
    [SerializeField] private TextMeshProUGUI _pointsText; // Added to show remaining points

    [Header("Buttons")]
    [SerializeField] private Button _strBtn;
    [SerializeField] private Button _dexBtn;
    [SerializeField] private Button _intBtn;

    [Header("HUD")]
    [SerializeField] private Button _notificationBtn;

    private PlayerEntity _player;

    private void Awake()
    {
        // Bind button listeners
        if (_closeBtn != null)
            _closeBtn.onClick.AddListener(ClosePopup);

        if (_strBtn != null)
            _strBtn.onClick.AddListener(() => OnStatBtnClicked(StatType.STR));

        if (_dexBtn != null)
            _dexBtn.onClick.AddListener(() => OnStatBtnClicked(StatType.DEX));

        if (_intBtn != null)
            _intBtn.onClick.AddListener(() => OnStatBtnClicked(StatType.INT));

        if (_notificationBtn != null)
            _notificationBtn.onClick.AddListener(OpenPopup);
    }

    private void Start()
    {
        _player = GameManager.Instance != null
            ? GameManager.Instance.GetPlayer()
            : FindFirstObjectByType<PlayerEntity>();

        // Ensure panel is closed at start
        ClosePopup();
    }

    private void Update()
    {
        // Requirement 4: Hide notification button when StatPoints == 0
        // Note: UIManager also does this for _statPointAlert, but we ensure it here as requested.
        if (_notificationBtn != null && LevelUpManager.Instance != null)
        {
            bool hasPoints = LevelUpManager.Instance.StatPoints > 0;
            if (_notificationBtn.gameObject.activeSelf != hasPoints)
            {
                _notificationBtn.gameObject.SetActive(hasPoints);
            }
        }
    }

    private void OnStatBtnClicked(StatType type)
    {
        if (LevelUpManager.Instance == null || LevelUpManager.Instance.StatPoints <= 0) return;

        if (PlayerManager.Instance != null && PlayerManager.Instance.AllocateStat(type))
        {
            RefreshUI();
        }
    }

    public void OpenPopup()
    {
        if (_statPanel != null)
        {
            RefreshUI();
            Time.timeScale = 0;
            _statPanel.SetActive(true);
        }
    }

    private void ClosePopup()
    {
        if (_statPanel != null)
        {
            Time.timeScale = 1;
            _statPanel.SetActive(false);
        }
    }

    private void RefreshUI()
    {
        if (_player == null) return;

        if (_strText != null) _strText.text = $"STR: {_player.Stat.FinalStr}";
        if (_dexText != null) _dexText.text = $"DEX: {_player.Stat.FinalDex}";
        if (_intText != null) _intText.text = $"INT: {_player.Stat.FinalInt}";

        if (_pointsText != null && LevelUpManager.Instance != null)
        {
            _pointsText.text = $"Points: {LevelUpManager.Instance.StatPoints}";
        }
    }
}
