using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class Movement : MonoBehaviour
{
    public Transform _camera;

    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private float verticalVelocity;

    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [HideInInspector]
    public bool activity = true;

    void Update()
    {
        float horizontal;
        float vertical;

        if (activity)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }
        else
        {
            horizontal = 0f;
            vertical = 0f;
        }

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveDirection = Vector3.zero;

        // Jump, fall
        if (Player.Instance.controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Player.Instance.animator.SetBool("isJumping", true);
            }
            else
            {
                Player.Instance.animator.SetBool("isJumping", false);
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

            Player.Instance.animator.SetBool("isRunning", true);
        }
        else
        {
            Player.Instance.animator.SetBool("isRunning", false);
        }

        Vector3 velocity = (moveDirection.normalized * speed + Vector3.up * verticalVelocity) * Time.deltaTime;
        Player.Instance.controller.Move(velocity);
    }
}