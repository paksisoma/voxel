using UnityEngine;

public class HudManager : MonoBehaviour
{
    public static HudManager Instance { get; private set; }

    public RectTransform healthTransform;
    public RectTransform temperatureTransform;
    public RectTransform hungerTransform;
    public RectTransform thirstTransform;

    private float healthMaxHeight;
    private float temperatureMaxHeight;
    private float hungerMaxHeight;
    private float thirstMaxHeight;

    private void Awake()
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

        healthMaxHeight = healthTransform.rect.height;
        temperatureMaxHeight = temperatureTransform.rect.height;
        hungerMaxHeight = hungerTransform.rect.height;
        thirstMaxHeight = thirstTransform.rect.height;
    }

    public void SetHealth(float percent)
    {
        healthTransform.anchoredPosition = new Vector2(0, healthMaxHeight * (percent - 1));
    }

    public void SetTemperature(float percent)
    {
        temperatureTransform.anchoredPosition = new Vector2(0, temperatureMaxHeight * (percent - 1));
    }

    public void SetHunger(float percent)
    {
        hungerTransform.anchoredPosition = new Vector2(0, hungerMaxHeight * (percent - 1));
    }

    public void SetThirst(float percent)
    {
        thirstTransform.anchoredPosition = new Vector2(0, thirstMaxHeight * (percent - 1));
    }
}