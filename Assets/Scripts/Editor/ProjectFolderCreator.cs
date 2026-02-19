#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ProjectFolderCreator
{
    [MenuItem("Tools/Project/Create Default Folders")]
    public static void CreateDefaultFolders()
    {
        string[] paths = new string[]
        {
            "Assets/Scripts",
            "Assets/Scripts/Interface", //IPooledObject
            "Assets/Scripts/Manager", //PoolManager, AudioManager, ResoulutionManager
            "Assets/Scripts/UI",    //SettingUI
            "Assets/Scripts/Core",  //PlayerInput
            "Assets/ScriptableObject",
            "Assets/Prefabs",
            "Assets/Arts",//Images, Models
            "Assets/Audios",
        };

        int created = 0;
        foreach (string path in paths)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                created++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ProjectFolderCreator] Created {created} folders (idempotent).");
    }
}
#endif