using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using status;

/// <summary>
/// HTML skillManager.js 대응
/// 패시브/액티브 스킬 로직 전담
/// 모든 스킬 로직은 SkillID 분기로 처리
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // 전직 및 스킬 초기화
    // ─────────────────────────────────────────────

    public void ApplyJobSkills(PlayerEntity player, JobDataSO jobData)
    {
        // 1. 기존 전투 이벤트 구독 및 코루틴 싹 다 초기화 (꼬임 방지)
        player.ClearCombatEvents();
        StopAllCoroutines();

        if (jobData == null) return;

        // 2. 새 직업의 패시브(구조)를 순회하며 적절한 이벤트(흐름)에 연결
        foreach (var passive in jobData.Passives)
        {
            // 예시 1: 프리스트 흡혈 (prs_p2)
            if (passive.SkillID == "prs_p2")
            {
                // 플레이어가 타격 성공 이벤트를 쏘면 -> 흡혈 로직을 실행하라
                player.OnAfterAttackHit += (target, damage) => ApplyLifeSteal(player, damage);
            }
            // 예시 2: 메이지 연쇄폭발 (mge_p3)
            else if (passive.SkillID == "mge_p3")
            {
                // 플레이어가 투사체를 생성하면 -> 그 투사체에 폭발 로직을 달아라
                player.OnProjectileSpawned += SubscribeChainExplosion;
            }
            // 예시 3: 팔라딘 성스러운 오라 (pal_p2)
            else if (passive.SkillID == "pal_p2")
            {
                // 초당 주변을 검사하는 독립적인 흐름(코루틴) 실행
                StartCoroutine(HolyAuraRoutine(player, 5f, 10f)); // 반경 5f, 틱당 10 회복 (임시 수치)
            }
        }
    }

    // ─────────────────────────────────────────────
    // 실제 스킬 구현부 (구조 데이터 연산)
    // ─────────────────────────────────────────────

    // [흡혈]
    private void ApplyLifeSteal(PlayerEntity player, float damage)
    {
        // 데미지의 20% 회복
        player.Stat.ChangeHealth(damage * 0.2f);
    }

    // [연쇄 폭발 구독 세팅]
    private void SubscribeChainExplosion(Projectile proj)
    {
        // 투사체의 명중 이벤트에 람다식으로 폭발 로직 연결
        proj.OnProjectileHit += (projectile, enemy) =>
        {
            // 타격 지점을 중심으로 반경 2f 내에 폭발 데미지 (임시 수치)
            DealAreaDamage(projectile, enemy.transform.position, 2f, 50f);
        };
    }

    // [광역 데미지 유틸]
    private void DealAreaDamage(Projectile proj, Vector3 center, float radius, float damage)
    {
        var enemies = EnemyManager.Instance.GetAllEnemies();
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            if (Vector2.Distance(center, e.transform.position) <= radius)
            {
                // 여기서 다시 OnProjectileHit가 발생하지 않도록 주의 (직접 TakeDamage만 호출)
                e.TakeDamage(proj.Attacker, damage);
            }
        }
    }

    // [성스러운 오라 틱 연산]
    private IEnumerator HolyAuraRoutine(PlayerEntity player, float radius, float healAmount)
    {
        while (true)
        {
            // EnemyManager에게 순수하게 목록만 받아와서 SkillManager가 직접 거리를 잼
            var enemies = EnemyManager.Instance.GetAllEnemies();
            bool hasNearby = false;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                if (Vector2.Distance(player.transform.position, enemy.transform.position) <= radius)
                {
                    hasNearby = true;
                    break;
                }
            }

            if (hasNearby)
            {
                player.Stat.ChangeHealth(healAmount);
            }

            yield return new WaitForSeconds(1f); // 1초마다 틱
        }
    }

    // ─────────────────────────────────────────────
    // 쿨다운 추적 (UI 연동용)
    // ─────────────────────────────────────────────

    // 스킬 ID별 마지막 사용 시간 저장
    private Dictionary<string, float> _cooldownMap = new Dictionary<string, float>();

    /// <summary>
    /// UI에서 쿨타임 비율을 알아가기 위한 메서드
    /// 1f면 준비 완료, 0f면 방금 사용함
    /// </summary>
    public float GetCooldownRatio(string skillID, float cooldown)
    {
        if (cooldown <= 0f) return 1f;

        if (_cooldownMap.TryGetValue(skillID, out float lastUseTime))
        {
            float elapsed = Time.time - lastUseTime;
            return Mathf.Clamp01(elapsed / cooldown);
        }

        return 1f; // 한 번도 사용 안 했으면 100% 준비 완료 상태
    }

    /// <summary>
    /// 향후 SkillManager가 특정 액티브 스킬을 대신 발동시켜 줄 때, 이 함수로 시간을 기록합니다.
    /// </summary>
    public void RecordSkillUse(string skillID)
    {
        _cooldownMap[skillID] = Time.time;
    }

    // ─────────────────────────────────────────────
    // 강화 카드(업그레이드 모디파이어) 추적
    // ─────────────────────────────────────────────

    // 유저가 획득한 패시브/스킬 변형 ID들을 중복 없이 저장하는 보관소
    private HashSet<string> _acquiredModifiers = new HashSet<string>();

    /// <summary>
    /// LevelUpManager에서 카드를 선택했을 때 호출되어 변형 ID를 주입합니다.
    /// </summary>
    public void AddUpgradeModifier(string modID)
    {
        // 공용 스탯 카드처럼 ModifierID가 없는 경우는 무시
        if (string.IsNullOrEmpty(modID)) return;

        _acquiredModifiers.Add(modID);
        Debug.Log($"[SkillManager] 모디파이어 획득 완료: {modID}");
    }

    /// <summary>
    /// 이후 스킬이 발동될 때, 특정 모디파이어(예: 다중발사)를 가지고 있는지 검사할 때 사용합니다.
    /// </summary>
    public bool HasModifier(string modID)
    {
        return _acquiredModifiers.Contains(modID);
    }
}