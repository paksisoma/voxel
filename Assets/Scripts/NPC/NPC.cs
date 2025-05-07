using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class NPC : MonoBehaviour
{
    // Movement
    public float movementSpeed = 5f;

    // Rotation
    public float rotationSpeed = 10f;
    private int targetRotation = 0;

    // Controller
    private CharacterController controller;

    // Fast fall
    private bool fastFall = false;

    // Gravity
    protected float gravity = -9.81f;

    // Velocity
    private Vector3 movementVelocity = Vector3.zero;
    private Vector3 verticalVelocity = Vector3.zero;
    private Vector3 additionalVelocity = Vector3.zero;

    // Path finding
    private List<Vector3Int> pathList = new List<Vector3Int>();
    private int pathIndex = 0;
    private Vector3Int goalPosition = Vector3Int.zero;
    private bool hasGoal = false;
    public bool follow = false;

    // Timer
    private float additionalVelocityTimer = 0f;
    private float pathTimer = 0f;
    public float pathInterval = 0.5f;

    // Position
    public Vector3Int worldPosition { get; private set; }
    public Vector3Int chunkPosition { get; private set; }

    // Health
    private float _health = 1f;
    public float health
    {
        get => _health;
        set
        {
            _health = value;

            if (_health <= 0f)
            {
                Destroy(transform.gameObject);

                InventoryManager.Instance.AddItem(200);

                // Tutorial
                TutorialManager.Instance.NextTask(13);
            }
        }
    }

    // Damage
    public float damage = 0.1f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        // Update position
        worldPosition = Vector3Int.RoundToInt(transform.position);
        chunkPosition = Utils.WorldPositionToChunkPosition(worldPosition);

        // Destroy if out of chunk
        if (!World.Instance.IsValidChunk(chunkPosition))
            Destroy(transform.gameObject);

        // Follow main character
        if (follow)
        {
            goalPosition = Player.Instance.groundPosition - new Vector3Int(0, 1, 0);
            hasGoal = true;
        }

        // Path finding
        if (hasGoal)
        {
            if (pathTimer > pathInterval)
            {
                Vector3Int startPosition = Vector3Int.RoundToInt(transform.position);

                if (World.Instance.TryPathfinding(startPosition, goalPosition, out List<Vector3Int> list))
                {
                    pathList = list;
                    pathIndex = 0;

                    if (follow && pathList.Count > 0)
                        pathList.RemoveAt(pathList.Count - 1);
                }

                pathTimer = 0f;
            }
            else
            {
                pathTimer += Time.deltaTime;
            }
        }

        // Rotation
        Quaternion currentRotation = transform.rotation;
        Quaternion targetQuaternion = Quaternion.Euler(0, targetRotation, 0);
        Quaternion newRotation = Quaternion.Slerp(currentRotation, targetQuaternion, Time.deltaTime * rotationSpeed);
        transform.rotation = newRotation;

        // Ground check
        if (controller.isGrounded)
        {
            fastFall = true;

            // Follow path
            if (hasGoal && pathList.Count > 0)
            {
                // Move
                targetRotation = Mathf.RoundToInt(Mathf.Atan2(pathList[pathIndex].x - transform.position.x, pathList[pathIndex].z - transform.position.z) * Mathf.Rad2Deg);
                movementVelocity = transform.forward;

                // Get closest point
                int closestIndex = pathIndex;
                float closestDistance = Vector3.Distance(transform.position, pathList[closestIndex]);

                for (int i = pathIndex + 1; i < pathIndex + 3 && i < pathList.Count; i++)
                {
                    if (Vector3.Distance(transform.position, pathList[i]) < closestDistance)
                    {
                        closestDistance = Vector3.Distance(transform.position, pathList[i]);
                        pathIndex = i;
                    }
                }

                // Increase path index if close enough, clear list if reached end
                if (closestDistance < 0.25f)
                {
                    if (pathIndex + 1 >= pathList.Count)
                    {
                        pathList.Clear();
                        pathIndex = 0;
                    }
                    else
                    {
                        pathIndex++;
                    }
                }
            }
            else
            {
                movementVelocity = Vector3.zero;
            }
        }
        else
        {
            movementVelocity = Vector3.zero;

            if (fastFall)
            {
                Vector3 rayOrigin = Player.Instance.GetControllerBottom();
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1f))
                    verticalVelocity.y = -10f;
                else
                    verticalVelocity.y = -2f;

                fastFall = false;
            }
            else
            {
                verticalVelocity.y += gravity * 2 * Time.deltaTime;
            }
        }

        // Additional velocity
        if (additionalVelocity == Vector3.zero)
        {
            additionalVelocityTimer = 0f;
        }
        else
        {
            float x = Mathf.Lerp(additionalVelocity.x, 0, additionalVelocityTimer);
            float z = Mathf.Lerp(additionalVelocity.z, 0, additionalVelocityTimer);

            additionalVelocityTimer += 0.05f * Time.deltaTime;
            additionalVelocity = new Vector3(x, 0, z);
            fastFall = false;
        }

        // Velocity
        Vector3 velocity = (movementVelocity.normalized * movementSpeed + verticalVelocity + additionalVelocity) * Time.deltaTime;
        controller.Move(velocity);

        // Debug
#if UNITY_EDITOR
        if (pathList == null || pathList.Count < 2) return;

        for (int i = 0; i < pathList.Count - 1; i++)
            Debug.DrawLine(pathList[i], pathList[i + 1], Color.green);
#endif
    }

    public void SetGoal(Vector3Int goalPosition)
    {
        this.goalPosition = goalPosition;
        hasGoal = true;
    }

    public void ClearPath()
    {
        pathList.Clear();
        pathIndex = 0;
        hasGoal = false;
    }

    public void AttackEffect(Vector3 attackerPosition)
    {
        additionalVelocity = (transform.position - attackerPosition).normalized * 10f;
        additionalVelocityTimer = 0f;

        if (controller.isGrounded)
            verticalVelocity = new Vector3(0, Mathf.Sqrt(1f * -2f * (gravity * 2f)));
    }
}