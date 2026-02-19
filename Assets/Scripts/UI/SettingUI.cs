using UnityEngine;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    public GameObject Panel => _panel;
    [SerializeField] private ResoulutionManager _resPanel;
    [SerializeField] private AudioManager _audPanel;
    //[SerializeField] private GameLevelManager _gamPanel;
    [SerializeField] private PlayerInput _playerInput;

    void Start()
    {
        OpenResolution();
        
        //로비용
        if (_playerInput != null)
        {
            _playerInput.OnPause += OnPause;
        }
        ClosePanel();
    }

    void Update()
    {
        
    }

    private void CloseAllPanel()
    {
        _resPanel.ClosePanel();
        _audPanel.ClosePanel();
        //_gamPanel.ClosePanel();
    }

    public void OpenResolution()
    {
        CloseAllPanel();
        _resPanel.OpenPanel();
    }
    
    public void OpenAudio()
    {
        CloseAllPanel();
        _audPanel.OpenPanel();
    }
    
    //public void OpenGameLevel()
    //{
    //    CloseAllPanel();
    //    _gamPanel.OpenPanel();
    //}

    public void OpenPanel()
    {
        _panel.SetActive(true);
    }

    public void ClosePanel()
    {
        _panel.SetActive(false);
    }

    //로비용
    public void OnPause(bool value)
    {
        if (value && _panel.activeInHierarchy)
        {
            ClosePanel();
        }
    }
}
