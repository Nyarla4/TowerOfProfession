using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public enum Scenes
    {
        Title,
        Lobby,
        Room,
    }

    public static void ToScene(Scenes scenes)
    {
        string sceneName = scenes.ToString();
        var sceneIdx = SceneUtility.GetBuildIndexByScenePath(sceneName);
        
        if (sceneIdx < 0)
        {
            Debug.Log("해당 Scene 없음");
            return;
        }

        SceneManager.LoadScene(sceneIdx);
    }

    public static void ToRoom()
    {
        ToScene(Scenes.Room);
    }

    public static void ToLobby()
    {
        ToScene(Scenes.Lobby);
    }

    public static void ToTitle()
    {
        ToScene(Scenes.Title);
    }

    public static void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
