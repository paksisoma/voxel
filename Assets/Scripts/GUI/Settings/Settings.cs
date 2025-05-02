using System;
using UnityEngine;
using static Constants;

public class Settings : MonoBehaviour
{
    public static Settings Instance { get; private set; }

    public int renderDistance;

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

        // FPS limit
        if (!PlayerPrefs.HasKey("FPSLimit"))
            PlayerPrefs.SetInt("FPSLimit", DEFAULT_FPS_LIMIT);

        // Resolution
        if (!PlayerPrefs.HasKey("ResolutionWidth"))
            PlayerPrefs.SetInt("ResolutionWidth", Screen.currentResolution.width);

        if (!PlayerPrefs.HasKey("ResolutionHeight"))
            PlayerPrefs.SetInt("ResolutionHeight", Screen.currentResolution.height);

        // Render distance
        if (!PlayerPrefs.HasKey("RenderDistance"))
            PlayerPrefs.SetInt("RenderDistance", DEFAULT_RENDER_DISTANCE);

        // V-Sync
        if (!PlayerPrefs.HasKey("V-Sync"))
            PlayerPrefs.SetInt("V-Sync", DEFAULT_VSYNC);

        // Display mdoe
        if (!PlayerPrefs.HasKey("DisplayMode"))
            PlayerPrefs.SetInt("DisplayMode", DEFAULT_DISPLAY_MODE);

        // Apply
        Application.targetFrameRate = PlayerPrefs.GetInt("FPSLimit");
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth"), PlayerPrefs.GetInt("ResolutionHeight"), Convert.ToBoolean(PlayerPrefs.GetInt("DisplayMode")));
        renderDistance = PlayerPrefs.GetInt("RenderDistance");
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("V-Sync");
    }
}