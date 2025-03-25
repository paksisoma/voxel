using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Item item;

    public Image image;
    public Text text;

    [HideInInspector]
    public Transform parentAfterDrag;

    [HideInInspector]
    public bool active = false;

    private InventorySlot slot;

    public int quantity = 1;

    public void Init(Item item)
    {
        this.item = item;
        image.sprite = item.itemImage;
        slot = transform.GetComponentInParent<InventorySlot>(true);
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
            text.text = quantity.ToString();
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
    }
}