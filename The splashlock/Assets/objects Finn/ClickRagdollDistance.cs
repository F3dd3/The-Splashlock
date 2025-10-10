using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(RagdollActivatorNetworked))]
public class ClickRagdollDelayedNetworked : NetworkBehaviour
{
    public float maxDistance = 3f;
    public float maxAngle = 60f;
    public LayerMask targetLayer;
    public float ragdollCooldown = 0.5f;
    public float ragdollDelay = 0.2f;

    private System.Collections.Generic.Dictionary<RagdollActivatorNetworked, float> cooldowns = new();
    private Transform myTransform;

    void Start() => myTransform = transform;

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
            TryRagdollInView();

        var keys = new System.Collections.Generic.List<RagdollActivatorNetworked>(cooldowns.Keys);
        foreach (var key in keys)
        {
            cooldowns[key] -= Time.deltaTime;
            if (cooldowns[key] <= 0f)
                cooldowns.Remove(key);
        }
    }

    void TryRagdollInView()
    {
        Collider[] hits = Physics.OverlapSphere(myTransform.position, maxDistance, targetLayer);

        foreach (var hit in hits)
        {
            RagdollActivatorNetworked activator = hit.GetComponentInParent<RagdollActivatorNetworked>();
            if (activator == null || activator.gameObject == gameObject) continue;
            if (cooldowns.ContainsKey(activator)) continue;

            Vector3 directionToTarget = (activator.transform.position - myTransform.position).normalized;
            float angle = Vector3.Angle(myTransform.forward, directionToTarget);
            if (angle > maxAngle / 2f) continue;

            cooldowns[activator] = ragdollCooldown;
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
}
