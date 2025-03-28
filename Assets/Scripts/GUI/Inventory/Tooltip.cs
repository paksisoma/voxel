using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public static Tooltip Instance { get; private set; }

    public GameObject tooltip;
    public Text label;

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

    private void Update()
    {
        tooltip.transform.position = Input.mousePosition;
    }

    public void Show(Item item)
    {
        label.text = item.name;
        tooltip.SetActive(true);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}