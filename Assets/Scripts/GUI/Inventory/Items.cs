using UnityEngine;

public class Items : MonoBehaviour
{
    public static Items Instance { get; private set; }

    public Item[] items;

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

    public bool TryGetItemByID(byte id, out Item result)
    {
        result = default;

        foreach (Item item in items)
        {
            if (item.itemID == id)
            {
                result = item;
                return true;
            }
        }

        return false;
    }
}