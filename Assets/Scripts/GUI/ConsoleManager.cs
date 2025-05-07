using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{
    public GameObject panel;
    public InputField input;

    private float defaultSpeed;
    private float defaultJumpHeight;

    private void Start()
    {
        defaultSpeed = Player.Instance.movement.speed;
        defaultJumpHeight = Player.Instance.movement.jumpHeight;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            panel.SetActive(!panel.activeSelf);

            if (panel.activeSelf)
            {
                input.Select();
                input.ActivateInputField();
                Player.Instance.DisableCameraMouse();
                Player.Instance.DisableActivity();
                Time.timeScale = 0f;
            }
            else
            {
                input.DeactivateInputField();
                input.text = "";
                Player.Instance.EnableCameraMouse();
                Player.Instance.EnableActivity();
                Time.timeScale = 1f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) && panel.activeSelf)
        {
            string[] temp = input.text.Split(' ');

            if (temp.Length < 2)
                return;

            // add [item id]
            if (temp[0] == "add" && byte.TryParse(temp[1], out byte itemID))
                InventoryManager.Instance.AddItem(itemID);

            // speed [value]
            if (temp[0] == "speed" && int.TryParse(temp[1], out int speed))
                Player.Instance.movement.speed = speed;

            // jump [value]
            if (temp[0] == "jump" && int.TryParse(temp[1], out int jump))
                Player.Instance.movement.jumpHeight = jump;

            // speed default
            if (temp[0] == "speed" && temp[1] == "default")
                Player.Instance.movement.speed = defaultSpeed;

            // jump default
            if (temp[0] == "jump" && temp[1] == "default")
                Player.Instance.movement.jumpHeight = defaultJumpHeight;

            // health [value]
            if (temp[0] == "health" && int.TryParse(temp[1], out int health))
                Player.Instance.health = health / 100f;

            // hunger [value]
            if (temp[0] == "hunger" && int.TryParse(temp[1], out int hunger))
                Player.Instance.hunger = hunger / 100f;

            // thirst [value]
            if (temp[0] == "thirst" && int.TryParse(temp[1], out int thirst))
                Player.Instance.thirst = thirst / 100f;

            // temperature [value]
            if (temp[0] == "temperature" && int.TryParse(temp[1], out int temperature))
                Player.Instance.temperature = temperature / 100f;

            // time [value]
            if (temp[0] == "time" && int.TryParse(temp[1], out int time))
                TimeCycle.Instance.SetTime(time);

            // Close console
            panel.SetActive(!panel.activeSelf);
            input.DeactivateInputField();
            input.text = "";
            Player.Instance.EnableCameraMouse();
            Player.Instance.EnableActivity();
            Time.timeScale = 1f;
        }
    }
}