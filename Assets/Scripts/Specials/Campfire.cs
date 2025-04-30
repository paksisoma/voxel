using UnityEngine;

public class Campfire : SpecialObject
{
    private float fireDuration = 0f;
    private float maxDuration = 0f;
    public bool active = false;

    public GameObject fire;

    private void Awake()
    {
        CampfireManager.Instance.campfires.Add(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CampfireManager.Instance.campfires.Remove(this);
    }

    private void Update()
    {
        if (!active) return;

        fireDuration += Time.deltaTime;

        if (fireDuration > maxDuration)
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
        active = true;
        fire.SetActive(true);
    }

    private void Deactivate()
    {
        fireDuration = 0f;
        active = false;
        fire.SetActive(false);
    }
}