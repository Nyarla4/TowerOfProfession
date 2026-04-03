using UnityEngine;
using status; // StatType 참조를 위해 필요

public enum UpgradeType { STAT, PASSIVE, SKILL_MOD }

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Tower/UpgradeData")]
public class UpgradeDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string UpgradeID;        // 예: "stat_atk_1", "mge_mod_3way"
    public string UpgradeName;      // 카드 이름 (예: "공격력 증가", "다중 영창")
    [TextArea] public string Description; // 카드 설명
    public Sprite Icon;             // 카드 아이콘

    [Header("등장 조건 (구조)")]
    [Tooltip("NONE이면 모든 직업 공용, 특정 직업 지정 시 해당 직업일 때만 등장")]
    public JobID RequiredJob = JobID.NONE;

    [Header("효과 설정")]
    public UpgradeType Type;

    // 🟢 [STAT] 타입일 경우 사용하는 변수들
    public StatType TargetStat;
    public float StatValue;

    // 🟣 [PASSIVE] 또는 [SKILL_MOD] 타입일 경우 사용하는 변수
    [Tooltip("SkillManager가 이 카드를 먹었는지 확인하기 위한 키워드")]
    public string ModifierID;
}