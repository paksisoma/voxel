using UnityEngine;

public class ContainerManager : MonoBehaviour
{
    public static ContainerManager Instance { get; private set; }

    public GameObject panel;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown("i"))
        {
            panel.SetActive(!panel.activeSelf);

            if (panel.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Player.Instance.DisableCameraMouse();
                Player.Instance.DisableActivity();
                CraftManager.Instance.UpdateRows();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Player.Instance.EnableCameraMouse();
                Player.Instance.EnableActivity();
                Tooltip.Instance.Hide();
            }
        }
    }
}