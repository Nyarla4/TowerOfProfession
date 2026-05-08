using UnityEngine;

public abstract class SkillLogicSO : ScriptableObject
{
    public abstract bool Execute(PlayerEntity player, ActiveData skillData);
}
