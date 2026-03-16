using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전직 선택 패널 UI
/// 버튼 4개를 공용으로 재활용 — 현재 직업에 따라 선택지 동적 할당
/// </summary>
public class JobChangeUI : MonoBehaviour
{
    [Header("Job Change Level Requirements")]
    [SerializeField] private int _firstJobLevel  = 5;
    [SerializeField] private int _secondJobLevel = 15;

    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("HUD")]
    [SerializeField] private GameObject _jobChangeButton;

    [Header("Job Select Buttons (공용 4개)")]
    [SerializeField] private Button    _btn0;
    private TMP_Text  _btn0Label;
    [SerializeField] private Button    _btn1;
    private TMP_Text  _btn1Label;
    [SerializeField] private Button    _btn2;
    private TMP_Text  _btn2Label;
    [SerializeField] private Button    _btn3;
    private TMP_Text  _btn3Label;

    [Header("Info")]
    [SerializeField] private TMP_Text _currentJobText;
    [SerializeField] private TMP_Text _levelReqText;

    // 현재 직업 JobID → 선택 가능한 다음 직업 JobID 목록
    private static readonly System.Collections.Generic.Dictionary<string, string[]> _jobTree
        = new()
    {
        { "APPRENTICE", new[] { "WARRIOR",   "ARCHER",  "WIZARD",  "ROGUE"   } },
        { "WARRIOR",    new[] { "BERSERKER",  "PALADIN"                       } },
        { "ARCHER",     new[] { "SNIPER",     "RANGER"                        } },
        { "WIZARD",     new[] { "MAGE",       "PRIEST"                        } },
        { "ROGUE",      new[] { "ASSASSIN",   "VIPER"                         } },
    };

    // JobID → 한국어 표시명
    private static readonly System.Collections.Generic.Dictionary<string, string> _jobNames
        = new()
    {
        { "WARRIOR",   "전사"    }, { "ARCHER",   "궁수"    },
        { "WIZARD",    "마법사"  }, { "ROGUE",    "도적"    },
        { "BERSERKER", "버서커"  }, { "PALADIN",  "팔라딘"  },
        { "SNIPER",    "스나이퍼"}, { "RANGER",   "레인저"  },
        { "MAGE",      "메이지"  }, { "PRIEST",   "프리스트"},
        { "ASSASSIN",  "어쌔신"  }, { "VIPER",    "독사"    },
    };

    private PlayerEntity  _player;
    private PlayerManager _playerManager;
    private Button[]      _buttons;
    private TMP_Text[]    _labels;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    private void Start()
    {
        _player        = GameManager.Instance?.GetPlayer();
        _playerManager = PlayerManager.Instance;
        _buttons = new[] { _btn0, _btn1, _btn2, _btn3 };
        _btn0Label = _btn0.transform.GetChild(0).GetComponent<TMP_Text>();
        _btn1Label = _btn1.transform.GetChild(0).GetComponent<TMP_Text>();
        _btn2Label = _btn2.transform.GetChild(0).GetComponent<TMP_Text>();
        _btn3Label = _btn3.transform.GetChild(0).GetComponent<TMP_Text>();
        _labels  = new[] { _btn0Label, _btn1Label, _btn2Label, _btn3Label };
        ClosePanel();
    }

    private void Update()
    {
        if (_jobChangeButton == null || _playerManager == null) return;
        _jobChangeButton.SetActive(CanChangeJob());
    }

    // ─────────────────────────────────────────────
    // 전직 가능 여부
    // ─────────────────────────────────────────────

    private bool CanChangeJob()
    {
        if (_player?.CurrentJob == null) return false;
        string jobId = _player.CurrentJob.JobID;
        int level    = _playerManager.Level;

        if (jobId == "APPRENTICE")
            return level >= _firstJobLevel && _jobTree.ContainsKey(jobId);

        if (_jobTree.ContainsKey(jobId))
            return level >= _secondJobLevel;

        return false;
    }

    // ─────────────────────────────────────────────
    // 패널 열기 / 닫기
    // ─────────────────────────────────────────────

    public void OpenPanel()
    {
        if (!CanChangeJob()) return;
        RefreshButtons();
        UpdateInfoText();
        _panel?.SetActive(true);
        GameManager.Instance?.PauseGame();
    }

    public void ClosePanel()
    {
        _panel?.SetActive(false);
        GameManager.Instance?.ResumeGame();
    }

    // ─────────────────────────────────────────────
    // 버튼 동적 할당
    // ─────────────────────────────────────────────

    private void RefreshButtons()
    {
        string currentJobId = _player?.CurrentJob?.JobID ?? "";
        string[] options = _jobTree.ContainsKey(currentJobId)
            ? _jobTree[currentJobId] : new string[0];

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i] == null) continue;

            if (i < options.Length)
            {
                string jobId = options[i]; // 클로저 캡처 방지용 로컬 변수
                _buttons[i].gameObject.SetActive(true);
                _buttons[i].onClick.RemoveAllListeners();
                _buttons[i].onClick.AddListener(() => ChangeJobTo(jobId));
                if (_labels[i] != null)
                    _labels[i].text = _jobNames.ContainsKey(jobId) ? _jobNames[jobId] : jobId;
            }
            else
            {
                _buttons[i].gameObject.SetActive(false);
            }
        }
    }

    // ─────────────────────────────────────────────
    // 전직 실행
    // ─────────────────────────────────────────────

    private void ChangeJobTo(string jobId)
    {
        var job = GameManager.Instance?.GetJobById(jobId);
        if (job == null)
        {
            Debug.LogError($"JobChangeUI: {jobId} SO 없음 — GameManager._allJobs 확인 필요");
            return;
        }
        _player?.ChangeJob(job);
        UIManager.Instance?.RefreshJobUI();
        ClosePanel();
        Debug.Log($"JobChangeUI: {jobId} 전직 완료");
    }

    // ─────────────────────────────────────────────
    // 정보 텍스트
    // ─────────────────────────────────────────────

    private void UpdateInfoText()
    {
        if (_currentJobText != null && _player?.CurrentJob != null)
            _currentJobText.text = $"현재 직업: {_player.CurrentJob.DisplayName}";

        if (_levelReqText != null)
        {
            string jobId = _player?.CurrentJob?.JobID ?? "";
            int req = jobId == "APPRENTICE" ? _firstJobLevel : _secondJobLevel;
            _levelReqText.text = $"전직 가능 레벨: {req}";
        }
    }
}
