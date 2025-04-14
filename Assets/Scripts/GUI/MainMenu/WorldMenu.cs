using System.IO;
using UnityEngine;

public class WorldMenu : MonoBehaviour
{
    public Transform worldSelector;
    public GameObject worldButtonPrefab;

    private void Awake()
    {
        string path = Application.dataPath + "/Save/";

        if (Directory.Exists(path))
        {
            string[] directories = Directory.GetDirectories(path);

            foreach (string directory in directories)
            {
                string worldName = Path.GetFileName(directory);

                GameObject worldButton = Instantiate(worldButtonPrefab, worldSelector);
                worldButton.GetComponent<WorldButton>().Init(worldName);
            }
        }
    }
}