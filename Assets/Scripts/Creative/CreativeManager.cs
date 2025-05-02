using UnityEngine;
using UnityEngine.UI;

public class CreativeManager : MonoBehaviour
{
    public GameObject panel;
    public Movement movement;

    public InputField speedInput;
    public InputField jumpHeightInput;
    public InputField renderDistanceInput;

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

        // Render distance
        int renderDistance;
        isNumeric = int.TryParse(renderDistanceInput.text, out renderDistance);

        if (isNumeric)
            World.Instance.renderDistance = renderDistance;

        // Clear
        ClearInputs();
    }

    public void Default()
    {
        movement.speed = defaultSpeed;
        movement.jumpHeight = defaultJumpHeight;
        World.Instance.renderDistance = Constants.DEFAULT_RENDER_DISTANCE;

        ClearInputs();
    }

    private void ClearInputs()
    {
        speedInput.text = "";
        jumpHeightInput.text = "";
        renderDistanceInput.text = "";
    }
}