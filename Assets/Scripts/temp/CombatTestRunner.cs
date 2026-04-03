using UnityEngine;

public class CombatTestRunner : MonoBehaviour
{
    public PlayerEntity Player;
    public JobDataSO TestRangedJob; // 인스펙터에서 '궁수'나 '마법사' SO를 할당하세요.

    private void Start()
    {
        // 2-3. 타격 이벤트 검증
        Player.OnAfterAttackHit += (enemy, dmg) =>
            Debug.Log($"[Hit] 플레이어가 {enemy.name}에게 {dmg} 데미지를 입힘!");

        // 5. 투사체 생성 이벤트 검증
        Player.OnProjectileSpawned += (proj) =>
            Debug.Log($"[Projectile] 투사체 발사됨!");

        // 3-3. 적 사망 이벤트 검증
        EnemyManager.Instance.OnEnemyDiedGlobal += (enemy, group) =>
            Debug.Log($"[Kill] {enemy.name} 처치됨! (경험치 {group.ExpReward} 획득 예정)");
    }

    private void Update()
    {
        // 4-1 & 4-2. 체력 및 자연 회복 검증 (H 키)
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log($"[HP Check] 현재 체력: {Player.Stat.CurrentHealth} / {Player.Stat.FinalMaxHealth}");
        }

        // 5. 강제 전직 및 투사체 테스트 (Space 키)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"[Job Change] {TestRangedJob.name} 직업으로 강제 전직!");
            Player.ChangeJob(TestRangedJob);
        }
    }
}