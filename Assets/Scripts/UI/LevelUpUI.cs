using System.Collections.Generic;
using UnityEngine;

public class LevelUpUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject LevelUpPanel;       // 어두운 배경과 카드들이 들어있는 전체 창
    public UpgradeCardUI[] Cards;         // 화면에 배치된 3개의 카드 스크립트

    private void Start()
    {
        // 시작할 때는 레벨업 창을 숨겨둠
        if (LevelUpPanel != null) LevelUpPanel.SetActive(false);

        // 매니저의 추첨 완료 이벤트를 구독
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OnLevelUp += ShowLevelUpScreen;
        }
    }

    private void OnDestroy()
    {
        // 씬이 파괴될 때 이벤트 구독 해제 (에러 방지)
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OnLevelUp -= ShowLevelUpScreen;
        }
    }

    private void ShowLevelUpScreen(List<UpgradeDataSO> choices)
    {
        LevelUpPanel.SetActive(true);

        // 뽑힌 카드 개수만큼 UI 세팅 (최대 3개)
        for (int i = 0; i < Cards.Length; i++)
        {
            if (i < choices.Count)
            {
                Cards[i].gameObject.SetActive(true);
                Cards[i].Setup(choices[i], this); // 데이터 주입
            }
            else
            {
                // 풀에 카드가 부족해서 2장만 뽑혔다면 남은 칸은 끔
                Cards[i].gameObject.SetActive(false);
            }
        }
    }

    // 카드가 클릭되었을 때 UpgradeCardUI가 이 함수를 찔러줌
    public void OnUpgradeSelected(UpgradeDataSO selectedUpgrade)
    {
        // 1. 매니저에게 유저가 고른 카드 전달 (스탯 적용 및 시간 재개)
        LevelUpManager.Instance.SelectUpgrade(selectedUpgrade);

        // 2. 레벨업 창 다시 닫기
        LevelUpPanel.SetActive(false);
    }
}