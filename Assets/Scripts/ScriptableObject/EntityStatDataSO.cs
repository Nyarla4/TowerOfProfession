using System.Collections.Generic;
using UnityEngine;

namespace status
{
    public enum StatType
    {
        MaxHp,
        MoveSpeed,
        Attack,
        Defense,
        AttackSpeed,
        CriticalChance,
        Regen,          // 자연회복량 (wiz_p1, prs_p1)
        AttackRange,    // 공격 사거리 (sni_p1, arc_a2)
    }

    [CreateAssetMenu(fileName = "NewEntityStat", menuName = "ScriptableObjects/StatData")]
    public class EntityStatDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string EntityID;
        public string DisplayName;

        [Header("Base Stats")]
        /// <summary> 최대 체력 </summary>
        public float MaxHealth;
        /// <summary> 이동 속도 </summary>
        public float MoveSpeed;
        /// <summary> 자연 회복량 </summary>
        public float Regen;

        [Header("Combat Stats")]
        /// <summary> 공격력 </summary>
        public float Attack;
        /// <summary> 방어력 </summary>
        public float Defense;
        /// <summary> 공격 속도 </summary>
        public float AttackSpeed;
        /// <summary> 공격 사거리 </summary>
        public float AttackRange;
        /// <summary> 치명 확률 (0.0~1.0) </summary>
        [Range(0, 1)] public float CriticalChance;

        [Header("Attack Type")]
        /// <summary> 근접/원거리 구분 </summary>
        public AttackType AttackType;
        /// <summary> 원거리 적 전용 투사체 프리팹 (MELEE 계열은 null) </summary>
        public GameObject ProjectilePrefab;

        // Tip: 레벨업 성장이 필요하다면 AnimationCurve 구조 사용 권장
    }

    /// <summary>
    /// 개별 스탯의 계산 로직을 담당하는 최소 단위
    /// </summary>
    public class StatValue
    {
        /// <summary> SO 기본값 </summary>
        public float RawBase { get; private set; }
        /// <summary> 영구 강화값 (전직 보너스, 아이템 영구 강화) </summary>
        public float PermanentBonus { get; private set; }
        public float Base => RawBase + PermanentBonus;
        public float Additional { get; set; }
        public float Multiplier { get; set; } = 1.0f;

        public StatValue(float baseValue)
        {
            RawBase = baseValue;
            PermanentBonus = 0;
        }

        public float Total => (Base + Additional) * Multiplier;

        public void Reset()
        {
            Additional = 0;
            Multiplier = 1.0f;
        }

        public void AddPermanentBonus(float amount)
        {
            PermanentBonus += amount;
        }
    }

    public class RuntimeStat
    {
        private readonly EntityStatDataSO _baseData;

        // ─────────────────────────────────────────────
        // 체력
        // ─────────────────────────────────────────────

        public float CurrentHealth { get; private set; }

        private Dictionary<StatType, StatValue> _statMap = new();

        // ─────────────────────────────────────────────
        // 스탯 최종값 프로퍼티
        // ─────────────────────────────────────────────

        public float FinalMaxHealth => Mathf.Max(0, _statMap[StatType.MaxHp].Total);
        public float FinalMoveSpd => Mathf.Max(0, _statMap[StatType.MoveSpeed].Total);
        public float FinalAtk => Mathf.Max(0, _statMap[StatType.Attack].Total);
        public float FinalDef => Mathf.Max(0, _statMap[StatType.Defense].Total);
        public float FinalAtkSpd => Mathf.Max(0, _statMap[StatType.AttackSpeed].Total);
        public float FinalCritChance => Mathf.Clamp01(_statMap[StatType.CriticalChance].Total);
        public float FinalRegen => Mathf.Max(0, _statMap[StatType.Regen].Total);
        public float FinalAtkRange => Mathf.Max(0, _statMap[StatType.AttackRange].Total);

        // ─────────────────────────────────────────────
        // 전투 플래그 (HTML 치명타/은신/관통 시스템 대응)
        // ─────────────────────────────────────────────

        /// <summary> 무적 카운터 — 중첩 가능 (은신+스킬 무적 동시 적용 대응) </summary>
        private int _invincibleCount = 0;
        public bool IsInvincible => _invincibleCount > 0;
        public void AddInvincible() => _invincibleCount++;
        public void RemoveInvincible() => _invincibleCount = Mathf.Max(0, _invincibleCount - 1);
        public void ClearInvincible() => _invincibleCount = 0;

        /// <summary> 강제 치명타 (은신, 완전집중 등) </summary>
        public bool ForceCrit { get; set; } = false;

        /// <summary> N번 공격마다 치명타 (0이면 비활성) </summary>
        public int CriticalEvery { get; set; } = 0;

        /// <summary> 치명타 배율 (기본 2.0) </summary>
        public float CriticalMultiplier { get; set; } = 2.0f;

        /// <summary> 관통 투사체 (스나이퍼 관통 패시브) </summary>
        public bool Piercing { get; set; } = false;

        // ─────────────────────────────────────────────
        // 상태이상 관련 (독사 맹독 패시브 대응)
        // ─────────────────────────────────────────────

        /// <summary> 독 피해 배율 (기본 1.0, 맹독 패시브로 2.0) </summary>
        public float PoisonDamageMultiplier { get; set; } = 1.0f;

        /// <summary> 독 지속시간 초 단위 (기본 3.0, 맹독 패시브로 5.0) </summary>
        public float PoisonDuration { get; set; } = 3.0f;

        // ─────────────────────────────────────────────
        // 생성자 및 초기화
        // ─────────────────────────────────────────────

        public RuntimeStat(EntityStatDataSO data)
        {
            _baseData = data;
            InitMap();
            Init();
        }

        private void InitMap()
        {
            _statMap = new Dictionary<StatType, StatValue>
            {
                { StatType.MaxHp,          new StatValue(_baseData.MaxHealth) },
                { StatType.MoveSpeed,      new StatValue(_baseData.MoveSpeed) },
                { StatType.Attack,         new StatValue(_baseData.Attack) },
                { StatType.Defense,        new StatValue(_baseData.Defense) },
                { StatType.AttackSpeed,    new StatValue(_baseData.AttackSpeed) },
                { StatType.AttackRange,    new StatValue(_baseData.AttackRange) },
                { StatType.CriticalChance, new StatValue(_baseData.CriticalChance) },
                { StatType.Regen,          new StatValue(_baseData.Regen) },
            };
        }

        public void Init()
        {
            CurrentHealth = FinalMaxHealth;
            foreach (var stat in _statMap.Values)
                stat.Reset();

            // 전투 플래그 초기화
            _invincibleCount = 0;
            ForceCrit = false;
            CriticalEvery = 0;
            CriticalMultiplier = 2.0f;
            Piercing = false;
            PoisonDamageMultiplier = 1.0f;
            PoisonDuration = 3.0f;
        }

        // ─────────────────────────────────────────────
        // 체력 조작
        // ─────────────────────────────────────────────

        public void SetHealth(float newAmount)
        {
            CurrentHealth = Mathf.Clamp(newAmount, 0, FinalMaxHealth);
        }

        public void ChangeHealth(float amount)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, FinalMaxHealth);
        }

        // ─────────────────────────────────────────────
        // 스탯 모디파이어
        // ─────────────────────────────────────────────

        public void AddModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Additional += value;
        }

        public void RemoveModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Additional -= value;
        }

        /// <summary> Multiplier에 value 가산 (예: ATK×1.4 → MulModifier(Attack, 0.4f)) </summary>
        public void MulModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Multiplier += value;
        }

        /// <summary> Multiplier에서 value 차감 — MulModifier 역연산 </summary>
        public void DivModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Multiplier -= value;
        }

        public void AddPermanent(StatType type, float value, bool isPercent)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.AddPermanentBonus(isPercent ? stat.Total * value : value);
        }
    }
}