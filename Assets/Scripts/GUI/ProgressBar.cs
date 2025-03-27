using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    public static ProgressBar Instance { get; private set; }

    public GameObject progressBar;
    public RectTransform progressBackgroundTransform;
    public RectTransform progressTransform;

    private float maxWidth;

    private float timer = 0f;
    public float interval = 3f;

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

        maxWidth = progressBackgroundTransform.rect.width;
    }

    private void Update()
    {
        if (progressBar.activeSelf == false)
            return;

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            progressBar.SetActive(false);
            timer = 0f;
        }
    }

    public void SetProgress(float percent)
    {
        timer = 0f;
        progressBar.SetActive(true);
        float newWidth = maxWidth * (percent - 1);
        progressTransform.sizeDelta = new Vector2(newWidth, progressTransform.sizeDelta.y);
    }

    public void CloseProgress()
    {
        progressBar.SetActive(false);
        timer = 0f;
    }
}