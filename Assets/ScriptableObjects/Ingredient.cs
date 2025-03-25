using UnityEngine;

[CreateAssetMenu(fileName = "NewIngredient", menuName = "Items/Ingredient")]
public class Ingredient : ScriptableObject
{
    public Item item;
    public int quantity;
}