using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public CharacterController controller { get; private set; }
    public Vector3Int worldPosition { get; private set; }
    public Vector3Int chunkPosition { get; private set; }

    public GameObject rightHandParent;
    public CinemachineInputAxisController cinemachineInput;
    public Movement movement;
    public Animator animator;
    public Camera playerCamera;

    private float _health = 1f;
    public float health
    {
        get => _health;
        set
        {
            _health = value;
            HudManager.Instance.SetHealth(value);
        }
    }

    private float _hunger = 1f;
    public float hunger
    {
        get => _hunger;
        set
        {
            _hunger = value;
            HudManager.Instance.SetHunger(value);
        }
    }

    private float _thirst = 1f;
    public float thirst
    {
        get => _thirst;
        set
        {
            _thirst = value;
            HudManager.Instance.SetThirst(value);
        }
    }

    private float _temperature = 1f;
    public float temperature
    {
        get => _temperature;
        set
        {
            _temperature = value;
            HudManager.Instance.SetTemperature(value);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        worldPosition = Vector3Int.FloorToInt(transform.position);
        chunkPosition = Utils.WorldPositionToChunkPosition(worldPosition);
    }

    public void DisableCameraMouse()
    {
        cinemachineInput.enabled = false;
    }

    public void EnableCameraMouse()
    {
        cinemachineInput.enabled = true;
    }

    public void DisableActivity()
    {
        movement.activity = false;
    }

    public void EnableActivity()
    {
        movement.activity = true;
    }

    public void Chop()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Tree"))
            {
                Tree tree = hitObject.GetComponent<Tree>();
                tree.TakeDamage(20);
            }
        }
    }
}