public class Rock : SpecialObject
{
    public override void Click()
    {
        InventoryManager.Instance.AddItem(103);
        Destroy(transform.gameObject);
    }
}