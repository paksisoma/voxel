using UnityEngine;
using UnityEngine.UI;

public class CreativeManager : MonoBehaviour
{
    public GameObject panel;
    public Movement movement;

    public InputField speedInput;
    public InputField jumpHeightInput;

    private float defaultSpeed;
    private float defaultJumpHeight;

    private void Awake()
    {
        defaultSpeed = movement.speed;
        defaultJumpHeight = movement.jumpHeight;
    }

    private void Update()
    {
        if (Input.GetKeyDown("p"))
        {
            panel.SetActive(!panel.activeSelf);

            if (panel.activeSelf)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Submit()
    {
        bool isNumeric;

        // Speed
        int speed;
        isNumeric = int.TryParse(speedInput.text, out speed);

        if (isNumeric)
            movement.speed = speed;

        // Gravity
        int jumpHeight;
        isNumeric = int.TryParse(jumpHeightInput.text, out jumpHeight);

        if (isNumeric)
            movement.jumpHeight = jumpHeight;

        // Clear
        ClearInputs();
    }

    public void Default()
    {
        movement.speed = defaultSpeed;
        movement.jumpHeight = defaultJumpHeight;

        ClearInputs();
    }

    private void ClearInputs()
    {
        speedInput.text = "";
        jumpHeightInput.text = "";
    }
}