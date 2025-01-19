using UnityEngine;

[CreateAssetMenu(fileName = "NewBlock", menuName = "Items/Block")]
public class Block : Item
{
    public Material topMaterial;
    public Material sideMaterial;
    public Material bottomMaterial;
}