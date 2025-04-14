using UnityEngine;

public class NewWorld : MonoBehaviour
{
    public UnityEngine.UI.InputField nameInputField;
    public UnityEngine.UI.InputField seedInputField;

    public void CreateNewWorld()
    {
        string seedText = seedInputField.text.Trim();
        string mapName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(seedText) || string.IsNullOrEmpty(mapName))
            return;

        if (uint.TryParse(seedText, out uint seed))
        {
            Storage.CreateMap(mapName, seed);
            UnityEngine.SceneManagement.SceneManager.LoadScene("World Menu");
        }
    }

    public void GenerateSeed()
    {
        seedInputField.text = Random.Range(uint.MinValue, uint.MaxValue).ToString();
    }
}