using UnityEngine;
using UnityEngine.UI;

public class WorldButton : MonoBehaviour
{
    public string worldName;
    public Text label;
    public Button button;

    public void Init(string worldName)
    {
        this.worldName = worldName;
        label.text = worldName;
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Storage.LoadMap(worldName);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}