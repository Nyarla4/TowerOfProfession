using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [SerializeField] private AudioMixer _masterMixer;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    private float _masterVolume = 0.8f;
    private float _bgmVolume = 0.8f;
    private float _sfxVolume = 0.8f;

    [SerializeField] Slider _masterSlider;
    [SerializeField] Slider _bgmSlider;
    [SerializeField] Slider _sfxSlider;

    [SerializeField] GameObject _audioPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    void Start()
    {
        FileRead();
        UpdateSlider();
    }

    public void SetMasterVolume(float linear01)
    {
        float dB = LinearToDecibel(linear01);
        _masterMixer.SetFloat("MasterVolume", dB);
        _masterVolume = linear01;
        SaveSystem.SaveVolume(_masterVolume, SaveSystem.volumeKind.master);
    }

    public void SetBGMVolume(float linear01)
    {
        float dB = LinearToDecibel(linear01);
        _masterMixer.SetFloat("BGMVolume", dB);
        _bgmVolume = linear01;
        SaveSystem.SaveVolume(_bgmVolume, SaveSystem.volumeKind.bgm);
    }

    public void SetSFXVolume(float linear01)
    {
        float dB = LinearToDecibel(linear01);
        _masterMixer.SetFloat("SFXVolume", dB);
        _sfxVolume = linear01;
        SaveSystem.SaveVolume(_sfxVolume, SaveSystem.volumeKind.sfx);
    }

    public void UpdateSlider()
    {
        _masterSlider.value = _masterVolume;
        _bgmSlider.value = _bgmVolume;
        _sfxSlider.value = _sfxVolume;
    }

    public float LinearToDecibel(float linear01)
    {
        if (linear01 <= 0.0001f)
        {
            return -80f;
        }
        return 20f * Mathf.Log10(linear01);
    }

    public void FileRead()
    {
        var master = SaveSystem.LoadVolume(0.8f, SaveSystem.volumeKind.master);
        var bgm = SaveSystem.LoadVolume(0.8f, SaveSystem.volumeKind.bgm);
        var sfx = SaveSystem.LoadVolume(0.8f, SaveSystem.volumeKind.sfx);

        SetMasterVolume(master);
        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
    }

    public void OpenPanel()
    {
        _audioPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        _audioPanel.SetActive(false);
    }
}