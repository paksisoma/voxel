using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public SettingsManager settingsManager;

    public GameObject menu;
    public GameObject buttons;
    public GameObject settings;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menu.activeSelf)
            {
                menu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Player.Instance.EnableCameraMouse();
                Player.Instance.EnableActivity();
                Time.timeScale = 1f;
            }
            else
            {
                Back();
                menu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Player.Instance.DisableCameraMouse();
                Player.Instance.DisableActivity();
                Time.timeScale = 0f;
            }
        }
    }

    public void Resume()
    {
        menu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Player.Instance.EnableCameraMouse();
        Player.Instance.EnableActivity();
        Time.timeScale = 1f;
    }

    public void Settings()
    {
        settingsManager.Load();
        buttons.SetActive(false);
        settings.SetActive(true);
    }

    public void Back()
    {
        buttons.SetActive(true);
        settings.SetActive(false);
    }

    public void StartMenu()
    {
        World.Instance.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start Menu");
    }
}