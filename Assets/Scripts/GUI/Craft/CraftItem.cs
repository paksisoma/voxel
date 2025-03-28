using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item item;

    public Image image;
    public Text text;
    public int quantity = 1;

    public void Init(Ingredient ingredient)
    {
        item = ingredient.item;
        image.sprite = ingredient.item.itemImage;
        quantity = ingredient.quantity;
        text.text = quantity.ToString();
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