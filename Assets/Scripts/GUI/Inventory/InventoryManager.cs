using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public GameObject inventory;
    public InventorySlot[] slots;
    public GameObject itemPrefab;

    private InventoryItem _activeItem;
    public InventoryItem activeItem
    {
        get => _activeItem;
        set
        {
            _activeItem = value;
            OnActiveItemChanged();
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddItem(Item item)
    {
        bool found = false;

        // Stackable
        if (item.stackable)
        {
            foreach (InventorySlot slot in slots)
            {
                InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

                if (inventoryItem != null && inventoryItem.item.itemID == item.itemID)
                {
                    inventoryItem.quantity++;
                    inventoryItem.UpdateQuantity();
                    found = true;
                    break;
                }
            }
        }

        // Not stackable or not found stackable
        if (!found)
        {
            foreach (InventorySlot slot in slots)
            {
                InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

                if (inventoryItem == null)
                {
                    SpawnItem(item, slot);
                    break;
                }
            }
        }
    }

    public void AddItem(byte id)
    {
        if (Items.Instance.items.TryGetValue(id, out Item item))
            AddItem(item);
    }

    public InventoryItem GetItem(Item item)
    {
        foreach (InventorySlot slot in slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null && inventoryItem.item.itemID == item.itemID)
                return inventoryItem;
        }

        return null;
    }

    public int CountItemsQuantity(Item item)
    {
        int quantity = 0;

        foreach (InventorySlot slot in slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null && inventoryItem.item.itemID == item.itemID)
                quantity += inventoryItem.quantity;
        }

        return quantity;
    }

    public int CountEmptySlots()
    {
        int count = 0;

        foreach (InventorySlot slot in slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem == null)
                count++;
        }

        return count;
    }

    private void SpawnItem(Item item, InventorySlot slot)
    {
        GameObject newItem = Instantiate(itemPrefab, slot.transform);
        InventoryItem inventoryItem = newItem.GetComponent<InventoryItem>();
        inventoryItem.Init(item);
    }

    private void OnActiveItemChanged()
    {
        Transform rightHand = Player.Instance.rightHandParent.transform;

        if (activeItem == null)
        {
            // Remove all children
            foreach (Transform child in rightHand)
                Destroy(child.gameObject);
        }
        else
        {
            Item item = activeItem.item;

            if (item is Tool tool)
            {
                GameObject a = Instantiate(tool.model);

                a.transform.SetParent(rightHand);
                a.transform.localPosition = Vector3.zero;
                a.transform.localRotation = Quaternion.identity;
            }
        }
    }
}