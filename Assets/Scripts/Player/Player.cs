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
}