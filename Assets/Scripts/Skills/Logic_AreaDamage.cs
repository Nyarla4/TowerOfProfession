using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필요할 수 있음

[CreateAssetMenu(fileName = "Logic_AreaDamage", menuName = "SkillLogic/AreaDamage")]
public class Logic_AreaDamage : SkillLogicSO
{
    public override bool Execute(PlayerEntity player, ActiveData skillData)
    {
        var enemies = EnemyManager.Instance.GetAllEnemies();
        Vector3 center = player.transform.position;

        // [수정된 부분] foreach 대신 뒤에서부터 도는 역순 for문 사용
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];

            if (e != null && e.IsAlive)
            {
                if (Vector2.Distance(center, e.transform.position) <= skillData.Radius)
                {
                    e.TakeDamage(player, player.Stat.FinalAtk * skillData.Multiplier);
                }
            }
        }

        if (skillData.EffectPrefab != null)
        {
            PoolManager.SpawnOrInstance(skillData.EffectPrefab, center, Quaternion.identity);
        }

        return true;
    }
}