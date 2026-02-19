using System;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private static string _settingFolderPath = "Setting";
    private static string _settingFilePath = "Config.ini";

    private static string _audioSection = "Audio";
    private static string _audioMasterKey = "Master";
    private static string _audioBgmKey = "BGM";
    private static string _audioSfxKey = "SFX";

    private static string _resoulutionSection = "Resoulution";
    private static string _resoulutionKey = "resoulution";
    private static string _fullScreenKey = "fullScreen";

    //private static string _goldSection = "Gold";
    //private static string _goldKey = "savedGold";
    //
    //private static string _shopSection = "Shop";
    //
    //private static string _pickCountKey = "pickCount";
    //
    //private static string _characterSection = "Character";
    //private static string _characterKey = "currentCharacter";
    //private static string _characterShapeKey = "currentShape";
    //
    //private static string _difficultySection = "Difficulty";
    //private static string _difficultyKey = "difficulty";

    public enum volumeKind
    {
        master,
        bgm,
        sfx
    }

    public static void SaveVolume(float volume, volumeKind kind)
    {
        switch (kind)
        {
            case volumeKind.master:
                IniFile.WriteFile(_settingFolderPath, _settingFilePath, _audioSection, _audioMasterKey, volume.ToString());
                break;
            case volumeKind.bgm:
                IniFile.WriteFile(_settingFolderPath, _settingFilePath, _audioSection, _audioBgmKey, volume.ToString());
                break;
            case volumeKind.sfx:
                IniFile.WriteFile(_settingFolderPath, _settingFilePath, _audioSection, _audioSfxKey, volume.ToString());
                break;
            default:
                break;
        }
    }

    public static float LoadVolume(float defaultVolume, volumeKind kind)
    {
        string key = "";
        switch (kind)
        {
            case volumeKind.master:
                key = _audioMasterKey;
                break;
            case volumeKind.bgm:
                key = _audioBgmKey;
                break;
            case volumeKind.sfx:
                key = _audioSfxKey;
                break;
            default:
                break;
        }
        var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _audioSection, key);
        if(float.TryParse(value, out var volume))
        {
            return volume;
        }

        return defaultVolume;
    }

    public static void SaveResolution(int resolutionIndex)
    {
        IniFile.WriteFile(_settingFolderPath, _settingFilePath, _resoulutionSection, _resoulutionKey, resolutionIndex.ToString());
    }

    public static int LoadResolutionIndex(int optimalResolutionIndex)
    {
        var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _resoulutionSection, _resoulutionKey);
        if (int.TryParse(value, out var index))
        {
            return index;
        }

        return optimalResolutionIndex;
    }

    public static void SaveFullScreen(int windowModeIndex)
    {
        IniFile.WriteFile(_settingFolderPath, _settingFilePath, _resoulutionSection, _fullScreenKey, windowModeIndex.ToString());
    }

    public static int LoadFullScreen(int initializedIndex)
    {
        var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _resoulutionSection, _fullScreenKey);
        if (int.TryParse(value, out var index))
        {
            return index;
        }

        return initializedIndex;
    }

    //public static void SaveGold(int currentGold)
    //{
    //    IniFile.WriteFile(_settingFolderPath, _settingFilePath, _goldSection, _goldKey, currentGold.ToString());
    //}
    //
    //public static int LoadGold()
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _goldSection, _goldKey);
    //    if (int.TryParse(value, out var gold))
    //    {
    //        return gold;
    //    }
    //
    //    return 0;
    //}
    //
    //public static void SaveBuy(string key)
    //{
    //    int count = LoadBought(key);
    //    IniFile.WriteFile(_settingFolderPath, _settingFilePath, _shopSection, key, (count + 1).ToString());
    //}
    //
    //public static int LoadBought(string key)
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _shopSection, key);
    //    if (int.TryParse(value, out var gold))
    //    {
    //        return gold;
    //    }
    //
    //    return 0;
    //}
    //
    //public static void SaveCharacter(Characters character)
    //{
    //    IniFile.WriteFile(_settingFolderPath, _settingFilePath, _characterSection, _characterKey, character.ToString());
    //}
    //
    //public static Characters LoadCharacter()
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _characterSection, _characterKey);
    //    if(Enum.TryParse(typeof(Characters), value, out var result))
    //    {
    //        return (Characters)result;
    //    }
    //
    //    return Characters.None;
    //}
    //
    //public static void SaveCharacterShapeIndex(int index)
    //{
    //    IniFile.WriteFile(_settingFolderPath, _settingFilePath, _characterSection, _characterShapeKey, index.ToString());
    //}
    //
    //public static int LoadCharacterShapeIndex()
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _characterSection, _characterShapeKey);
    //    if (int.TryParse(value, out var index))
    //    {
    //        return index;
    //    }
    //    return 0;
    //}
    //
    //public static int LoadPickCount()
    //{
    //    int result = 3;
    //
    //    if (LoadBought(_pickCountKey) > 0)
    //    {
    //        result++;
    //    }
    //
    //    return result;
    //}
    //
    //public static void SaveDifficulty(Difficulty difficulty)
    //{
    //    IniFile.WriteFile(_settingFolderPath, _settingFilePath, _difficultySection, _difficultyKey, difficulty.ToString());
    //}
    //
    //public static Difficulty LoadDifficulty()
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _difficultySection, _difficultyKey);
    //    if (Enum.TryParse(value, out Difficulty index))
    //    {
    //        return index;
    //    }
    //
    //    return Difficulty.Easy;
    //}
    //
    //public static bool CheckDifficulty()
    //{
    //    var value = IniFile.ReadFile(_settingFolderPath, _settingFilePath, _difficultySection, _difficultyKey);
    //    if (Enum.TryParse(value, out Difficulty index))
    //    {
    //        return true;
    //    }
    //
    //    return false;
    //}
}
