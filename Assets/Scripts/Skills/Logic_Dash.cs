using UnityEngine;

[CreateAssetMenu(fileName = "Logic_Dash", menuName = "SkillLogic/Dash")]
public class Logic_Dash : SkillLogicSO
{
    public override bool Execute(PlayerEntity player, ActiveData skillData)
    {
        // 예전 기획 코드에 있었던 player.Dash()를 그대로 호출합니다.
        // DashDistance가 없다면 skillData.Radius 등을 거리값으로 재활용할 수도 있습니다.

        if (player != null)
        {
            // 스킬 데이터를 기반으로 플레이어의 대시 흐름(Flow)을 실행
            player.Dash(skillData.DashDistance);

            // 이펙트가 있다면 시작 위치나 플레이어의 자식 오브젝트로 생성
            if (skillData.EffectPrefab != null)
            {
                PoolManager.SpawnOrInstance(skillData.EffectPrefab, player.transform.position, player.transform.rotation);
            }

            return true;
        }

        return false;
    }
}