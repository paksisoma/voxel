using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public static ThirdPersonCamera Instance { get; private set; }

    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -4);
    public float sensitivity = 2.0f;
    public float distance = 4.0f;
    public float minDistance = 4.0f;
    public float maxDistance = 10.0f;
    public float zoomSpeed = 2.0f;
    public float minYAngle = -20f, maxYAngle = 60f;

    public float yaw = 0.0f;
    public float pitch = 0.0f;

    public float smoothSpeed = 5f;
    public Vector3 smoothPosition;

    public bool active = true;

    private void Awake()
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

        smoothPosition = target.position;
    }

    private void LateUpdate()
    {
        // Camera zoom
        if (active)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        // Camera rotation
        smoothPosition = new Vector3(target.position.x, Mathf.Lerp(smoothPosition.y, target.position.y, smoothSpeed * Time.deltaTime), target.position.z);

        if (active)
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = smoothPosition + rotation * new Vector3(0, offset.y, -distance);
        Vector3Int blockPosition = new Vector3Int((int)desiredPosition.x, (int)desiredPosition.y, (int)desiredPosition.z);

        if (World.Instance.IsValidChunk(Utils.WorldPositionToChunkPosition(blockPosition)) && World.Instance.GetBlock(blockPosition) != 0)
        {
            RaycastHit hit;

            if (Physics.Raycast(smoothPosition, desiredPosition - smoothPosition, out hit, distance))
                desiredPosition = hit.point;
        }

        transform.position = desiredPosition;
        transform.LookAt(smoothPosition + Vector3.up * offset.y);
    }
}