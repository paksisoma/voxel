using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void StartMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start Menu");
    }

    public void WorldMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("World Menu");
    }

    public void NewWorld()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("New World");
    }

    public void Settings()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Settings");
    }

    public void Quit()
    {
        Application.Quit();
    }
}