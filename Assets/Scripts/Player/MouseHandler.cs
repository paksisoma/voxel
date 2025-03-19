using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 100f;

    public Item rock;

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
                Vector3Int worldPosition = Vector3Int.RoundToInt(hit.point - hit.normal * 0.5f);

                worldPosition.y++;

                GameObject prefab = World.Instance.GetPrefab(worldPosition);

                if (prefab != null && prefab.CompareTag("Rock"))
                {
                    Destroy(prefab);
                    InventoryManager.Instance.AddItem(rock);
                }
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