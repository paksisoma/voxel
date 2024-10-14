using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class Movement : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    public Transform _camera;

    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private float verticalVelocity;

    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    void Awake()
    {
        controller = Player.Instance.controller;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveDirection = Vector3.zero;

        // Jump, fall
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetBool("isJumping", true);
            }
            else
            {
                animator.SetBool("isJumping", false);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Move
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camera.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        Vector3 velocity = (moveDirection.normalized * speed + Vector3.up * verticalVelocity) * Time.deltaTime;
        controller.Move(velocity);
    }
}