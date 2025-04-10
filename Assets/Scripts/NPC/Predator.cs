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

        if (Vector3.Distance(Player.Instance.transform.position, transform.position) <= detectRange)
            follow = true;

        if (Vector3.Distance(Player.Instance.transform.position, transform.position) > chaseRange)
            follow = false;

        if (Vector3.Distance(Player.Instance.transform.position, transform.position) <= attackRange && canHit)
        {
            Player.Instance.movement.AttackEffect(transform.position);
            Player.Instance.health -= damage;
            canHit = false;
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