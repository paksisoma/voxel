using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class GameOver : MonoBehaviour
{
    public static GameOver Instance { get; private set; }

    public GameObject gameOverMenu;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Show()
    {
        Cursor.lockState = CursorLockMode.None;
        Player.Instance.DisableCameraMouse();
        Player.Instance.DisableActivity();
        Time.timeScale = 0f;

        gameOverMenu.SetActive(true);
    }

    public void Respawn()
    {
        List<InventoryItem> items = InventoryManager.Instance.GetItems();

        foreach (InventoryItem item in items)
        {
            item.quantity = 0;
            item.UpdateQuantity();
        }

        Player.Instance.WarpPlayer(Vector3.zero);

        Player.Instance.hunger = INIT_HUNGER;
        Player.Instance.thirst = INIT_THIRST;
        Player.Instance.temperature = INIT_TEMPERATURE;
        Player.Instance.health = INIT_HEALTH;

        Cursor.lockState = CursorLockMode.Locked;
        Player.Instance.EnableCameraMouse();
        Player.Instance.EnableActivity();
        Time.timeScale = 1f;

        gameOverMenu.SetActive(false);
    }
}