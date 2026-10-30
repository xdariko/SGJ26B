using UnityEngine;
using UnityEngine.SceneManagement;

// Имена сцен и переход между ними. Сцены должны быть в Build Profiles в этом порядке: Splash, Menu, Game.
public static class Scenes
{
    public const string Splash = "Splash";
    public const string Menu = "Menu";
    public const string Game = "Game";

    public static void Load(string scene)
    {
        // G статический и переживает смену сцены: снимаем паузу, иначе новая сцена стартует замороженной
        G.IsPaused = false;
        G.IsMenuOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }
}
