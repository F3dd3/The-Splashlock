using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(RagdollActivatorNetworked))]
public class ClickRagdollDelayedNetworked : NetworkBehaviour
{
    [Header("Detectie instellingen")]
    public float maxDistance = 3f;
    public float maxAngle = 60f;
    public LayerMask targetLayer; // Alleen spelerslaag

    [Header("Tijdinstellingen")]
    public float ragdollCooldown = 0.5f;
    public float ragdollDelay = 0.2f;
    public float autoCheckRate = 0.1f; // Tijd tussen automatische checks

    private readonly System.Collections.Generic.Dictionary<RagdollActivatorNetworked, float> cooldowns = new();
    private Transform myTransform;
    private RagdollActivatorNetworked selfRagdoll;
    private Coroutine autoAttackCoroutine;

    void Start()
    {
        myTransform = transform;
        selfRagdoll = GetComponent<RagdollActivatorNetworked>();
    }

    void Update()
    {
        if (!IsOwner) return;

        // Start auto-attack bij eerste klik
        if (Input.GetMouseButtonDown(0) && autoAttackCoroutine == null)
        {
            autoAttackCoroutine = StartCoroutine(AutoAttackLoop());
        }

        // Update cooldowns
        if (cooldowns.Count > 0)
        {
            var keys = new System.Collections.Generic.List<RagdollActivatorNetworked>(cooldowns.Keys);
            foreach (var key in keys)
            {
                cooldowns[key] -= Time.deltaTime;
                if (cooldowns[key] <= 0f)
                    cooldowns.Remove(key);
            }
        }
    }

    private IEnumerator AutoAttackLoop()
    {
        while (true)
        {
            TryRagdollInView();
            yield return new WaitForSeconds(autoCheckRate);

            // Stop als je zelf ragdolled
            if (selfRagdoll != null && selfRagdoll.isRagdollActive)
            {
                autoAttackCoroutine = null;
                yield break;
            }
        }
    }

    void TryRagdollInView()
    {
        Collider[] hits = Physics.OverlapSphere(myTransform.position, maxDistance, targetLayer);

        foreach (var hit in hits)
        {
            RagdollActivatorNetworked activator = hit.GetComponentInParent<RagdollActivatorNetworked>();
            if (activator == null) continue;

            // negeer jezelf
            if (activator.transform.root == transform.root) continue;

            // cooldown check
            if (cooldowns.ContainsKey(activator)) continue;

            // kijkhoek check
            Vector3 directionToTarget = (activator.transform.position - myTransform.position).normalized;
            float angle = Vector3.Angle(myTransform.forward, directionToTarget);
            if (angle > maxAngle * 0.5f) continue;

            // voeg target toe aan cooldown
            cooldowns[activator] = ragdollCooldown;

            // start ragdoll bij target
            StartCoroutine(DelayedRagdoll(activator));
        }
    }

    private IEnumerator DelayedRagdoll(RagdollActivatorNetworked activator)
    {
        yield return new WaitForSeconds(ragdollDelay);

        if (!IsOwner) yield break;

        ActivateRagdollOnTargetServerRpc(activator.NetworkObjectId);
    }

    [ServerRpc]
    private void ActivateRagdollOnTargetServerRpc(ulong targetNetworkId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject target))
        {
            var activator = target.GetComponent<RagdollActivatorNetworked>();
            if (activator != null)
                activator.EnableRagdollClientRpc();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
