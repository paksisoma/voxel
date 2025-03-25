using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraft", menuName = "Items/Craftable")]
public class Craftable : ScriptableObject
{
    public List<Ingredient> craftInput;
    public Ingredient craftOutput;
}