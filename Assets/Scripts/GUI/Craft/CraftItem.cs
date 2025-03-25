using UnityEngine;
using UnityEngine.UI;

public class CraftItem : MonoBehaviour
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
}