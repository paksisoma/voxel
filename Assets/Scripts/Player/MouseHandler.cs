using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 100f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        InventoryManager inventoryManager = InventoryManager.Instance;

        if (inventoryManager.inventory.activeSelf)
            return;

        InventoryItem activeItem = inventoryManager.activeItem;

        // No item selected
        if (activeItem == null)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                Vector3Int hitPoint = Vector3Int.RoundToInt(hit.point - hit.normal * 0.5f);

                int blockID = World.Instance.GetBlock(hitPoint);

                if (Blocks.Instance.blocksID.ContainsKey(blockID))
                    InventoryManager.Instance.AddItem(Blocks.Instance.blocksID[blockID]);

                World.Instance.SetBlock(hitPoint, 0);
            }
        }
        else
        {
            if (activeItem.item is Tool) // Tool selected
            {
                Player.Instance.animator.SetTrigger("chop");
            }
            else if (activeItem.item is Block) // Block selected
            {
                Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, maxDistance))
                {
                    Vector3Int hitPoint = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);

                    World.Instance.SetBlock(hitPoint, activeItem.item.itemID);

                    activeItem.quantity--;
                    activeItem.UpdateQuantity();
                }
            }
        }
    }
}