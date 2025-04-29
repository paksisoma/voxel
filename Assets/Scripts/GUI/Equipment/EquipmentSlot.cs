using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour, IDropHandler
{
    private Image slot;

    private Color defaultColor;

    [HideInInspector]
    public Color activeColor;

    private void Awake()
    {
        slot = transform.GetComponent<Image>();
        defaultColor = slot.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        InventoryItem dragItem = eventData.pointerDrag.GetComponent<InventoryItem>();

        if (transform.childCount != 0) return;
        if (dragItem.active) return;
        if (dragItem.item is not Armor) return;

        dragItem.parentAfterDrag = transform;
    }

    public void ChangeSprite(bool value)
    {
        if (value)
            slot.color = activeColor;
        else
            slot.color = defaultColor;
    }
}