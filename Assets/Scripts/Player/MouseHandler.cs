using UnityEngine;

public class MouseHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 10f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
            Player.Instance.animator.SetBool("isChopping", false);

        if (!Input.GetMouseButtonDown(0))
            return;

        if (ContainerManager.Instance.panel.activeSelf)
            return;

        InventoryItem activeItem = InventoryManager.Instance.activeItem;

        if (activeItem == null) // No item selected
        {
            if (GetHitPointOut(out Vector3Int worldPosition))
            {
                GameObject prefab = World.Instance.GetPrefab(worldPosition);

                if (prefab != null && prefab.CompareTag("Rock"))
                {
                    Destroy(prefab);
                    InventoryManager.Instance.AddItem(103);
                }
            }
        }
        else // Item selected
        {
            if (activeItem.item is Tool) // Tool selected
            {
                Player.Instance.animator.SetBool("isChopping", true);
            }
            else if (activeItem.item is Block) // Block selected
            {
                if (GetHitPointOut(out Vector3Int worldPosition))
                {
                    World.Instance.SetBlock(worldPosition, activeItem.item.itemID);

                    activeItem.quantity--;
                    activeItem.UpdateQuantity();
                }
            }
        }
    }

    public void Chop()
    {
        InventoryItem activeItem = InventoryManager.Instance.activeItem;

        byte activeItemID = activeItem.item.itemID;

        if (activeItemID == 100) // Axe
        {
            if (GetHitPointPrefab(out GameObject gameObject))
            {
                if (gameObject.CompareTag("Tree"))
                {
                    Tree tree = gameObject.GetComponent<Tree>();
                    tree.TakeDamage(20);
                    tree.ChopTree();
                }
            }
        }
        else if (activeItemID == 101) // Shovel
        {
            if (GetHitPointIn(out Vector3Int worldPosition))
            {
                byte blockID = World.Instance.GetBlock(worldPosition);

                if (blockID == 1 || blockID == 4 || blockID == 6)
                {
                    World.Instance.SetBlock(worldPosition, 0);
                    InventoryManager.Instance.AddItem(blockID);
                }
            }
        }
        else if (activeItemID == 102) // Pickaxe
        {
            if (GetHitPointIn(out Vector3Int worldPosition))
            {
                byte blockID = World.Instance.GetBlock(worldPosition);

                if (blockID == 2)
                {
                    World.Instance.SetBlock(worldPosition, 0);
                    InventoryManager.Instance.AddItem(blockID);
                }
            }
        }
    }

    private bool GetHitPointIn(out Vector3Int worldPosition)
    {
        worldPosition = default;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 50))
        {
            Vector3Int hitPosition = Vector3Int.RoundToInt(hit.point - hit.normal * 0.5f);

            if (Vector3Int.Distance(Player.Instance.worldPosition, hitPosition) <= maxDistance)
            {
                worldPosition = hitPosition;
                return true;
            }
        }

        return false;
    }

    private bool GetHitPointOut(out Vector3Int worldPosition)
    {
        worldPosition = default;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 50))
        {
            Vector3Int hitPosition = Vector3Int.RoundToInt(hit.point + hit.normal * 0.5f);

            if (Vector3Int.Distance(Player.Instance.worldPosition, hitPosition) <= maxDistance)
            {
                worldPosition = hitPosition;
                return true;
            }
        }

        return false;
    }

    private bool GetHitPointPrefab(out GameObject prefab)
    {
        prefab = default;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 50))
        {
            if (Vector3.Distance(Player.Instance.worldPosition, hit.transform.position) <= maxDistance)
            {
                prefab = hit.collider.gameObject;
                return true;
            }
        }

        return false;
    }
}