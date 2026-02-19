using UnityEngine;

namespace status
{
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
        public float AttackDamage;
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

    public class RuntimeStat
    {//RuntimeStat : 동적인 데이터

        private readonly EntityStatDataSO _baseData;

        public float CurrentHealth;

        public float AdditionalMoveSpeed { get; set; }
        public float MultiplierMoveSpeed { get; set; }
        public float FinalMoveSpeed => Mathf.Max(0, (_baseData.MoveSpeed + AdditionalMoveSpeed) * MultiplierMoveSpeed);

        public float AdditionalAtk { get; set; }
        public float MultiplierAtk { get; set; }
        public float FinalAtk => Mathf.Max(0, (_baseData.AttackDamage + AdditionalAtk) * MultiplierAtk);

        public float AdditionalDef { get; set; }
        public float MultiplierDef { get; set; }
        public float FinalDef => Mathf.Max(0, (_baseData.Defense + AdditionalDef) * MultiplierDef);

        public RuntimeStat(EntityStatDataSO data)
        {
            _baseData = data;
            Init();
        }

        //초기화
        public void Init()
        {
            CurrentHealth = _baseData.MaxHealth;

            AdditionalMoveSpeed = 0;
            MultiplierMoveSpeed = 1.0f;
            AdditionalAtk = 0;
            MultiplierAtk = 1.0f;
            AdditionalDef = 0;
            MultiplierDef = 1.0f;
        }
    }
}