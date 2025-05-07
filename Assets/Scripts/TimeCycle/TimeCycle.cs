using UnityEngine;
using UnityEngine.UI;

public class TimeCycle : MonoBehaviour
{
    public static TimeCycle Instance { get; private set; }

    public Light sunLight;
    public Material material;
    public float rotationSpeed = 1f;
    public float transitionSpeed = 0.1f;

    //[HideInInspector]
    public float rotation = 0f;
    private float progress = 0f;
    private bool day;

    private float startIntensity, targetIntensity;
    private float startAmbient, targetAmbient;
    private float startBlend, targetBlend;

    [HideInInspector]
    public uint survivedDays = 0;
    public Text survivedText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateSurvivedDays();
    }

    private void Update()
    {
        rotation = (rotation + Time.deltaTime * rotationSpeed) % 360f;
        transform.rotation = Quaternion.Euler(rotation, 90, 0);

        bool isDay = rotation <= 180f;

        if (isDay != day)
            ChangeDay(isDay, false);

        progress += Time.deltaTime * transitionSpeed;

        sunLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
        RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, progress);
        material.SetFloat("_Blend", Mathf.Lerp(startBlend, targetBlend, progress));
    }

    private void ChangeDay(bool value, bool instant)
    {
        day = value;
        progress = instant ? 1f : 0f;

        startIntensity = sunLight.intensity;
        targetIntensity = day ? 1f : 0f;

        startAmbient = RenderSettings.ambientIntensity;
        targetAmbient = day ? 1f : 0.4f;

        startBlend = material.GetFloat("_Blend");
        targetBlend = day ? 0f : 1f;

        if (value && instant == false)
        {
            survivedDays++;
            UpdateSurvivedDays();
            Debug.Log("Most: " + survivedDays);
        }
    }

    public void SetTime(float rotation)
    {
        ChangeDay(rotation <= 180f, true);
        this.rotation = rotation;
    }

    private void UpdateSurvivedDays()
    {
        survivedText.text = "Survived days: " + survivedDays;
    }
}