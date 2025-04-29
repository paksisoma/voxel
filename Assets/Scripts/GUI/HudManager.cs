using UnityEngine;

public class HudManager : MonoBehaviour
{
    public static HudManager Instance { get; private set; }

    public RectTransform healthTransform;
    public RectTransform armorTransform;
    public RectTransform hungerTransform;
    public RectTransform thirstTransform;
    public RectTransform temperatureTransform;

    private float healthMaxWidth;
    private float armorMaxWidth;
    private float thirstMaxWidth;
    private float hungerMaxWidth;
    private float temperatureMaxHeight;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        healthMaxWidth = healthTransform.rect.width;
        armorMaxWidth = armorTransform.rect.width;
        thirstMaxWidth = thirstTransform.rect.width;
        hungerMaxWidth = hungerTransform.rect.width;
        temperatureMaxHeight = temperatureTransform.rect.height;
    }

    public void SetHealth(float percent)
    {
        float newWidth = healthMaxWidth * (percent - 1);
        healthTransform.sizeDelta = new Vector2(newWidth, healthTransform.sizeDelta.y);
    }

    public void SetArmor(float percent)
    {
        float newWidth = armorMaxWidth * (percent - 1);
        armorTransform.sizeDelta = new Vector2(newWidth, armorTransform.sizeDelta.y);
    }

    public void SetThirst(float percent)
    {
        float newWidth = thirstMaxWidth * (percent - 1);
        thirstTransform.sizeDelta = new Vector2(newWidth, thirstTransform.sizeDelta.y);
    }

    public void SetHunger(float percent)
    {
        float newWidth = hungerMaxWidth * (percent - 1);
        hungerTransform.sizeDelta = new Vector2(newWidth, hungerTransform.sizeDelta.y);
    }

    public void SetTemperature(float percent)
    {
        float newHeight = temperatureMaxHeight * (percent - 1);
        temperatureTransform.sizeDelta = new Vector2(temperatureTransform.sizeDelta.x, newHeight);
    }
}