using status;
using System.Collections.Generic;
using UnityEngine;

namespace item
{
    /// <summary> 아이템 분류 </summary>
    public enum ItemType
    {
        /// <summary> 장비 </summary>
        Equipment,
        /// <summary> 소모품 </summary>
        Consumable,
        /// <summary> 기타(퀘스트 등) </summary>
        Etc,
    }

    /// <summary> 스탯에 영향 </summary>
    public struct StatModifier
    {
        /// <summary>
        /// 영향을 주는 스탯 
        ///     회복의 경우 MaxHp
        /// </summary>
        public StatType type;
        /// <summary> 영향값 </summary>
        public float value;
        /// <summary> 퍼센트 여부 </summary>
        public bool isPercent;
    }

    [CreateAssetMenu(fileName = "NewItemData", menuName = "SO/Item/ItemData")]
    public class ItemDataSO : ScriptableObject
    {
        /// <summary> 이름 </summary>
        [Header("Basic Info")]
        public string ItemName;
        /// <summary> 분류 </summary>
        public ItemType ItemType;
        /// <summary> 아이콘 </summary>
        public Sprite Icon;

        /// <summary> 스탯에 주는 영향 </summary>
        [Header("Stat Modifiers")]
        public List<StatModifier> Modifiers;

        /// <summary> 지속시간(0이면 영구적용) </summary>
        [Header("Consumable Effect")]
        public float duration;
        /// <summary> 회복인지 여부 </summary>
        public bool isHeal;
    }

    /// <summary>
    /// 아이템 => 엔티티
    /// </summary>
    public static class ItemProcessor
    {
        /// <summary>
        /// 아이템 사용
        /// </summary>
        public static void ExecuteItemUsage(Entity target, ItemDataSO itemData)
        {
            if(target == null || itemData == null)
            {
                Debug.LogWarning("item_ItemProcessor_ExecuteItemUsage: target 혹은 itemData 누락");
                return;
            }

            if (itemData.ItemType == ItemType.Equipment)
            {
                ApplyEquipment(target, itemData);
            }
            else
            {
                ApplyConsumable(target, itemData);
            }
        }

        private static void ApplyEquipment(Entity target, ItemDataSO data)
        {
            if (data == null || data.Modifiers == null)
            {
                Debug.LogWarning("item_ItemProcessor_ApplyEquipment: data 혹은 data.Modifiers 누락");
                return;
            }
            // 장착 시 Stat을 변화시키는 흐름
            foreach (var modi in data.Modifiers)
            {
                if (modi.isPercent)
                {
                    target.Stat.MulModifier(modi.type, modi.value);
                }
                else
                {
                    target.Stat.AddModifier(modi.type, modi.value);
                }
            }
            Debug.Log($"{data.ItemName} 장착됨.");
        }

        private static void RemoveEquipment(Entity target, ItemDataSO data)
        {
            if (data == null || data.Modifiers == null)
            {
                Debug.LogWarning("item_ItemProcessor_RemoveEquipment: data 혹은 data.Modifiers 누락");
                return;
            }
            // 장착 시 Stat을 변화시키는 흐름
            foreach (var modi in data.Modifiers)
            {
                if (modi.isPercent)
                {
                    target.Stat.DivModifier(modi.type, modi.value);
                }
                else
                {
                    target.Stat.RemoveModifier(modi.type, modi.value);
                }
            }
            Debug.Log($"{data.ItemName} 해제됨.");
        }

        private static void ApplyConsumable(Entity target, ItemDataSO data)
        {
            if (data.isHeal)
            {
                ProcessRecovery(target, data);
            }
            else if (data.duration > 0)
            {
                // 일정 시간 스탯 상승 (도핑/버프) 흐름
                ProcessBuff(target, data);
                Debug.Log($"{data.ItemName} 사용: {data.duration}초간 능력치 상승.");
            }
            else
            {
                // 영구적 스탯 상승 (예: 힘의 영약 - 영구 체력 증가 등)
                ProcessPermanentPowerUp(target, data);
                Debug.Log($"{data.ItemName} 사용: 능력치 영구 상승.");
            }
        }

        private static void ProcessRecovery(Entity target, ItemDataSO data)
        {
            int modiIdx = data.Modifiers.FindIndex(f => f.type == StatType.MaxHp);
            if(modiIdx < 0)
            {
                Debug.LogWarning("item_ItemProcessor_ProcessRecovery: data의 Modifiers에 MaxHp 타입 없음");
                return;
            }
            StatModifier hpModi = data.Modifiers[modiIdx];
            if (hpModi.isPercent)
            {
                target.TakeHeal(target.Stat.FinalMaxHealth * hpModi.value);
            }
            else
            {
                target.TakeHeal(hpModi.value);
            }            
        }

        private static void ProcessBuff(Entity target, ItemDataSO data)
        {
            // 버프 흐름: BuffSystem(별도 흐름 관리자)에 데이터 위임
            // BuffSystem.Instance.AddBuff(target, data.Modifiers, data.duration);
            Debug.Log($"[버프] {data.ItemName} 사용: {data.duration}초간 효과 지속.");
        }

        private static void ProcessPermanentPowerUp(Entity target, ItemDataSO data)
        {
            // 영구 강화 흐름: Entity의 BaseStat 구조 자체를 수정하는 흐름
            foreach (var modi in data.Modifiers)
            {
                target.Stat.AddPermanent(modi.type, modi.value, modi.isPercent);
            }
            Debug.Log($"[영구 강화] {data.ItemName} 사용: 영구적으로 능력치가 상승했습니다.");
        }
    }
}