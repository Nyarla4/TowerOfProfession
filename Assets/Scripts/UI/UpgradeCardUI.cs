using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image IconImage;
    public TMP_Text NameText;
    public TMP_Text DescText;
    public Button SelectButton;

    private UpgradeDataSO _currentData;
    private LevelUpUI _parentUI;

    public void Setup(UpgradeDataSO data, LevelUpUI parentUI)
    {
        _currentData = data;
        _parentUI = parentUI;

        // 구조(Data)의 텍스트와 이미지를 UI(흐름)에 반영
        if (IconImage != null) IconImage.sprite = data.Icon;
        if (NameText != null) NameText.text = data.UpgradeName;
        if (DescText != null) DescText.text = data.Description;

        // 버튼 클릭 이벤트 리스너 연결 (기존 연결 제거 후 새로 달기)
        SelectButton.onClick.RemoveAllListeners();
        SelectButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_currentData != null && _parentUI != null)
        {
            _parentUI.OnUpgradeSelected(_currentData);
        }
    }
}