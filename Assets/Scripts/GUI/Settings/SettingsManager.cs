using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Slider fpsSlider;
    public Text fpsLabel;

    private List<Resolution> availableResolutions;
    public Dropdown resolutionDropdown;

    public Slider renderDistanceSlider;
    public Text renderDistanceLabel;


    public Dropdown vsyncDropdown;

    public Dropdown displayModeDropdown;

    private void Start()
    {
        Load();
    }

    public void Load()
    {
        // FPS limit
        fpsSlider.value = Application.targetFrameRate;

        // Resolution
        availableResolutions = Screen.resolutions.Reverse().DistinctBy(r => new { r.width, r.height }).ToList();
        List<string> options = availableResolutions.Select(r => $"{r.width}x{r.height}").ToList();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = availableResolutions.FindIndex(a => a.width == Screen.width && a.height == Screen.height);

        // Render distance
        renderDistanceSlider.value = PlayerPrefs.GetInt("RenderDistance");

        // V-Sync
        vsyncDropdown.value = QualitySettings.vSyncCount == 1 ? 0 : 1;

        // Display mode
        displayModeDropdown.value = Screen.fullScreen ? 0 : 1;
    }

    public void FPSChange()
    {
        if (fpsSlider.value == -1)
            fpsLabel.text = "INF";
        else
            fpsLabel.text = fpsSlider.value.ToString();
    }

    public void RenderDistanceChange()
    {
        renderDistanceLabel.text = renderDistanceSlider.value.ToString();
    }

    public void ApplyChanges()
    {
        // FPS limit
        int fpsLimit = Convert.ToInt32(fpsSlider.value);
        Application.targetFrameRate = fpsLimit;
        PlayerPrefs.SetInt("FPSLimit", fpsLimit);

        // Resolution
        Resolution selectedResolution = availableResolutions[resolutionDropdown.value];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionWidth", selectedResolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", selectedResolution.height);

        // Render distance
        int renderDistance = Convert.ToInt32(renderDistanceSlider.value);
        Settings.Instance.renderDistance = renderDistance;
        PlayerPrefs.SetInt("RenderDistance", renderDistance);

        // V-Sync
        int vSync = vsyncDropdown.options[vsyncDropdown.value].text == "On" ? 1 : 0;
        QualitySettings.vSyncCount = vSync;
        PlayerPrefs.SetInt("V-Sync", vSync);

        // Display Mode
        bool displayMode = displayModeDropdown.options[displayModeDropdown.value].text == "Fullscreen";
        Screen.fullScreen = displayMode;
        PlayerPrefs.SetInt("DisplayMode", displayMode ? 1 : 0);
    }
}