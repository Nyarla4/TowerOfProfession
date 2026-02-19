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
    }

    [CreateAssetMenu(fileName = "NewEntityStat", menuName = "ScriptableObjects/StatData")]
    public class EntityStatDataSO : ScriptableObject
    {//EntityStatDataSO : 정적인 데이터

        [Header("Identity")]
        public string EntityID;
        public string DisplayName;

        [Header("Base Stats")]
        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth;
        /// <summary>
        /// 이동 속도
        /// </summary>
        public float MoveSpeed;

        [Header("Combat Stats")]
        /// <summary>
        /// 공격력
        /// </summary>
        public float Attack;
        /// <summary>
        /// 방어력
        /// </summary>
        public float Defense;
        /// <summary>
        /// 공격속도
        /// </summary>
        public float AttackSpeed;
        /// <summary>
        /// 치명확률(0.0~1.0)
        /// </summary>
        [Range(0, 1)] public float CriticalChance;


        // Tip: 만약 레벨업 성장이 필요하다면, 여기에 직접 수치를 적기보다 
        // '성장 곡선(AnimationCurve)'을 구조로 포함시키는 것이 좋습니다.
    }


    /// <summary>
    /// [구조] 개별 스탯의 계산 로직을 담당하는 최소 단위
    /// </summary>
    public class StatValue
    {
        /// <summary> SO 값 </summary>
        public float RawBase { get; private set; }
        /// <summary> 영구 강화 값 </summary>
        public float PermanentBonus { get; private set; }
        public float Base => RawBase + PermanentBonus;
        public float Additional { get; set; }
        public float Multiplier { get; set; } = 1.0f;

        public StatValue(float baseValue)
        {
            RawBase = baseValue;
            PermanentBonus = 0;
        }

        // 최종 값 계산 흐름 (클램프 로직은 외부나 타입별로 처리 가능)
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
    {//RuntimeStat : 동적인 데이터

        private readonly EntityStatDataSO _baseData;

        public float CurrentHealth { get; private set; }

        private Dictionary<StatType, StatValue> _statMap = new();

        public float FinalMaxHealth => Mathf.Max(0, _statMap[StatType.MaxHp].Total);
        public float FinalMoveSpd => Mathf.Max(0, _statMap[StatType.MoveSpeed].Total);
        public float FinalAtk => Mathf.Max(0, _statMap[StatType.Attack].Total);
        public float FinalDef => Mathf.Max(0, _statMap[StatType.Defense].Total);
        public float FinalAtkSpd => Mathf.Max(0, _statMap[StatType.AttackSpeed].Total);
        public float FinalCritChance => Mathf.Clamp01(_statMap[StatType.CriticalChance].Total);

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
                { StatType.CriticalChance, new StatValue(_baseData.CriticalChance) }
            };
        }

        public void Init()
        {
            CurrentHealth = FinalMaxHealth;

            foreach (var stat in _statMap.Values)
            {
                stat.Reset();
            }
        }

        public void SetHealth(float newAmount)
        {
            CurrentHealth = Mathf.Clamp(newAmount, 0, FinalMaxHealth);
        }

        public void ChangeHealth(float amount)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, FinalMaxHealth);
        }

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

        public void MulModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Multiplier += value;
        }

        public void DivModifier(StatType type, float value)
        {
            if (_statMap.TryGetValue(type, out var stat))
                stat.Multiplier -= value;
        }

        public void AddPermanent(StatType type, float value, bool isPercent)
        {
            if (_statMap.TryGetValue(type, out var stat))
            {
                stat.AddPermanentBonus(isPercent ? stat.Total * value : value);
            }
        }
    }
}