using UnityEngine;
using Unity.Netcode;

public class PlayerLobbyAnimation : NetworkBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator niet gevonden op " + gameObject.name);
            return;
        }

        // Direct idle animatie aanzetten bij spawn
        SetIdle();
    }

    private void SetIdle()
    {
        // Zorg dat alle bewegingsbools uitstaan
        animator.SetBool("isRunning", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isAttacking", false);
        // Je kunt hier andere bools resetten die je Animator heeft
    }
}
