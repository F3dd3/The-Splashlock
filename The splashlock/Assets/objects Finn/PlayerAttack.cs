using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class PlayerAttackNetworked : NetworkBehaviour
{
    public Animator animator;
    public TemporaryRagdollTrigger attackTrigger;
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
        TriggerAttackClientRpc();
    }

    [ClientRpc]
    void TriggerAttackClientRpc(ClientRpcParams rpcParams = default)
    {
        if (animator != null)
            animator.SetTrigger("Attack");

        // ✅ direct triggeren (werkt ook tijdens lopen)
        if (attackTrigger != null)
            attackTrigger.ActivateTrigger();
    }
}
