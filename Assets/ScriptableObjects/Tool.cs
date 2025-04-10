using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "Items/Tool")]
public class Tool : Item
{
    public GameObject model;
    public bool canMine;
    public bool canDig;
    public bool canChop;
    public float damage;
}