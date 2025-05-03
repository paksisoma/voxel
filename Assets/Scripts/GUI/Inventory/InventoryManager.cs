using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public GameObject inventory;
    public InventorySlot[] slots;
    public EquipmentSlot armorSlot;
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

    [HideInInspector]
    public Armor activeArmor;

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

    public void AddItem(byte id, int index, int quantity)
    {
        if (Items.Instance.items.TryGetValue(id, out Item item))
        {
            InventorySlot slot = slots[index];
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem == null)
                SpawnItem(item, slot, quantity);
        }
    }

    public void AddArmor(byte id)
    {
        InventoryItem inventoryItem = armorSlot.GetComponentInChildren<InventoryItem>();

        if (inventoryItem == null && Items.Instance.items.TryGetValue(id, out Item item))
            SpawnArmor(item);
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

    public List<InventoryItem> GetItems()
    {
        List<InventoryItem> result = new List<InventoryItem>();

        foreach (InventorySlot slot in slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null)
                result.Add(inventoryItem);
        }

        return result;
    }

    public List<StorageItem> GetStorageItems()
    {
        List<StorageItem> result = new List<StorageItem>();

        byte index = 0;

        foreach (InventorySlot slot in slots)
        {
            InventoryItem inventoryItem = slot.GetComponentInChildren<InventoryItem>();

            if (inventoryItem != null)
                result.Add(new StorageItem(index, inventoryItem.item.itemID, (byte)inventoryItem.quantity));

            index++;
        }

        return result;
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

    private void SpawnItem(Item item, InventorySlot slot, int quantity)
    {
        GameObject newItem = Instantiate(itemPrefab, slot.transform);
        InventoryItem inventoryItem = newItem.GetComponent<InventoryItem>();
        inventoryItem.Init(item);
        inventoryItem.quantity = quantity;
        inventoryItem.UpdateQuantity();
    }

    private void SpawnArmor(Item item)
    {
        GameObject newItem = Instantiate(itemPrefab, armorSlot.transform);
        InventoryItem inventoryItem = newItem.GetComponent<InventoryItem>();
        inventoryItem.Init(item);
        activeArmor = (Armor)inventoryItem.item;
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