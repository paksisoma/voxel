using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private Image slot;

    private Color defaultColor;
    public Color activeColor;

    private void Awake()
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
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        InventoryItem item = transform.GetComponentInChildren<InventoryItem>();

        if (item != null)
        {
            if (item.active)
            {
                item.active = false;
                ChangeSprite(false);
                InventoryManager.Instance.activeItem = null;
            }
            else
            {
                if (InventoryManager.Instance.activeItem == null)
                {
                    item.active = true;
                    ChangeSprite(true);
                    InventoryManager.Instance.activeItem = item;
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