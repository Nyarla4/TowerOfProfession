using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResoulutionManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _screenResolution;
    private List<Resolution> _resolutions = new()
    {
        new Resolution { width = 1280, height = 720 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1920, height = 1080 },
        new Resolution { width = 2048, height = 1152 },
        new Resolution { width = 2560, height = 1440 },
        new Resolution { width = 2880, height = 1620 },
        new Resolution { width = 3840, height = 2160 },
    };

    private int _optimalResolutionIndex = 0;
    private int _currentResolutionIndex = -1;
    private int _currentWindowIndex = -1;

    [SerializeField] private GameObject _resolutionPanel;
    
    [SerializeField] private TMP_Dropdown _screenWindow;

    private List<FullScreenMode> _fullScreenModes = new()
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.MaximizedWindow,
        FullScreenMode.Windowed,
    };

    void Start()
    {
        _screenResolution.ClearOptions();
        List<string> options = new ();
        for (int i = 0; i < _resolutions.Count; i++)
        {
            string option = _resolutions[i].width + " x " + _resolutions[i].height;
            // 가장 적합한 해상도에 별표를 표기합니다.
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                _optimalResolutionIndex = i;
                option += " *";
            }
            options.Add(option);
        }
        _screenResolution.AddOptions(options);

        _currentResolutionIndex = SaveSystem.LoadResolutionIndex(_optimalResolutionIndex);

        _screenResolution.value = _currentResolutionIndex;
        _screenResolution.RefreshShownValue();
        SetResolution(_currentResolutionIndex);


        _screenWindow.ClearOptions();
        options = new();
        for (int i = 0; i < _fullScreenModes.Count; i++)
        {
            string option = "";
            switch (_fullScreenModes[i])
            {
                case FullScreenMode.ExclusiveFullScreen:
                    option = "전체화면";
                    break;
                case FullScreenMode.FullScreenWindow:
                    option = "테두리 없는 전체화면";
                    break;
                case FullScreenMode.MaximizedWindow:
                    option = "최대화된 창";
                    break;
                case FullScreenMode.Windowed:
                    option = "창 모드";
                    break;
                default:
                    break;
            }
            options.Add(option);
        }
        _screenWindow.AddOptions(options);

        _currentWindowIndex = SaveSystem.LoadFullScreen((int)Screen.fullScreenMode);

        _screenWindow.value = _currentWindowIndex;
        _screenWindow.RefreshShownValue();
        SetWindow(_currentWindowIndex);
    }

    public void SetWindow(int windowIndex)
    {
        _currentWindowIndex = windowIndex;
        FullScreenMode fullScreenMode = _fullScreenModes[windowIndex];
        Screen.fullScreenMode = fullScreenMode;
        SaveSystem.SaveFullScreen(_currentWindowIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        _currentResolutionIndex = resolutionIndex;
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        SaveSystem.SaveResolution(_currentResolutionIndex);
    }

    public void OpenPanel()
    {
        _resolutionPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        _resolutionPanel.SetActive(false);
    }
}