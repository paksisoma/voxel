using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject menu;

    private void Awake()
    {
        Time.timeScale = 1f;   
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menu.SetActive(!menu.activeSelf);

            if (menu.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Player.Instance.DisableCameraMouse();
                Player.Instance.DisableActivity();
                Time.timeScale = 0f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Player.Instance.EnableCameraMouse();
                Player.Instance.EnableActivity();
                Time.timeScale = 1f;
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

    }

    public void StartMenu()
    {
        World.Instance.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start Menu");
    }
}