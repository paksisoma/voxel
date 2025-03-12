using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health = 100;

    private Quaternion originalRotation;
    private Quaternion targetRotation;

    private bool isTilting = false;
    private float tiltProgress = 0f;

    private void Awake()
    {
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        if (isTilting)
        {
            tiltProgress += Time.deltaTime * 20f;
            transform.rotation = Quaternion.Slerp(originalRotation, targetRotation, Mathf.PingPong(tiltProgress, 1f));

            if (tiltProgress >= 2f)
            {
                isTilting = false;
                tiltProgress = 0f;
            }
            /*else if (tiltProgress >= 1f && health <= 0)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                    rb = gameObject.AddComponent<Rigidbody>();
                    
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.angularVelocity = -Player.Instance.transform.forward * 1f;
            }*/
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Destroy(gameObject);
            ProgressBar.Instance.CloseProgress();
            return;
        }

        ChopTree();

        ProgressBar.Instance.SetProgress(1f - (float)health / 100);
    }

    private void ChopTree()
    {
        if (!isTilting)
        {
            Vector3 playerRotation = Player.Instance.transform.eulerAngles;
            playerRotation.y -= 90;

            float x = Mathf.Cos(playerRotation.y * Mathf.Deg2Rad);
            float z = Mathf.Sin((playerRotation.y - 180) * Mathf.Deg2Rad);

            targetRotation = Quaternion.Euler(x * 1, 0, z * 1);
            isTilting = true;
        }
    }
}