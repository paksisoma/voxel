using UnityEngine;

public class Movement : MonoBehaviour
{
    private CharacterController controller;

    public Transform _camera;

    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 faceDirection = new Vector3(_camera.forward.x, 0, _camera.forward.z);
        float cameraAngle = Vector3.SignedAngle(Vector3.forward, faceDirection, Vector3.up);
        Vector3 moveDirection = Quaternion.Euler(0, cameraAngle, 0) * direction;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;

        transform.rotation = Quaternion.Euler(0f, _camera.localRotation.eulerAngles.y, 0f);
        controller.Move(velocity * Time.deltaTime);
    }
}