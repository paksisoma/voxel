using UnityEngine;
using UnityEngine.EventSystems;

public class CraftRow : MonoBehaviour, IPointerClickHandler
{
    public GameObject itemPrefab;
    public GameObject left;
    public GameObject right;
    public GameObject foreground;

    public Craftable craftable;

    public void Init()
    {
        GameObject item = Instantiate(itemPrefab, right.transform);
        item.GetComponent<CraftItem>().Init(craftable.craftOutput);

        foreach (Ingredient ingredient in craftable.craftInput)
        {
            GameObject ingredientItem = Instantiate(itemPrefab, left.transform);
            ingredientItem.GetComponent<CraftItem>().Init(ingredient);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check empty slots
        int emptySlots = InventoryManager.Instance.CountEmptySlots();
        if (emptySlots <= 0) return;

        // Check if ingredients are available
        foreach (Ingredient ingredient in craftable.craftInput)
        {
            if (ingredient.quantity > InventoryManager.Instance.CountItemsQuantity(ingredient.item))
                return;
        }

        // Remove ingredients from inventory
        foreach (Ingredient ingredient in craftable.craftInput)
        {
            int remainingQuantity = ingredient.quantity;

            while (remainingQuantity > 0)
            {
                InventoryItem item = InventoryManager.Instance.GetItem(ingredient.item);

                if (remainingQuantity >= item.quantity)
                {
                    remainingQuantity -= item.quantity;
                    item.quantity = 0;
                }
                else
                {
                    item.quantity -= remainingQuantity;
                    remainingQuantity = 0;
                }

                item.UpdateQuantity();
            }
        }

        // Add item
        for (int i = 0; i < craftable.craftOutput.quantity; i++)
            InventoryManager.Instance.AddItem(craftable.craftOutput.item);

        CraftManager.Instance.UpdateRows(); // Update craft menu
    }
}