using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Predator : NPC
{
    // Range
    public float detectRange = 40f;
    public float chaseRange = 80f;
    public float attackRange = 5f;

    // Attack
    private float hitTimer = 0f;
    public float hitInterval = 3f;
    private bool canHit = true;

    protected override void Update()
    {
        base.Update();

        float playerDistance = Vector3.Distance(Player.Instance.transform.position, transform.position);

        if (playerDistance <= attackRange && canHit)
        {
            Player.Instance.movement.AttackEffect(transform.position);
            Player.Instance.Damage(damage);
            canHit = false;
        }
        else if (playerDistance <= detectRange)
        {
            follow = true;
        }
        else if (playerDistance > chaseRange)
        {
            follow = false;
            ClearPath();
        }

        if (canHit == false)
        {
            hitTimer += Time.deltaTime;

            if (hitTimer > hitInterval)
            {
                canHit = true;
                hitTimer = 0f;
            }
        }
    }
}