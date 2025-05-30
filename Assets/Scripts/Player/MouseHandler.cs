using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class MouseHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance = 10f;

    public float holdThreshold = 0.25f;
    private float mouseDownTime = 0f;
    private bool isMouseDown = false;
    private bool isHolding = false;

    private float waterTimer = 0f;
    public Text interact;

    private int layerMask;

    private void Awake()
    {
        layerMask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Ignore Raycast")));
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // Drink water
        if (waterTimer > 0f)
        {
            waterTimer -= Time.deltaTime;
        }

        if (GetHitPointIn(out Vector3Int worldPosition) && World.Instance.GetBlock(worldPosition) == 3)
        {
            if (waterTimer <= 0)
            {
                if (Input.GetKeyDown("e") && waterTimer <= 0)
                {
                    waterTimer = 2f;
                    Player.Instance.thirst -= 0.1f;

                    // Tutorial
                    TutorialManager.Instance.NextTask(15);
                }

                interact.text = "Press E to drink";
            }
            else
            {
                interact.text = waterTimer.ToString("F1") + " seconds until you can drink again";
            }
        }
        else
        {
            interact.text = "";
        }

        if (ContainerManager.Instance.panel.activeSelf)
        {
            if (isHolding)
                MouseHoldEnd();

            isMouseDown = false;
            isHolding = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            mouseDownTime = Time.time;
            isHolding = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isHolding)
                MouseHoldEnd();
            else if (Time.time - mouseDownTime < holdThreshold)
                MouseClick();

            isMouseDown = false;
            isHolding = false;
        }

        if (isMouseDown && !isHolding)
        {
            float heldDuration = Time.time - mouseDownTime;

            if (heldDuration >= holdThreshold)
            {
                MouseHoldStart();
                isHolding = true;
            }
        }
    }

    private void MouseClick()
    {
        InventoryItem activeItem = InventoryManager.Instance.activeItem;
        GameObject special = null;

        if (GetHitPointOut(out Vector3Int worldPosition))
            special = World.Instance.GetSpecial(worldPosition);

        if (special == null)
        {
            if (activeItem != null)
            {
                if (activeItem.item is Block) // Block selected
                {
                    if (GetHitPointOut(out worldPosition))
                    {
                        World.Instance.SetBlock(worldPosition, activeItem.item.itemID);

                        activeItem.quantity--;
                        activeItem.UpdateQuantity();

                        // Tutorial
                        TutorialManager.Instance.NextTask(17);
                    }
                }
                else if (activeItem.item is Special) // Special selected
                {
                    if (GetHitPointOut(out worldPosition))
                    {
                        Special specialItem = (Special)activeItem.item;

                        World.Instance.AddSpecial(worldPosition, specialItem);

                        activeItem.quantity--;
                        activeItem.UpdateQuantity();

                        // Tutorial
                        if (TutorialManager.Instance.currentTask == 8 && specialItem.itemID == 105)
                            TutorialManager.Instance.NextTask(9);
                    }
                }
            }
        }
        else
        {
            if (special.TryGetComponent(out SpecialObject specialObject))
                specialObject.Click();

            // Tutorial
            if (TutorialManager.Instance.currentTask == 6)
            {
                int stickQuantity = InventoryManager.Instance.CountItemsQuantity(Items.Instance.items[104]);
                int rockQuantity = InventoryManager.Instance.CountItemsQuantity(Items.Instance.items[103]);

                if (stickQuantity > 0 && rockQuantity > 0)
                    TutorialManager.Instance.NextTask(7);
            }
        }
    }

    private void MouseHoldStart()
    {
        InventoryItem activeItem = InventoryManager.Instance.activeItem;

        if (activeItem == null) // No tool selected
            Player.Instance.animator.SetBool("isPunching", true);
        else
            if (activeItem.item is Tool) // Tool selected
            Player.Instance.animator.SetBool("isChopping", true);

        TutorialManager.Instance.NextTask(6);
    }

    private void MouseHoldEnd()
    {
        Player.Instance.animator.SetBool("isChopping", false);
        Player.Instance.animator.SetBool("isPunching", false);
    }

    public void Chop()
    {
        InventoryItem activeItem = InventoryManager.Instance.activeItem;

        if (activeItem)
        {
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

                                // Tutorial
                                if (activeItem.item.itemID != 2)
                                    TutorialManager.Instance.NextTask(18);
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

                                // Tutorial
                                TutorialManager.Instance.NextTask(16);
                            }
                        }
                    }
                    else if (tool.canChop)
                    {
                        if (GetHitPointPrefab(out GameObject gameObject) && gameObject.TryGetComponent(out SpecialObject specialObject))
                            specialObject.Hit();
                    }
                }
            }
        }
        else
        {
            if (GetHitPointPrefab(out GameObject npcGameObject) && npcGameObject.CompareTag("NPC")) // NPC
            {
                NPC npc = npcGameObject.GetComponent<NPC>();
                npc.AttackEffect(Player.Instance.transform.position);
                npc.health -= PUNCH_DAMAGE;
            }
        }
    }

    private bool GetHitPointIn(out Vector3Int worldPosition)
    {
        worldPosition = default;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 50, layerMask))
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

        if (Physics.Raycast(ray, out hit, 50, layerMask))
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

        if (Physics.Raycast(ray, out hit, 50, layerMask))
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