using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public Text label;
    public Text shadowLabel;

    [HideInInspector]
    public byte currentTask;

    readonly private string[] tasks = {
        "Hold W to move forward.",
        "Hold S to move backward.",
        "Hold A to move left.",
        "Hold D to move right.",
        "Press SPACE to jump.",
        "Hold left mouse to punch.",
        "Collect rocks and sticks using the left mouse button.",
        "Press I to open your inventory and craft a campfire.",
        "Place the campfire by clicking the left mouse button.",
        "Select a stick in your inventory with the left mouse button, then click the campfire to light it. Staying near the fire will warm you up and increase your body temperature.",
        "Craft an axe.",
        "Select the axe in your inventory with a left-click, then hold down the left mouse button to chop down a tree.",
        "Kill an animal using the axe.",
        "Left-click the meat in your inventory to eat it. This will reduce your hunger.",
        "Go to a lake and drink from it to reduce your thirst.",
        "Dig a block using the shovel. You can use the shovel to dig dirt, sand, and snow.",
        "Select the block in your inventory and place it somewhere with a left-click.",
        "Head north to find valuable ores on mountain tops. Mine them with a pickaxe, but beware, it gets colder.",
        "Craft a suit of armor. It will grant you extra health and provide warmth.",
    };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (currentTask == byte.MaxValue)
            SetText("");
        else
            SetText(tasks[currentTask]);
    }

    private void SetText(string text)
    {
        label.text = text;
        shadowLabel.text = text;
    }

    public void NextTask(byte task)
    {
        if (task - 1 == currentTask)
        {
            if (task < tasks.Length)
            {
                currentTask = task;
                SetText(tasks[currentTask]);
            }
            else
            {
                currentTask = byte.MaxValue;
                SetText("");
            }
        }
    }
}