using UnityEngine;

public class Campfire : SpecialObject
{
    private float workingDuration = 0f;
    private float maxDuration = 0f;
    private bool working = false;

    public GameObject fire;

    private void Update()
    {
        if (!working) return;

        workingDuration += Time.deltaTime;

        if (workingDuration > maxDuration)
            Deactivate();
    }

    public override void Click()
    {
        InventoryItem activeItem = InventoryManager.Instance.activeItem;

        if (activeItem != null && activeItem.item.itemID == 104)
        {
            activeItem.quantity--;
            activeItem.UpdateQuantity();
            maxDuration += 5f;
            Activate();
        }
    }

    public override void Hit()
    {
        Destroy(transform.gameObject);
    }

    private void Activate()
    {
        working = true;
        fire.SetActive(true);
    }

    private void Deactivate()
    {
        workingDuration = 0f;
        working = false;
        fire.SetActive(false);
    }
}