using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private Image slot;

    private Color defaultColor;
    public Color activeColor;

    public void Init()
    {
        slot = transform.GetComponent<Image>();
        defaultColor = slot.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        InventoryItem dragItem = eventData.pointerDrag.GetComponent<InventoryItem>();

        if (dragItem.active)
            return;

        if (transform.childCount == 0)
        {
            dragItem.parentAfterDrag = transform;
        }
        else
        {
            InventoryItem slotItem = transform.GetComponentInChildren<InventoryItem>();

            if (dragItem.item.itemID == slotItem.item.itemID)
            {
                slotItem.quantity += dragItem.quantity;
                slotItem.UpdateQuantity();

                Destroy(eventData.pointerDrag);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        InventoryItem inventoryItem = transform.GetComponentInChildren<InventoryItem>();

        if (inventoryItem != null)
        {
            if (inventoryItem.item is Food) // Food
            {
                Food food = (Food)inventoryItem.item;

                Player.Instance.hunger -= food.hungerValue;

                inventoryItem.quantity--;
                inventoryItem.UpdateQuantity();

                // Tutorial
                TutorialManager.Instance.NextTask(14);
            }
            else // Other
            {
                if (inventoryItem.active)
                {
                    inventoryItem.active = false;
                    ChangeSprite(false);
                    InventoryManager.Instance.activeItem = null;
                }
                else
                {
                    InventoryItem activeItem = InventoryManager.Instance.activeItem;

                    // Deselect already selected item
                    if (activeItem != null)
                    {
                        activeItem.active = false;
                        activeItem.slot.ChangeSprite(false);
                    }

                    // Select selected item
                    inventoryItem.active = true;
                    ChangeSprite(true);
                    InventoryManager.Instance.activeItem = inventoryItem;
                }
            }
        }
    }

    public void ChangeSprite(bool value)
    {
        if (value)
            slot.color = activeColor;
        else
            slot.color = defaultColor;
    }
}