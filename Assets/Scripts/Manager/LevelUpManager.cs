using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    // UI 업데이트를 위한 이벤트
    public event Action<float, float> OnExpChanged;         // 현재 경험치, 최대 경험치
    public event Action<List<UpgradeDataSO>> OnLevelUp;     // 뽑힌 카드 목록 전달

    [Header("Level Settings")]
    public int CurrentLevel = 1;
    public float CurrentExp = 0f;
    public float MaxExp = 100f; // 다음 레벨업 요구량

    [Header("Data Pool")]
    public List<UpgradeDataSO> AllUpgrades; // 인스펙터에서 전체 카드 할당

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // EnemyManager의 '적 사망 이벤트'를 구독하여 경험치 획득 흐름 연결
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDiedGlobal += HandleEnemyDied;
        }
    }

    private void HandleEnemyDied(Enemy enemy, EnemyManager.SpawnGroup group)
    {
        if (group != null) GainExp(group.ExpReward);
    }

    public void GainExp(float amount)
    {
        CurrentExp += amount;

        // 레벨업 조건 달성
        if (CurrentExp >= MaxExp)
        {
            CurrentExp -= MaxExp;
            CurrentLevel++;
            MaxExp = MaxExp * 1.5f; // 임시: 요구 경험치 1.5배씩 증가

            TriggerLevelUp();
        }

        OnExpChanged?.Invoke(CurrentExp, MaxExp);
    }

    private void TriggerLevelUp()
    {
        // 1. 물리 연산 및 전투 흐름 일시 정지
        Time.timeScale = 0f;

        var player = FindFirstObjectByType<PlayerEntity>();
        JobID currentJobID = player != null && player.CurrentJob != null ? player.CurrentJob.JobID : JobID.NONE;

        // 2. 현재 직업에 맞는 카드만 필터링 (공용 + 내 직업 전용)
        List<UpgradeDataSO> availablePool = new List<UpgradeDataSO>();
        foreach (var upgrade in AllUpgrades)
        {
            if (upgrade.RequiredJob == JobID.NONE || upgrade.RequiredJob == currentJobID)
            {
                availablePool.Add(upgrade);
            }
        }

        // 3. 필터링된 풀에서 최대 3장 무작위 추첨 (중복 방지)
        List<UpgradeDataSO> choices = new List<UpgradeDataSO>();
        int drawCount = Mathf.Min(3, availablePool.Count); // 풀이 적으면 적은 대로 뽑음

        for (int i = 0; i < drawCount; i++)
        {
            int rnd = UnityEngine.Random.Range(0, availablePool.Count);
            choices.Add(availablePool[rnd]);
            availablePool.RemoveAt(rnd); // 중복으로 안 뽑히게 풀에서 제거
        }

        // 4. UI에 카드 던져주기 (방송)
        OnLevelUp?.Invoke(choices);
    }

    // UI에서 유저가 카드를 클릭했을 때 호출할 함수
    public void SelectUpgrade(UpgradeDataSO selectedUpgrade)
    {
        var player = FindFirstObjectByType<PlayerEntity>();

        // 카드 효과 적용 (구조 데이터 기반 연산)
        switch (selectedUpgrade.Type)
        {
            case UpgradeType.STAT:
                player.Stat.AddModifier(selectedUpgrade.TargetStat, selectedUpgrade.StatValue);
                break;
            case UpgradeType.PASSIVE:
            case UpgradeType.SKILL_MOD:
                SkillManager.Instance?.AddUpgradeModifier(selectedUpgrade.ModifierID);
                break;
        }

        // 💡 시간 연산 재개 (전투 흐름 복구)
        Time.timeScale = 1f;

        // 혹시 폭발적으로 경험치를 얻어 레벨업이 2번 터진 경우를 위한 체크
        if (CurrentExp >= MaxExp) GainExp(0);
    }
}