using System.Collections.Generic;
using UnityEngine;

public class CraftManager : MonoBehaviour
{
    public static CraftManager Instance { get; private set; }

    public GameObject content;
    public GameObject rowPrefab;

    public List<Craftable> craftables = new List<Craftable>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UpdateRows();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateRows()
    {
        // Count inventory items
        Dictionary<int, int> items = new Dictionary<int, int>();

        foreach (InventorySlot slot in InventoryManager.Instance.slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null)
            {
                int itemID = inventoryItem.item.itemID;

                if (items.ContainsKey(itemID))
                    items[itemID] += inventoryItem.quantity;
                else
                    items.Add(itemID, inventoryItem.quantity);
            }
        }

        // Destroy rows
        foreach (Transform child in content.transform)
            Destroy(child.gameObject);

        // Create rows
        foreach (Craftable craftable in craftables)
        {
            GameObject objectRow = Instantiate(rowPrefab, content.transform);
            CraftRow craftRow = objectRow.GetComponent<CraftRow>();

            bool disabled = false;

            foreach (Ingredient ingredient in craftable.craftInput)
            {
                if (items.TryGetValue(ingredient.item.itemID, out int itemCount))
                {
                    if (itemCount < ingredient.quantity)
                    {
                        disabled = true;
                        break;
                    }
                }
                else
                {
                    disabled = true;
                }
            }

            craftRow.foreground.SetActive(disabled);
            craftRow.craftable = craftable;
            craftRow.Init();
        }
    }
}