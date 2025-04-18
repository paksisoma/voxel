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

                if (prefab != null)
                {
                    if (prefab.CompareTag("Rock"))
                    {
                        Destroy(prefab);
                        World.Instance.SetBlock(worldPosition, 0);
                        InventoryManager.Instance.AddItem(103);
                    }
                    else if (prefab.CompareTag("Stick"))
                    {
                        Destroy(prefab);
                        World.Instance.SetBlock(worldPosition, 0);
                        InventoryManager.Instance.AddItem(104);
                    }
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

        if (activeItem.item is Tool)
        {
            Tool tool = (Tool)activeItem.item;

            if (GetHitPointPrefab(out GameObject npcGameObject) && npcGameObject.CompareTag("NPC")) // NPC
            {
                NPC npc = npcGameObject.GetComponent<NPC>();
                npc.AttackEffect(Player.Instance.transform.position);
                npc.health -= tool.damage;
            }
            else // Block
            {
                if (tool.canMine)
                {
                    if (GetHitPointIn(out Vector3Int worldPosition))
                    {
                        byte blockID = World.Instance.GetBlock(worldPosition);
                        Block block = Items.Instance.blocks[blockID];

                        if (block.isMineable)
                        {
                            World.Instance.SetBlock(worldPosition, 0);
                            InventoryManager.Instance.AddItem(blockID);
                        }
                    }
                }
                else if (tool.canDig)
                {
                    if (GetHitPointIn(out Vector3Int worldPosition))
                    {
                        byte blockID = World.Instance.GetBlock(worldPosition);
                        Block block = Items.Instance.blocks[blockID];

                        if (block.isDiggable)
                        {
                            World.Instance.SetBlock(worldPosition, 0);
                            InventoryManager.Instance.AddItem(blockID);
                        }
                    }
                }
                else if (tool.canChop)
                {
                    if (GetHitPointPrefab(out GameObject gameObject) && gameObject.CompareTag("Tree"))
                    {
                        Tree tree = gameObject.GetComponent<Tree>();
                        tree.TakeDamage(20);
                        tree.ChopTree();
                    }
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