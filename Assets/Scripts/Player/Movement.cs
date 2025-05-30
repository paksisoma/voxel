using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class Movement : MonoBehaviour
{
    public Transform _camera;

    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private float verticalVelocity;
    private Vector3 additionalVelocity = Vector3.zero;

    private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    [HideInInspector]
    public bool activity = true;

    private bool previousIsGrounded = false;

    private float additionalVelocityTimer = 0f;

    private void Update()
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

        if (Player.Instance.controller.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * (gravity * 2f));
                Player.Instance.animator.SetBool("isJumping", true);
                previousIsGrounded = false;
            }
            else
            {
                Player.Instance.animator.SetBool("isJumping", false);
                previousIsGrounded = true;
            }
        }
        else
        {
            if (previousIsGrounded)
            {
                Vector3 rayOrigin = Player.Instance.GetControllerBottom();
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1f))
                    verticalVelocity = -10f;
                else
                    verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * 2 * Time.deltaTime;
            }

            previousIsGrounded = false;
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
            previousIsGrounded = false;
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

        // Tutorial
        if (TutorialManager.Instance.currentTask < 5)
        {
            // Forward
            if (vertical > 0.5f)
                TutorialManager.Instance.NextTask(1);

            // Backward
            if (vertical < -0.5f)
                TutorialManager.Instance.NextTask(2);

            // Left
            if (horizontal < -0.5f)
                TutorialManager.Instance.NextTask(3);

            // Right
            if (horizontal > 0.5f)
                TutorialManager.Instance.NextTask(4);

            // Jump
            if (verticalVelocity > 0.5f)
                TutorialManager.Instance.NextTask(5);
        }

        Vector3 velocity = (moveDirection.normalized * speed + Vector3.up * verticalVelocity + additionalVelocity) * Time.deltaTime;
        Player.Instance.controller.Move(velocity);
    }

    public void AttackEffect(Vector3 attackerPosition)
    {
        Player.Instance.movement.additionalVelocity = (transform.position - attackerPosition).normalized * 10f;

        if (Player.Instance.controller.isGrounded)
            Player.Instance.movement.verticalVelocity = Mathf.Sqrt(-2f * (gravity * 2f));

        previousIsGrounded = false;
    }
}