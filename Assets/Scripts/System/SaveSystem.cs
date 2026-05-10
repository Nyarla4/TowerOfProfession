using System.IO;
using UnityEngine;

/// <summary>
/// 설정값 → PlayerPrefs
/// 플레이 데이터 → JsonUtility + persistentDataPath
/// </summary>
public static class SaveSystem
{
    // ─────────────────────────────────────────────
    // 1. 설정값 (PlayerPrefs)
    // ─────────────────────────────────────────────

    public enum volumeKind { master, bgm, sfx }

    public static void SaveVolume(float volume, volumeKind kind)
    {
        PlayerPrefs.SetFloat("Volume_" + kind.ToString(), volume);
        // Save()는 AudioManager의 슬라이더 OnPointerUp에서 한 번만 호출
    }

    public static float LoadVolume(float defaultVolume, volumeKind kind)
    {
        return PlayerPrefs.GetFloat("Volume_" + kind.ToString(), defaultVolume);
    }

    public static void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────
    // 2. 플레이 데이터 (Json + persistentDataPath)
    // ─────────────────────────────────────────────

    private static readonly string _saveFileName = "savedata.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

    public static void SavePlayData(PlayData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem_SavePlayData: 저장 실패 — {e.Message}");
        }
    }

    public static PlayData LoadPlayData()
    {
        try
        {
            if (!File.Exists(SavePath))
                return new PlayData();

            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<PlayData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem_LoadPlayData: 로드 실패 — {e.Message}");
            return new PlayData();
        }
    }

    public static void DeletePlayData()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static bool HasPlayData()
    {
        return File.Exists(SavePath);
    }
}

/// <summary>
/// 플레이 데이터 구조체
/// HTML의 localStorage 저장 데이터에 대응
/// </summary>
[System.Serializable]
public class PlayData
{
    // 기본 정보
    public int level = 1;
    public float exp = 0f;
    public JobID jobId = JobID.APPRENTICE;
    public int statPoints = 0;

    // 전투
    public float hp;

    // 위치 (마지막 스폰 포인트)
    public string lastSpawnPointName = "";
    public float worldX = 0f;
    public float worldY = 0f;

    // 투자된 스탯 포인트
    public int AllocatedStr = 0;
    public int AllocatedDex = 0;
    public int AllocatedInt = 0;
    }