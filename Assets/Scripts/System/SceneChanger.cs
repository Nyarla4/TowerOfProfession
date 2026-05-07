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
        SceneManager.LoadScene(sceneName);
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
