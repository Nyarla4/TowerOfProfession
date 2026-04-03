using UnityEngine;

[CreateAssetMenu(fileName = "NewJobData", menuName = "ScriptableObjects/JobData")]
public class JobDataSO : ScriptableObject
{
    [Header("Identity")]
    public JobID JobID;
    public string DisplayName;
    public AttackType AttackType;
    public Color JobColor;

    [Header("Attack")]
    public GameObject ProjectilePrefab;

    [Header("Stat Bonus")]
    public float BonusMaxHp;
    public float BonusAtk;
    public float BonusDef;
    public float BonusMoveSpeed;
    public float BonusRegen;
    public float BonusRange;

    [Header("Skills")]
    public PassiveData[] Passives;
    public ActiveData[] Actives;
}