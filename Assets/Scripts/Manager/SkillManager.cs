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

    // 액티브 스킬 쿨타임 추적 (SkillID → 마지막 사용 시간)
    private Dictionary<string, float> _cooldownMap = new();

    // 현재 활성화된 버프 스킬 (SkillID → 종료 시간)
    private Dictionary<string, float> _activeBuffMap = new();

    // 현재 적용 중인 CONDITION 패시브 추적
    private HashSet<string> _activePassiveIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // 1. 패시브 — PERMANENT
    // ─────────────────────────────────────────────

    /// <summary>
    /// 전직 시 PERMANENT 패시브 일괄 적용
    /// HTML: initJobPassives()
    /// </summary>
    public void ApplyPermanentPassives(PlayerEntity player, JobDataSO jobData)
    {
        if (jobData == null) return;

        foreach (var passive in jobData.Passives)
        {
            if (passive.Type != PassiveType.PERMANENT) continue;
            ApplyPermanentEffect(player, passive.SkillID);
        }
    }

    private void ApplyPermanentEffect(PlayerEntity player, string skillId)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            // ── 견습 ──
            case "app_p1": // 견습의 발걸음
                stat.AddModifier(StatType.MoveSpeed, 0.5f); break;

            // ── 전사 ──
            case "war_p1": // 철갑
                stat.AddModifier(StatType.Defense, 10f);
                stat.AddModifier(StatType.MaxHp, 100f); break;

            // ── 궁수 ──
            case "arc_p1": // 바람의 발걸음
                stat.AddModifier(StatType.MoveSpeed, 0.8f); break;

            // ── 마법사 ──
            case "wiz_p1": // 마력 순환
                stat.AddModifier(StatType.Regen, 10f); break;

            // ── 도적 ──
            case "rog_p1": // 암살자의 발걸음
                stat.AddModifier(StatType.MoveSpeed, 1.2f);
                stat.AddModifier(StatType.Defense, -5f); break;

            // ── 버서커 ──
            case "ber_p1": // 피의 각성
                stat.AddModifier(StatType.Attack, 10f); break;

            // ── 팔라딘 ──
            case "pal_p1": // 신성한 갑옷
                stat.AddModifier(StatType.MaxHp, 200f);
                stat.AddModifier(StatType.Defense, 20f); break;

            // ── 스나이퍼 ──
            case "sni_p1": // 정밀조준
                stat.AddModifier(StatType.AttackRange, 100f); break;
            case "sni_p3": // 관통
                stat.Piercing = true; break;

            // ── 블랙메이지 ──
            case "blk_p1": // 마력증폭
                stat.AddModifier(StatType.Attack, 20f); break;

            // ── 프리스트 ──
            case "prs_p1": // 신성한 생명력
                stat.AddModifier(StatType.MaxHp, 150f);
                stat.AddModifier(StatType.Regen, 20f); break;

            // ── 어쌔신 ──
            case "ass_p1": // 달인의 손
                stat.CriticalMultiplier = 3.5f; break;

            // ── 독사 ──
            case "vip_p1": // 맹독
                stat.PoisonDamageMultiplier = 2.0f;
                stat.PoisonDuration = 5f; break;

            default:
                Debug.LogWarning($"SkillManager_ApplyPermanentEffect: 미등록 SkillID — {skillId}");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 2. 패시브 — CONDITION
    // ─────────────────────────────────────────────

    /// <summary>
    /// 매 프레임 CONDITION 패시브 체크
    /// HTML: checkPassiveConditions()
    /// </summary>
    public void UpdateConditionPassives(PlayerEntity player, JobDataSO jobData, List<Enemy> enemies)
    {
        if (jobData == null) return;

        foreach (var passive in jobData.Passives)
        {
            if (passive.Type != PassiveType.CONDITION) continue;

            bool conditionMet = CheckCondition(player, passive.SkillID, enemies);
            bool wasActive = _activePassiveIds.Contains(passive.SkillID);

            if (conditionMet && !wasActive)
            {
                ApplyConditionEffect(player, passive.SkillID);
                _activePassiveIds.Add(passive.SkillID);
            }
            else if (!conditionMet && wasActive)
            {
                RemoveConditionEffect(player, passive.SkillID);
                _activePassiveIds.Remove(passive.SkillID);
            }
        }
    }

    private bool CheckCondition(PlayerEntity player, string skillId, List<Enemy> enemies)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            // ── 견습 ──
            case "app_p2": // 위기 감지 (HP 30% 이하)
                return stat.CurrentHealth / stat.FinalMaxHealth <= 0.3f;
            case "app_p3": // 휴식의 지혜 (마을 내)
                return player.IsInTown;

            // ── 전사 ──
            case "war_p2": // 광전사의 분노 (HP 35% 이하)
                return stat.CurrentHealth / stat.FinalMaxHealth <= 0.35f;

            // ── 궁수 ──
            case "arc_p2": // 집중 (근처 150 이내 적 없음)
                return !HasEnemyInRange(player, enemies, 150f);

            // ── 마법사 ──
            case "wiz_p2": // 유리 대포 (풀피)
                return stat.CurrentHealth >= stat.FinalMaxHealth;

            // ── 도적 ──
            case "rog_p2": // 은신 (정지 중)
                return !player.IsMoving;

            // ── 버서커 ──
            case "ber_p2": // 광전사 (HP 50% 이하)
                return stat.CurrentHealth / stat.FinalMaxHealth <= 0.5f;

            // ── 팔라딘 ──
            case "pal_p2": // 성스러운 오라 (철벽방어 활성 중)
                return _activeBuffMap.ContainsKey("pal_a1");

            // ── 스나이퍼 ──
            case "sni_p2": // 완전집중 (정지 중)
                return !player.IsMoving;

            // ── 레인저 ──
            case "ran_p1": // 유격대 (이동 중)
                return player.IsMoving;

            // ── 블랙메이지 ──
            case "blk_p2": // 유리대포 (HP 70% 이상)
                return stat.CurrentHealth / stat.FinalMaxHealth >= 0.7f;

            // ── 프리스트 ──
            case "prs_p2": // 신의 가호 (HP 80% 이상)
                return stat.CurrentHealth / stat.FinalMaxHealth >= 0.8f;

            // ── 어쌔신 ──
            case "ass_p2": // 사냥감 포착 (근처 200 이내 HP 50% 이하 적)
                return HasLowHpEnemyInRange(player, enemies, 200f, 0.5f);

            // ── 독사 ──
            case "vip_p2": // 독 친화 (근처 200 이내 독 걸린 적)
                return HasPoisonedEnemyInRange(player, enemies, 200f);

            default:
                return false;
        }
    }

    private void ApplyConditionEffect(PlayerEntity player, string skillId)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            case "app_p2": stat.AddModifier(StatType.Defense, 5f); break;
            case "app_p3": player.RegenMultiplier = 2f; break;
            case "war_p2":
                stat.MulModifier(StatType.Attack, 0.4f);
                stat.AddModifier(StatType.MoveSpeed, 0.3f); break;
            case "arc_p2": stat.AddModifier(StatType.Attack, 20f); break;
            case "wiz_p2": stat.MulModifier(StatType.Attack, 0.6f); break;
            case "rog_p2":
                stat.AddInvincible();
                stat.ForceCrit = true; break;
            case "ber_p2":
                stat.MulModifier(StatType.Attack, 0.8f);
                stat.AddModifier(StatType.MoveSpeed, 0.5f); break;
            case "pal_p2": player.HolyAuraActive = true; break;
            case "sni_p2":
                stat.AddInvincible();
                stat.ForceCrit = true; break;
            case "ran_p1":
                stat.AddModifier(StatType.Attack, 15f);
                stat.MulModifier(StatType.AttackSpeed, 0.5f); break;
            case "blk_p2": stat.MulModifier(StatType.Attack, 1.0f); break;
            case "prs_p2":
                stat.MulModifier(StatType.Attack, 0.5f);
                player.RegenMultiplier = 2f; break;
            case "ass_p2": stat.MulModifier(StatType.Attack, 1.0f); break;
            case "vip_p2": stat.MulModifier(StatType.Attack, 0.8f); break;
        }
    }

    private void RemoveConditionEffect(PlayerEntity player, string skillId)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            case "app_p2": stat.RemoveModifier(StatType.Defense, 5f); break;
            case "app_p3": player.RegenMultiplier = 1f; break;
            case "war_p2":
                stat.DivModifier(StatType.Attack, 0.4f);
                stat.RemoveModifier(StatType.MoveSpeed, 0.3f); break;
            case "arc_p2": stat.RemoveModifier(StatType.Attack, 20f); break;
            case "wiz_p2": stat.DivModifier(StatType.Attack, 0.6f); break;
            case "rog_p2":
                stat.RemoveInvincible();
                stat.ForceCrit = false; break;
            case "ber_p2":
                stat.DivModifier(StatType.Attack, 0.8f);
                stat.RemoveModifier(StatType.MoveSpeed, 0.5f); break;
            case "pal_p2": player.HolyAuraActive = false; break;
            case "sni_p2":
                stat.RemoveInvincible();
                stat.ForceCrit = false; break;
            case "ran_p1":
                stat.RemoveModifier(StatType.Attack, 15f);
                stat.DivModifier(StatType.AttackSpeed, 0.5f); break;
            case "blk_p2": stat.DivModifier(StatType.Attack, 1.0f); break;
            case "prs_p2":
                stat.DivModifier(StatType.Attack, 0.5f);
                player.RegenMultiplier = 1f; break;
            case "ass_p2": stat.DivModifier(StatType.Attack, 1.0f); break;
            case "vip_p2": stat.DivModifier(StatType.Attack, 0.8f); break;
        }
    }

    // ─────────────────────────────────────────────
    // 3. 패시브 — EVENT
    // ─────────────────────────────────────────────

    /// <summary>
    /// 이벤트 발생 시 해당 패시브 트리거
    /// HTML: triggerEventPassive()
    /// </summary>
    public void TriggerEventPassive(PlayerEntity player, JobDataSO jobData,
        EventType eventType, Enemy defender = null)
    {
        if (jobData == null) return;

        foreach (var passive in jobData.Passives)
        {
            if (passive.Type != PassiveType.EVENT) continue;
            if (passive.EventType != eventType) continue;
            ExecuteEventPassive(player, passive.SkillID, defender);
        }
    }

    private void ExecuteEventPassive(PlayerEntity player, string skillId, Enemy defender)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            // ── 전사 ──
            case "war_p3": // 전장의 치유 (ON_KILL, HP+15)
                player.Stat.ChangeHealth(15f); break;

            // ── 궁수 ──
            case "arc_p3": // 연사 (ON_ATTACK, 10% 추가공격)
                if (Random.value <= 0.1f)
                    player.TriggerExtraAttack(); break;

            // ── 마법사 ──
            case "wiz_p3": // 마력잔재 (ON_SKILL_USE, 다음 투사체 크기×2)
                player.NextProjectileScale = 2f; break;

            // ── 도적 ──
            case "rog_p3": // 기습 (ON_ATTACK, forceCrit 중 은신 해제)
                if (stat.ForceCrit)
                {
                    stat.ForceCrit = false;
                    stat.RemoveInvincible();
                    // _activePassiveIds 상태는 다음 프레임 UpdateConditionPassives에서 자동 해제
                }
                break;

            // ── 버서커 ──
            case "ber_p3": // 학살의 쾌감 (ON_KILL, HP+30, 용기 쿨 -2초)
                stat.ChangeHealth(30f);
                ReduceCooldown("ber_a1", 2f); break;

            // ── 팔라딘 ──
            case "pal_p3": // 신의 가호 (ON_KILL, DEF+2 최대 +20)
                player.AddHolyStack(); break;

            // ── 레인저 ──
            case "ran_p2": // 연속기동 (ON_ATTACK, 후퇴난사 쿨 조건부 감소)
                if (player.IsMoving)
                    ReduceCooldown("ran_a1", 0.5f); break;
            case "ran_p3": // 산탄 (ON_ATTACK, 30% 확률)
                if (Random.value <= 0.3f)
                    player.TriggerScatterShot(); break;

            // ── 블랙메이지 ──
            case "blk_p3": // 연쇄폭발 (ON_SKILL_USE)
                player.ChainExplosionActive = true;
                // ⚠️ ChainExplosionActive = false 리셋은
                // Projectile 시스템에서 폭발 처리 후 수행 필요
                break;

            // ── 프리스트 ──
            case "prs_p3": // 생명흡수 (ON_KILL, MaxHP의 10% 회복)
                stat.ChangeHealth(stat.FinalMaxHealth * 0.1f); break;

            // ── 어쌔신 ──
            case "ass_p3": // 연속 암살 (ON_KILL, 즉시 은신 재진입)
                stat.AddInvincible();
                stat.ForceCrit = true;
                ResetCooldown("ass_a1"); break;

            // ── 독사 ──
            case "vip_p3": // 독 전이 (ON_KILL, 처치 적의 독 반경 100 전이)
                if (defender != null && defender.IsPoison)
                    SpreadPoison(player, defender, 100f); break;

            default:
                Debug.LogWarning($"SkillManager_ExecuteEventPassive: 미등록 SkillID — {skillId}");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 4. 액티브 스킬
    // ─────────────────────────────────────────────

    /// <summary>
    /// 액티브 스킬 사용
    /// HTML: useActiveSkill()
    /// </summary>
    public void UseActiveSkill(PlayerEntity player, JobDataSO jobData,
        int slotIndex, List<Enemy> enemies)
    {
        if (jobData == null || jobData.Actives == null) return;
        if (slotIndex >= jobData.Actives.Length) return;

        var activeData = jobData.Actives[slotIndex];
        if (!IsOffCooldown(activeData.SkillID, activeData.Cooldown)) return;

        // ON_SKILL_USE 이벤트 트리거
        TriggerEventPassive(player, jobData, EventType.ON_SKILL_USE);

        // 버프 지속시간이 있으면 등록
        if (activeData.Duration > 0)
            RegisterBuff(activeData.SkillID, activeData.Duration);

        // 쿨타임 등록
        _cooldownMap[activeData.SkillID] = Time.time;

        // 실제 스킬 실행
        ExecuteActiveSkill(player, activeData, enemies);
    }

    private void ExecuteActiveSkill(PlayerEntity player, ActiveData data, List<Enemy> enemies)
    {
        switch (data.SkillID)
        {
            // ── 견습 ──
            case "app_a1": // 위협의 외침 (반경 120 광역)
                DealAreaDamage(player, enemies, data.Radius, 1f); break;
            case "app_a2": // 긴급 회피 (대시 + 무적 0.2초)
                player.Dash(data.DashDistance);
                StartCoroutine(TemporaryInvincible(player, 0.2f)); break;

            // ── 전사 ──
            case "war_a1": // 용기 (ATK×1.5 버프)
                player.Stat.MulModifier(StatType.Attack, 0.5f); break;
            case "war_a2": // 방어태세 (DEF×2, 이속-50%)
                player.Stat.MulModifier(StatType.Defense, 1.0f);
                player.Stat.MulModifier(StatType.MoveSpeed, -0.5f); break;

            // ── 궁수 ──
            case "arc_a1": // 후퇴사격 (뒤로 대시 + 즉시 공격)
                player.DashBackward(data.DashDistance);
                player.TriggerInstantAttack(); break;
            case "arc_a2": // 화살 집중 (사거리×1.5, ATK+10)
                player.Stat.MulModifier(StatType.AttackRange, 0.5f);
                player.Stat.AddModifier(StatType.Attack, 10f); break;

            // ── 마법사 ──
            case "wiz_a1": // 메테오 (반경 250, ATK×2.5)
                DealAreaDamage(player, enemies, data.Radius, data.Multiplier); break;
            case "wiz_a2": // 마력 보호막 (무적 + 이속-70%)
                player.Stat.AddInvincible();
                player.Stat.MulModifier(StatType.MoveSpeed, -0.7f); break;

            // ── 도적 ──
            case "rog_a1": // 그림자 질주 (대시 + 반경 80)
                player.Dash(data.DashDistance);
                DealAreaDamage(player, enemies, data.Radius, data.Multiplier); break;
            case "rog_a2": // 독 단검 (가장 가까운 적 독 부여)
                ApplyPoisonToNearest(player, enemies, data.Multiplier, 3f); break;

            // ── 버서커 ──
            case "ber_a1": // 피의 맹세 (HP 30% 소모, 소모량×5 광역)
                {
                    float cost = player.Stat.FinalMaxHealth * data.SelfHpCostPercent;
                    player.Stat.ChangeHealth(-cost);
                    DealAreaDamage(player, enemies, data.Radius, cost * 5f, isRaw: true); break;
                }
            case "ber_a2": // 돌진 (대시 + 경로 ATK×1.2)
                player.Dash(data.DashDistance);
                DealAreaDamage(player, enemies, 60f, data.Multiplier); break;

            // ── 팔라딘 ──
            case "pal_a1": // 철벽방어 (DEF×3, 이속×0.3)
                player.Stat.MulModifier(StatType.Defense, 2.0f);
                player.Stat.MulModifier(StatType.MoveSpeed, -0.7f); break;
            case "pal_a2": // 도발 (반경 200 적 강제 CHASE)
                ForceChaseAllInRange(player, enemies, data.Radius); break;

            // ── 스나이퍼 ──
            case "sni_a1": // 저격 (가장 가까운 적 ATK×4.0)
                DealDamageToNearest(player, enemies, data.Multiplier); break;
            case "sni_a2": // 집중사격 (공속×3, forceCrit)
                player.Stat.MulModifier(StatType.AttackSpeed, 2.0f);
                player.Stat.ForceCrit = true; break;

            // ── 레인저 ──
            case "ran_a1": // 후퇴난사 (대시 + 즉시 공격)
                player.DashBackward(data.DashDistance);
                player.TriggerInstantAttack(); break;
            case "ran_a2": // 전력질주 (이속×2, 공속×1.5)
                player.Stat.MulModifier(StatType.MoveSpeed, 1.0f);
                player.Stat.MulModifier(StatType.AttackSpeed, 0.5f); break;

            // ── 블랙메이지 ──
            case "blk_a1": // 블랙홀 (반경 200 끌어당기며 ATK×1.5)
                StartCoroutine(BlackHole(player, enemies, data.Radius,
                    data.Multiplier, data.Duration)); break;
            case "blk_a2": // 메테오 (반경 400, ATK×4.0)
                DealAreaDamage(player, enemies, data.Radius, data.Multiplier); break;

            // ── 프리스트 ──
            case "prs_a1": // 신성치유 (MaxHP 40% 즉시 회복)
                player.Stat.ChangeHealth(player.Stat.FinalMaxHealth * 0.4f); break;
            case "prs_a2": // 성스러운 분노 (ATK×2.0 + 흡혈)
                player.Stat.MulModifier(StatType.Attack, 1.0f);
                player.LifeStealActive = true; break;

            // ── 어쌔신 ──
            case "ass_a1": // 그림자 질주 강화 (대시 + 반경 100)
                player.Dash(data.DashDistance);
                DealAreaDamage(player, enemies, data.Radius,
                    player.Stat.ForceCrit ? data.Multiplier * 1.6f : data.Multiplier); break;
            case "ass_a2": // 처형 (HP 25% 이하 즉시 처치)
                ExecuteNearest(player, enemies, data.ThresholdPercent); break;

            // ── 독사 ──
            case "vip_a1": // 독 안개 (반경 150 독 부여)
                player.PoisonAuraActive = true; break;
            case "vip_a2": // 마비 단검 (단일 마비 + 독)
                StunAndPoisonNearest(player, enemies, 2f, data.Multiplier, 3f); break;

            default:
                Debug.LogWarning($"SkillManager_ExecuteActiveSkill: 미등록 SkillID — {data.SkillID}");
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 5. 버프 종료 처리
    // ─────────────────────────────────────────────

    /// <summary>
    /// 매 프레임 버프 종료 체크
    /// HTML: checkSkillEndTimes()
    /// </summary>
    public void CheckSkillEndTimes(PlayerEntity player, JobDataSO jobData)
    {
        var expired = new List<string>();
        foreach (var kv in _activeBuffMap)
        {
            if (Time.time >= kv.Value)
                expired.Add(kv.Key);
        }

        foreach (var skillId in expired)
        {
            _activeBuffMap.Remove(skillId);
            OnBuffEnd(player, skillId);
        }
    }

    private void OnBuffEnd(PlayerEntity player, string skillId)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            case "war_a1": stat.DivModifier(StatType.Attack, 0.5f); break;
            case "war_a2":
                stat.DivModifier(StatType.Defense, 1.0f);
                stat.DivModifier(StatType.MoveSpeed, -0.5f); break;
            case "arc_a2":
                stat.DivModifier(StatType.AttackRange, 0.5f);
                stat.RemoveModifier(StatType.Attack, 10f); break;
            case "wiz_a2":
                stat.RemoveInvincible();
                stat.DivModifier(StatType.MoveSpeed, -0.7f); break;
            case "pal_a1":
                stat.DivModifier(StatType.Defense, 2.0f);
                stat.DivModifier(StatType.MoveSpeed, -0.7f); break;
            case "pal_a2":
                ReturnEnemiesFromChase(); break;
            case "sni_a2":
                stat.DivModifier(StatType.AttackSpeed, 2.0f);
                stat.ForceCrit = false; break;
            case "ran_a2":
                stat.DivModifier(StatType.MoveSpeed, 1.0f);
                stat.DivModifier(StatType.AttackSpeed, 0.5f); break;
            case "prs_a2":
                stat.DivModifier(StatType.Attack, 1.0f);
                player.LifeStealActive = false; break;
            case "vip_a1":
                player.PoisonAuraActive = false; break;
        }
    }

    // ─────────────────────────────────────────────
    // 6. 전직 시 초기화
    // ─────────────────────────────────────────────

    /// <summary>
    /// 전직 시 스킬 상태 초기화 (스탯 역적용 없이 맵만 클리어)
    /// ChangeJob에서 스탯 역적용은 RemovePermanentPassives로 별도 처리
    /// </summary>
    public void ResetOnJobChange()
    {
        _cooldownMap.Clear();
        _activeBuffMap.Clear();
        _activePassiveIds.Clear();
    }

    /// <summary>
    /// 전직 시 이전 직업 PERMANENT 패시브 스탯 역적용
    /// PlayerEntity.ChangeJob에서 이전 직업 제거 시 호출
    /// </summary>
    public void RemovePermanentPassives(PlayerEntity player, JobDataSO jobData)
    {
        if (jobData == null) return;
        foreach (var passive in jobData.Passives)
        {
            if (passive.Type != PassiveType.PERMANENT) continue;
            RemovePermanentEffect(player, passive.SkillID);
        }
    }

    private void RemovePermanentEffect(PlayerEntity player, string skillId)
    {
        var stat = player.Stat;
        switch (skillId)
        {
            case "app_p1": stat.RemoveModifier(StatType.MoveSpeed, 0.5f); break;
            case "war_p1":
                stat.RemoveModifier(StatType.Defense, 10f);
                stat.RemoveModifier(StatType.MaxHp, 100f); break;
            case "arc_p1": stat.RemoveModifier(StatType.MoveSpeed, 0.8f); break;
            case "wiz_p1": stat.RemoveModifier(StatType.Regen, 10f); break;
            case "rog_p1":
                stat.RemoveModifier(StatType.MoveSpeed, 1.2f);
                stat.RemoveModifier(StatType.Defense, -5f); break;
            case "ber_p1": stat.RemoveModifier(StatType.Attack, 10f); break;
            case "pal_p1":
                stat.RemoveModifier(StatType.MaxHp, 200f);
                stat.RemoveModifier(StatType.Defense, 20f); break;
            case "sni_p1": stat.RemoveModifier(StatType.AttackRange, 100f); break;
            case "sni_p3": stat.Piercing = false; break;
            case "blk_p1": stat.RemoveModifier(StatType.Attack, 20f); break;
            case "prs_p1":
                stat.RemoveModifier(StatType.MaxHp, 150f);
                stat.RemoveModifier(StatType.Regen, 20f); break;
            case "ass_p1": stat.CriticalMultiplier = 2.0f; break; // RuntimeStat 기본값 복원
            case "vip_p1":
                stat.PoisonDamageMultiplier = 1.0f;
                stat.PoisonDuration = 3.0f; break; // RuntimeStat 기본값 복원
        }
    }

    /// <summary>
    /// 리스폰 시 활성 버프/CONDITION 패시브 스탯 역적용 후 클리어
    /// 버프 중 사망해도 리스폰 시 정상 스탯으로 복원됨
    /// </summary>
    public void ClearActiveBuffs(PlayerEntity player)
    {
        // 활성 버프 역적용
        foreach (var skillId in new List<string>(_activeBuffMap.Keys))
            OnBuffEnd(player, skillId);
        _activeBuffMap.Clear();

        // 활성 CONDITION 패시브 역적용
        foreach (var skillId in new List<string>(_activePassiveIds))
            RemoveConditionEffect(player, skillId);
        _activePassiveIds.Clear();
    }

    // ─────────────────────────────────────────────
    // 유틸 — 전투
    // ─────────────────────────────────────────────

    private void DealAreaDamage(PlayerEntity player, List<Enemy> enemies,
        float radius, float multiplier, bool isRaw = false)
    {
        var pos = player.transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= radius)
            {
                if (isRaw) enemy.TakeDamage(player, multiplier);
                else enemy.TakeDamage(player, player.Stat.FinalAtk * multiplier);
            }
        }
    }

    private void DealDamageToNearest(PlayerEntity player, List<Enemy> enemies, float multiplier)
    {
        var nearest = GetNearest(player, enemies);
        nearest?.TakeDamage(player, player.Stat.FinalAtk * multiplier);
    }

    private void ApplyPoisonToNearest(PlayerEntity player, List<Enemy> enemies,
        float multiplier, float duration)
    {
        var nearest = GetNearest(player, enemies);
        if (nearest == null) return;
        float dmg = player.Stat.FinalAtk * multiplier * player.Stat.PoisonDamageMultiplier;
        float dur = player.Stat.PoisonDuration > 0 ? player.Stat.PoisonDuration : duration;
        nearest.ApplyPoison(dmg, dur);
    }

    private void StunAndPoisonNearest(PlayerEntity player, List<Enemy> enemies,
        float stunDuration, float poisonMultiplier, float poisonDuration)
    {
        var nearest = GetNearest(player, enemies);
        if (nearest == null) return;
        nearest.ApplyStun(stunDuration);
        float dmg = player.Stat.FinalAtk * poisonMultiplier * player.Stat.PoisonDamageMultiplier;
        float dur = player.Stat.PoisonDuration > 0 ? player.Stat.PoisonDuration : poisonDuration;
        nearest.ApplyPoison(dmg, dur);
    }

    private void ExecuteNearest(PlayerEntity player, List<Enemy> enemies, float thresholdPercent)
    {
        var nearest = GetNearest(player, enemies);
        if (nearest == null) return;
        if (nearest.HpPercent <= thresholdPercent)
            nearest.TakeDamage(player, nearest.Stat.FinalMaxHealth * 9999f);
    }

    private void ForceChaseAllInRange(PlayerEntity player, List<Enemy> enemies, float radius)
    {
        var pos = player.transform.position;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= radius)
            {
                enemy.ForceChase(player);
                EnemyManager.Instance?.RegisterForcedChase(enemy);
            }
        }
    }

    private void ReturnEnemiesFromChase()
    {
        // EnemyManager를 통해 도발 대상 적 PATROL 복귀
        EnemyManager.Instance?.ReturnForcedChaseEnemies();
    }

    private void SpreadPoison(PlayerEntity player, Enemy source, float radius)
    {
        var allEnemies = EnemyManager.Instance?.GetAllEnemies() ?? new List<Enemy>();
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = allEnemies[i];
            if (!enemy.IsAlive || enemy == source) continue;
            if (Vector2.Distance(source.transform.position, enemy.transform.position) <= radius)
            {
                float dmg = player.Stat.FinalAtk * 0.3f * player.Stat.PoisonDamageMultiplier;
                float dur = player.Stat.PoisonDuration > 0 ? player.Stat.PoisonDuration : 3f;
                enemy.ApplyPoison(dmg, dur);
            }
        }
    }

    // ─────────────────────────────────────────────
    // 유틸 — 쿨타임
    // ─────────────────────────────────────────────

    public bool IsOffCooldown(string skillId, float cooldown)
    {
        if (!_cooldownMap.ContainsKey(skillId)) return true;
        return Time.time - _cooldownMap[skillId] >= cooldown;
    }

    public float GetCooldownRatio(string skillId, float cooldown)
    {
        if (!_cooldownMap.ContainsKey(skillId)) return 1f;
        return Mathf.Clamp01((Time.time - _cooldownMap[skillId]) / cooldown);
    }

    private void ReduceCooldown(string skillId, float amount)
    {
        if (_cooldownMap.ContainsKey(skillId))
            _cooldownMap[skillId] -= amount;
    }

    private void ResetCooldown(string skillId)
    {
        if (_cooldownMap.ContainsKey(skillId))
            _cooldownMap[skillId] = 0f;
    }

    private void RegisterBuff(string skillId, float duration)
    {
        _activeBuffMap[skillId] = Time.time + duration;
    }

    // ─────────────────────────────────────────────
    // 유틸 — 탐색
    // ─────────────────────────────────────────────

    private Enemy GetNearest(PlayerEntity player, List<Enemy> enemies)
    {
        Enemy nearest = null;
        float minDist = Mathf.Infinity;
        var pos = player.transform.position;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            float dist = Vector2.Distance(pos, enemy.transform.position);
            if (dist < minDist) { minDist = dist; nearest = enemy; }
        }
        return nearest;
    }

    private bool HasEnemyInRange(PlayerEntity player, List<Enemy> enemies, float range)
    {
        var pos = player.transform.position;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= range) return true;
        }
        return false;
    }

    private bool HasLowHpEnemyInRange(PlayerEntity player, List<Enemy> enemies,
        float range, float hpPercent)
    {
        var pos = player.transform.position;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= range &&
                enemy.HpPercent <= hpPercent) return true;
        }
        return false;
    }

    private bool HasPoisonedEnemyInRange(PlayerEntity player, List<Enemy> enemies, float range)
    {
        var pos = player.transform.position;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            if (Vector2.Distance(pos, enemy.transform.position) <= range &&
                enemy.IsPoison) return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────
    // 유틸 — 코루틴
    // ─────────────────────────────────────────────

    private IEnumerator TemporaryInvincible(PlayerEntity player, float duration)
    {
        player.Stat.AddInvincible();
        yield return new WaitForSeconds(duration);
        player.Stat.RemoveInvincible();
    }

    private IEnumerator BlackHole(PlayerEntity player, List<Enemy> enemies,
        float radius, float multiplier, float duration)
    {
        float endTime = Time.time + duration;
        var pos = player.transform.position;
        while (Time.time < endTime)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;
                if (Vector2.Distance(pos, enemy.transform.position) <= radius)
                {
                    // 플레이어 방향으로 끌어당기기
                    enemy.PullToward(pos, 3f);
                    enemy.TakeDamage(player, player.Stat.FinalAtk * multiplier * Time.deltaTime);
                }
            }
            yield return null;
        }
    }
}