using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Item item;

    public Image image;
    public Text label;

    [HideInInspector]
    public Transform parentAfterDrag;

    [HideInInspector]
    public bool active = false;

    public InventorySlot slot;

    public int quantity = 1;

    public void Init(Item item)
    {
        this.item = item;
        image.sprite = item.itemImage;
        slot = transform.GetComponentInParent<InventorySlot>(true);

        if (item.stackable)
            label.text = quantity.ToString();
    }

    public void UpdateQuantity()
    {
        if (quantity == 0)
        {
            slot.ChangeSprite(false);
            InventoryManager.Instance.activeItem = null;
            DestroyImmediate(transform.gameObject);
        }
        else
        {
            if (item.stackable)
                label.text = quantity.ToString();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || active)
            return;

        image.raycastTarget = false;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || active)
            return;

        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || active)
            return;

        image.raycastTarget = true;
        transform.SetParent(parentAfterDrag);
        slot = transform.GetComponentInParent<InventorySlot>();

        if (item is Armor)
        {
            EquipmentSlot equipmentSlot = transform.GetComponentInParent<EquipmentSlot>();

            if (equipmentSlot == null)
                InventoryManager.Instance.activeArmor = null;
            else
                InventoryManager.Instance.activeArmor = (Armor)item;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Tooltip.Instance.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.Instance.Hide();
    }
}