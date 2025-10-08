using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;
    public float attackCooldown = 1f;
    private bool canAttack = true;

    void Update()
    {
        // Linkermuisknop om te slaan
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            Attack();
        }
    }

    void Attack()
    {
        // Trigger de animatie
        animator.SetTrigger("Attack");

        // Start cooldown
        canAttack = false;
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    void ResetAttack()
    {
        canAttack = true;
    }
}

