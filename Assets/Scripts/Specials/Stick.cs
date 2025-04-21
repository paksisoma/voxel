public class Stick : SpecialObject
{
    public override void Click()
    {
        InventoryManager.Instance.AddItem(104);
        Destroy(transform.gameObject);
    }
}