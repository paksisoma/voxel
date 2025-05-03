using UnityEngine;
using static Constants;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public CharacterController controller { get; private set; }
    public Vector3Int worldPosition { get; private set; }
    public Vector3Int chunkPosition { get; private set; }
    public Vector3Int groundPosition { get; private set; }

    public GameObject rightHandParent;
    public Movement movement;
    public Animator animator;
    public Camera playerCamera;

    private float armorRechargeTimer = 0f;

    public float environmentTemperature = 0f;

    private float _health = 1f;
    public float health
    {
        get => _health;
        set
        {
            _health = Mathf.Min(1, Mathf.Max(value, 0));
            HudManager.Instance.SetHealth(_health);
        }
    }

    private float _armor = 1f;
    public float armor
    {
        get => _armor;
        set
        {
            _armor = Mathf.Min(1, Mathf.Max(value, 0));
            HudManager.Instance.SetArmor(_armor);
        }
    }

    private float _hunger = 1f;
    public float hunger
    {
        get => _hunger;
        set
        {
            _hunger = Mathf.Min(1, Mathf.Max(value, 0));
            HudManager.Instance.SetHunger(_hunger);
        }
    }

    private float _thirst = 1f;
    public float thirst
    {
        get => _thirst;
        set
        {
            _thirst = Mathf.Min(1, Mathf.Max(value, 0));
            HudManager.Instance.SetThirst(_thirst);
        }
    }

    private float _temperature = 1f;
    public float temperature
    {
        get => _temperature;
        set
        {
            _temperature = Mathf.Min(1, Mathf.Max(value, 0));
            HudManager.Instance.SetTemperature(_temperature);
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        worldPosition = Vector3Int.RoundToInt(transform.position);
        chunkPosition = Utils.WorldPositionToChunkPosition(worldPosition);
        groundPosition = World.Instance.TryGetGroundPosition(worldPosition, out Vector3Int position) ? position : groundPosition;

        UpdateTemperature();
        UpdateArmor();
        UpdateStats();
    }

    private void UpdateTemperature()
    {
        Armor activeArmor = InventoryManager.Instance.activeArmor;
        float armorResistance = activeArmor ? activeArmor.resistance : 0f;

        float outsideTemperature = Mathf.Max(0f, worldPosition.z) * 0.000025f;
        temperature += (Mathf.Min(-outsideTemperature + armorResistance, 0f) + environmentTemperature) * Time.deltaTime;
    }

    private void UpdateArmor()
    {
        if (armorRechargeTimer > 0)
        {
            armorRechargeTimer -= Time.deltaTime;
            return;
        }

        Armor activeArmor = InventoryManager.Instance.activeArmor;
        armor = activeArmor ? activeArmor.defense : 0f;
    }

    private void UpdateStats()
    {
        hunger += 0.001f * Time.deltaTime;
        thirst += 0.001f * Time.deltaTime;

        if (temperature <= 0 || hunger >= 1 || thirst >= 1)
            health -= 0.05f * Time.deltaTime;
        else
            health += 0.005f * Time.deltaTime;
    }

    public void Damage(float value)
    {
        float newArmor = armor - value;

        if (newArmor >= 0)
        {
            armor = newArmor;
        }
        else
        {
            armor = 0;
            health += newArmor;
        }

        armorRechargeTimer = ARMOR_RECHARGE_DELAY;
    }

    public void WarpPlayer(Vector3 worldPosition)
    {
        controller.enabled = false;
        ThirdPersonCamera.Instance.smoothPosition = worldPosition;
        controller.transform.position = worldPosition;
        controller.enabled = true;
    }

    public void WarpPlayerUp(Vector2 worldPosition)
    {
        if (Physics.Raycast(new Vector3(worldPosition.x, 500, worldPosition.y), Vector3.down, out RaycastHit hit, 500))
            WarpPlayer(hit.point + Vector3.up * 2f);
    }

    public Vector3 GetControllerBottom()
    {
        Vector3 center = controller.transform.position + controller.center;
        float bottomY = center.y - (controller.height / 2);
        return new Vector3(center.x, bottomY, center.z);
    }

    public void DisableCameraMouse()
    {
        ThirdPersonCamera.Instance.active = false;
    }

    public void EnableCameraMouse()
    {
        ThirdPersonCamera.Instance.active = true;
    }

    public void DisableActivity()
    {
        movement.activity = false;
    }

    public void EnableActivity()
    {
        movement.activity = true;
    }
}