using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class IniFile : MonoBehaviour
{
    #region ini Import
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern long WritePrivateProfileString(
        string Section, string Key, string Value, string FilePath);
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern int GetPrivateProfileString(
        string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
    #endregion

    #region saveLoadFunction
    /// <summary>
    /// ini 파일 쓰기
    /// </summary>
    /// <param name="filePath">폴더경로 및 파일(확장자 포함)</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 key</param>
    /// <param name="value">저장 값</param>
    private static void WriteIni(string filePath, string section, string key, string value)
    {
        WritePrivateProfileString(section, key, value, filePath);
    }

    /// <summary>
    /// ini 파일 읽기
    /// </summary>
    /// <param name="filePath">폴더경로 및 파일(확장자 포함)</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 key</param>
    /// <returns></returns>
    private static string ReadIni(string filePath, string section, string key)
    {
        var value = new StringBuilder(255);
        GetPrivateProfileString(section, key, "Error", value, 255, filePath);
        return value.ToString();
    }
    #endregion

    #region outerFunction
    /// <summary>
    /// ini 쓰기
    /// </summary>
    /// <param name="_folderPath">"Config" 형식, 폴더명</param>
    /// <param name="filePath">"PlayerStat.ini" 형식, 파일명</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 키</param>
    /// <param name="value">저장 값</param>
    public static void WriteFile(string _folderPath, string filePath, string section, string key, string value)
    {
        var accesser = "\\";
#if UNITY_EDITOR
        accesser = accesser.Replace("\\", "/");
#endif

        string folderPath = Application.persistentDataPath + accesser + _folderPath;

        if (!Directory.Exists(folderPath))//폴더 확인
        {
            Directory.CreateDirectory(folderPath);
        }

        var _filePath = accesser + filePath;
        var iniPath = folderPath + _filePath;

        WriteIni(iniPath, section, key, value);
    }

    /// <summary>
    /// ini 읽기, 공백: 파일없음
    /// </summary>
    /// <param name="_folderPath">"Config" 형식, 폴더명</param>
    /// <param name="filePath">"PlayerStat.ini" 형식, 파일명</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 키</param>
    public static string ReadFile(string _folderPath, string filePath, string section, string key)
    {
        var accesser = "\\";
#if UNITY_EDITOR
        accesser = accesser.Replace("\\", "/");
#endif

        string folderPath = Application.persistentDataPath + accesser + _folderPath;

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var iniPath = folderPath + accesser + filePath;

        if (!File.Exists(iniPath))
        {
            var file = File.CreateText(iniPath);
            file.Close();
            return "";
        }

        return ReadIni(iniPath, section, key);
    }
    #endregion
}
