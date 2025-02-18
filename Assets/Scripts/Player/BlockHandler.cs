using UnityEngine;

public class BlockHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 100f;

    public Item tempItem;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        InventoryManager.Instance.AddItem(tempItem);
    }

    void Update()
    {
        InventoryManager inventoryManager = InventoryManager.Instance;

        if (inventoryManager.inventory.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0)) // Left button
        {
            InventoryItem activeItem = inventoryManager.activeItem;

            if (activeItem != null && activeItem.item.GetType() == typeof(Block))
            {
                Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, maxDistance))
                {
                    Vector3 hitPoint = hit.point + (hit.normal * 0.5f);

                    World.Instance.SetBlock(Mathf.RoundToInt(hitPoint.x), Mathf.RoundToInt(hitPoint.y), Mathf.RoundToInt(hitPoint.z), activeItem.item.itemID);

                    activeItem.quantity--;
                    activeItem.UpdateQuantity();
                }
            }
        }
        else if (Input.GetMouseButtonDown(1)) // Right button
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                Vector3 hitPoint = hit.point - (hit.normal * 0.5f);

                int blockID = World.Instance.GetBlock(Mathf.RoundToInt(hitPoint.x), Mathf.RoundToInt(hitPoint.y), Mathf.RoundToInt(hitPoint.z));

                if (Blocks.Instance.blocksID.ContainsKey(blockID))
                    InventoryManager.Instance.AddItem(Blocks.Instance.blocksID[blockID]);

                World.Instance.SetBlock(Mathf.RoundToInt(hitPoint.x), Mathf.RoundToInt(hitPoint.y), Mathf.RoundToInt(hitPoint.z), 0);
            }
        }
    }
}
