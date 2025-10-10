using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class PlayerAttackNetworked : NetworkBehaviour
{
    public Animator animator;
    public float attackCooldown = 1f;
    private bool canAttack = true;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            AttackServerRpc();
            Invoke(nameof(ResetAttack), attackCooldown);
        }
    }

    void ResetAttack() => canAttack = true;

    [ServerRpc]
    void AttackServerRpc(ServerRpcParams rpcParams = default)
    {
        // Trigger animatie op alle clients
        TriggerAttackClientRpc();
    }

    [ClientRpc]
    void TriggerAttackClientRpc(ClientRpcParams rpcParams = default)
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }
}
