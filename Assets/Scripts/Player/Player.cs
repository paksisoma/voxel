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