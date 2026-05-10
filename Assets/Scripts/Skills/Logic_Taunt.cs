using UnityEngine;

[CreateAssetMenu(fileName = "Logic_Taunt", menuName = "SkillLogic/Taunt")]
public class Logic_Taunt : SkillLogicSO
{
    public override bool Execute(PlayerEntity player, ActiveData skillData)
    {
        if (player == null) return false;

        var nearbyEnemies = EnemyManager.Instance.GetAllEnemies();
        Vector3 center = player.transform.position;

        foreach (var e in nearbyEnemies)
        {
            if (e != null && e.IsAlive && Vector2.Distance(center, e.transform.position) <= skillData.Radius)
            {
                e.ForceChase(player);
            }
        }

        if (skillData.EffectPrefab != null)
        {
            PoolManager.SpawnOrInstance(skillData.EffectPrefab, center, Quaternion.identity);
        }

        return true;
    }
}
