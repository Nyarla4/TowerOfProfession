using status;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체 생명주기 관리
/// HTML game.js 의 initGame / gameLoop / handlePlayerDeath 대응
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // 게임 상태
    // ─────────────────────────────────────────────

    public enum GameState { Playing, Paused, GameOver }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ─────────────────────────────────────────────
    // 외부 참조
    // ─────────────────────────────────────────────

    [Header("Player")]
    [SerializeField] private PlayerEntity _player;
    [SerializeField] private EntityStatDataSO _playerStatData;
    [SerializeField] private Transform _defaultSpawnPoint;

    [Header("Jobs")]
    [SerializeField] private List<JobDataSO> _allJobs; // Inspector에서 전 직업 SO 등록

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
        InitGame();
    }

    private void InitGame()
    {
        if (_player == null)
        {
            Debug.LogError("GameManager_InitGame: PlayerEntity 미연결");
            return;
        }

        if (SaveSystem.HasPlayData())
            LoadGame();
        else
            NewGame();

        // 적 스폰
        EnemyManager.Instance?.InitSpawn();

        // 플레이어 사망 이벤트 구독
        _player.OnDead += OnPlayerDead;

        // UI 초기화 (스탯 초기화 완료 후)
        UIManager.Instance?.RefreshAll();
        }

    // ─────────────────────────────────────────────
    // 신규 게임
    // ─────────────────────────────────────────────

    private void NewGame()
    {
        _player.Initialize(_playerStatData);

        // 초기 직업(견습) 적용
        var startJob = GetJobById(JobID.APPRENTICE);
        if (startJob != null)
            _player.ChangeJob(startJob);

        // 시작 위치
        if (_defaultSpawnPoint != null)
            _player.transform.position = _defaultSpawnPoint.position;
    }

    // ─────────────────────────────────────────────
    // 저장 / 로드
    // ─────────────────────────────────────────────

    public void SaveGame()
    {
        if (_player == null) return;

        var data = new PlayData
        {
            level              = LevelUpManager.Instance != null ? LevelUpManager.Instance.CurrentLevel : 1,
            exp                = LevelUpManager.Instance != null ? LevelUpManager.Instance.CurrentExp : 0f,
            jobId              = _player.CurrentJob != null ? _player.CurrentJob.JobID : JobID.APPRENTICE,
            hp                 = _player.Stat.CurrentHealth,
            statPoints         = LevelUpManager.Instance != null ? LevelUpManager.Instance.StatPoints : 0,
            lastSpawnPointName = _defaultSpawnPoint != null ? _defaultSpawnPoint.name : "",
            worldX             = _player.transform.position.x,
            worldY             = _player.transform.position.y,
        };

        SaveSystem.SavePlayData(data);
        SaveSystem.SaveSettings();
        Debug.Log("GameManager_SaveGame: 저장 완료");
    }

    private void LoadGame()
    {
        var data = SaveSystem.LoadPlayData();

        _player.Initialize(_playerStatData);

        // 직업 복원
        var job = GetJobById(data.jobId);
        if (job != null)
            _player.ChangeJob(job);

        // HP 복원 (직업 보너스 적용 후)
        if (data.hp > 0)
            _player.Stat.SetHealth(data.hp);

        // 위치 복원 — (0,0) 정상 좌표 오판 방지를 위해 lastSpawnPointName 유무로 판별
        bool hasSavedPos = !string.IsNullOrEmpty(data.lastSpawnPointName);
        _player.transform.position = hasSavedPos
            ? new Vector3(data.worldX, data.worldY, 0f)
            : (_defaultSpawnPoint != null ? _defaultSpawnPoint.position : Vector3.zero);

        // PlayerManager 복원
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.LoadFromData(data);

        Debug.Log($"GameManager_LoadGame: Lv{data.level} {data.jobId} 로드 완료");
    }

    // ─────────────────────────────────────────────
    // 플레이어 사망 / 리스폰
    // ─────────────────────────────────────────────

    private void OnPlayerDead()
    {
        Debug.Log("GameManager_OnPlayerDead: 플레이어 사망");
        // 일정 시간 후 리스폰 (PlayerManager가 있으면 위임, 없으면 직접 처리)
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.TriggerRespawn();
        else
            RespawnPlayer();
    }

    /// <summary> 리스폰 진입점 (PlayerManager 없을 때 직접 호출) </summary>
    public void RespawnPlayer()
    {
        if (_player == null) return;
        Vector3 spawnPos = _defaultSpawnPoint != null
            ? _defaultSpawnPoint.position
            : Vector3.zero;
        _player.Respawn(spawnPos);
    }

    // ─────────────────────────────────────────────
    // 게임 상태 제어
    // ─────────────────────────────────────────────

    public void SetState(GameState state)
    {
        CurrentState = state;
        Time.timeScale = state == GameState.Paused ? 0f : 1f;
    }

    public void PauseGame()  => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);

    // ─────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────

    public JobDataSO GetJobById(JobID jobId)
    {
        if (_allJobs == null) return null;
        foreach (var job in _allJobs)
        {
            if (job != null && job.JobID == jobId)
                return job;
        }
        Debug.LogWarning($"GameManager_GetJobById: {jobId} 없음");
        return null;
    }

    /// <summary> 외부에서 플레이어 참조 접근 </summary>
    public PlayerEntity GetPlayer() => _player;

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        // 모바일 — 백그라운드 전환 시 자동 저장
        if (pause) SaveGame();
    }
}
