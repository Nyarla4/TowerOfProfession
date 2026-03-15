using UnityEngine;

public enum PassiveType { PERMANENT, CONDITION, EVENT }
public enum EventType { NONE, ON_KILL, ON_ATTACK, ON_SKILL_USE }
public enum AttackType { MELEE_SINGLE, MELEE_AREA, RANGED_SINGLE, RANGED_AREA }

[System.Serializable]
public class PassiveData
{
    public string SkillID;
    public string DisplayName;
    public PassiveType Type;
    public EventType EventType;
}

[System.Serializable]
public class ActiveData
{
    public string SkillID;
    public string DisplayName;
    public Sprite Icon;
    public float Cooldown;
    public float Duration;
    public float Radius;
    public float DashDistance;
    public float Multiplier;
    public float ThresholdPercent;
    public float SelfHpCostPercent;
    public GameObject EffectPrefab;
}