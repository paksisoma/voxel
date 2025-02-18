using Unity.Cinemachine;
using UnityEngine;
using static Constants;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    public CharacterController controller { get; private set; }
    public Vector3Int chunkPosition { get; private set; }

    public GameObject rightHandParent;

    public CinemachineInputAxisController cinemachineInput;

    public Movement movement;

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
        UpdatePosition();
    }

    void UpdatePosition()
    {
        int playerChunkX = Mathf.FloorToInt(transform.position.x / CHUNK_SIZE_NO_PADDING);
        int playerChunkY = Mathf.FloorToInt(transform.position.y / CHUNK_SIZE_NO_PADDING);
        int playerChunkZ = Mathf.FloorToInt(transform.position.z / CHUNK_SIZE_NO_PADDING);

        chunkPosition = new Vector3Int(playerChunkX, playerChunkY, playerChunkZ);
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