using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerAttackNetworked : NetworkBehaviour
{
    public Animator animator;
    public TemporaryRagdollTrigger attackTrigger;
    [Tooltip("Fallback cooldown als de animatie niet gevonden wordt.")]
    public float defaultAttackCooldown = 1f;

    private bool canAttack = true;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            canAttack = false;
            AttackServerRpc();
            StartCoroutine(ResetAttackWhenAnimationEnds());
        }
    }

    private IEnumerator ResetAttackWhenAnimationEnds()
    {
        float cooldown = defaultAttackCooldown;

        if (animator != null)
        {
            // Wacht even zodat de animator in de attack state komt
            yield return new WaitForEndOfFrame();

            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Attack"))
                cooldown = state.length;
        }

        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }

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

        if (attackTrigger != null)
            attackTrigger.ActivateTrigger();
    }
}
