using UnityEngine;
using UnityEngine.UI;
using TMPro;
using status;

public class StatAllocationUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _statPanel;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private Button _confirmBtn;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _strText;
    [SerializeField] private TextMeshProUGUI _dexText;
    [SerializeField] private TextMeshProUGUI _intText;
    [SerializeField] private TextMeshProUGUI _pointsText;

    [Header("Buttons (+ / -)")]
    [SerializeField] private Button _strPlusBtn;
    [SerializeField] private Button _strMinusBtn;
    [SerializeField] private Button _dexPlusBtn;
    [SerializeField] private Button _dexMinusBtn;
    [SerializeField] private Button _intPlusBtn;
    [SerializeField] private Button _intMinusBtn;

    [Header("HUD")]
    [SerializeField] private Button _notificationBtn;

    private PlayerEntity _player;

    // State tracking
    private int _baseStr, _pendingStr;
    private int _baseDex, _pendingDex;
    private int _baseInt, _pendingInt;
    private int _basePoints, _pendingPoints;

    private void Awake()
    {
        // Bind UI flow
        if (_closeBtn != null)
            _closeBtn.onClick.AddListener(ClosePopup);

        if (_confirmBtn != null)
            _confirmBtn.onClick.AddListener(ConfirmAllocation);

        // Bind Stat Adjustment Logic
        if (_strPlusBtn != null) _strPlusBtn.onClick.AddListener(() => AdjustStat(StatType.STR, 1));
        if (_strMinusBtn != null) _strMinusBtn.onClick.AddListener(() => AdjustStat(StatType.STR, -1));

        if (_dexPlusBtn != null) _dexPlusBtn.onClick.AddListener(() => AdjustStat(StatType.DEX, 1));
        if (_dexMinusBtn != null) _dexMinusBtn.onClick.AddListener(() => AdjustStat(StatType.DEX, -1));

        if (_intPlusBtn != null) _intPlusBtn.onClick.AddListener(() => AdjustStat(StatType.INT, 1));
        if (_intMinusBtn != null) _intMinusBtn.onClick.AddListener(() => AdjustStat(StatType.INT, -1));

        // Bind HUD Notification
        if (_notificationBtn != null)
            _notificationBtn.onClick.AddListener(OpenPopup);
    }

    private void Start()
    {
        _player = GameManager.Instance != null
            ? GameManager.Instance.GetPlayer()
            : FindFirstObjectByType<PlayerEntity>();

        if (_statPanel != null)
            _statPanel.SetActive(false);
    }

    private void Update()
    {
        if (_notificationBtn != null && LevelUpManager.Instance != null)
        {
            bool hasPoints = LevelUpManager.Instance.StatPoints > 0;
            if (_notificationBtn.gameObject.activeSelf != hasPoints)
                _notificationBtn.gameObject.SetActive(hasPoints);
        }
    }

    public void OpenPopup()
    {
        if (_statPanel == null || _player == null || LevelUpManager.Instance == null) return;

        // Initialize state
        _baseStr = _pendingStr = Mathf.RoundToInt(_player.Stat.FinalStr);
        _baseDex = _pendingDex = Mathf.RoundToInt(_player.Stat.FinalDex);
        _baseInt = _pendingInt = Mathf.RoundToInt(_player.Stat.FinalInt);
        _basePoints = _pendingPoints = LevelUpManager.Instance.StatPoints;

        RefreshUI();
        Time.timeScale = 0;
        _statPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        if (_statPanel != null)
        {
            Time.timeScale = 1;
            _statPanel.SetActive(false);
        }
    }

    private void AdjustStat(StatType type, int amount)
    {
        if (amount > 0) // Addition
        {
            if (_pendingPoints > 0)
            {
                switch (type)
                {
                    case StatType.STR: _pendingStr++; break;
                    case StatType.DEX: _pendingDex++; break;
                    case StatType.INT: _pendingInt++; break;
                }
                _pendingPoints--;
            }
        }
        else // Subtraction
        {
            switch (type)
            {
                case StatType.STR: if (_pendingStr > _baseStr) { _pendingStr--; _pendingPoints++; } break;
                case StatType.DEX: if (_pendingDex > _baseDex) { _pendingDex--; _pendingPoints++; } break;
                case StatType.INT: if (_pendingInt > _baseInt) { _pendingInt--; _pendingPoints++; } break;
            }
        }

        RefreshUI();
    }

    private void ConfirmAllocation()
    {
        int addStr = _pendingStr - _baseStr;
        int addDex = _pendingDex - _baseDex;
        int addInt = _pendingInt - _baseInt;

        if (addStr + addDex + addInt <= 0)
        {
            ClosePopup();
            return;
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.AllocateStatsBulk(addStr, addDex, addInt);
            
            // Update base to current pending after successful allocation
            _baseStr = _pendingStr;
            _baseDex = _pendingDex;
            _baseInt = _pendingInt;
            _basePoints = _pendingPoints;

            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        // Stats
        UpdateStatText(_strText, "STR", _pendingStr, _baseStr);
        UpdateStatText(_dexText, "DEX", _pendingDex, _baseDex);
        UpdateStatText(_intText, "INT", _pendingInt, _baseInt);

        // Points
        if (_pointsText != null)
        {
            _pointsText.text = $"Points: {_pendingPoints}";
            _pointsText.color = _pendingPoints < _basePoints ? Color.yellow : Color.white;
        }

        // Confirm button interactability
        if (_confirmBtn != null)
        {
            _confirmBtn.interactable = (_pendingStr > _baseStr || _pendingDex > _baseDex || _pendingInt > _baseInt);
        }
    }

    private void UpdateStatText(TextMeshProUGUI textObj, string label, int pending, int baseVal)
    {
        if (textObj == null) return;
        textObj.text = $"{label}: {pending}";
        textObj.color = pending > baseVal ? Color.green : Color.white;
    }
}
