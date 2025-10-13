using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(RagdollControllerSmooth))]
public class RagdollActivatorNetworked : NetworkBehaviour
{
    private RagdollControllerSmooth ragdollController;
    private CharacterController characterController;

    [Header("Scripts to disable while ragdolling")]
    public MonoBehaviour[] scriptsToToggle;

    [Header("Pushback settings")]
    public Vector3 pushBackForce = new Vector3(0, 0, -2f);

    [Header("Immunity")]
    public float ragdollImmunityDuration = 0.5f;

    [HideInInspector] public bool isRagdollActive = false;
    private bool ragdollImmune = false;

    void Start()
    {
        ragdollController = GetComponent<RagdollControllerSmooth>();
        characterController = GetComponent<CharacterController>();

        if (scriptsToToggle == null || scriptsToToggle.Length == 0)
        {
            var list = new System.Collections.Generic.List<MonoBehaviour>();
            var move = GetComponent<CharacterMovement>();
            if (move != null) list.Add(move);
            scriptsToToggle = list.ToArray();
        }

        if (characterController) characterController.enabled = true;
    }

    public void EnableRagdoll()
    {
        if (isRagdollActive || ragdollImmune) return;
        if (IsOwner)
            EnableRagdollServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void EnableRagdollServerRpc(ServerRpcParams rpcParams = default)
    {
        EnableRagdollClientRpc();
    }

    [ClientRpc]
    public void EnableRagdollClientRpc(ClientRpcParams rpcParams = default)
    {
        if (isRagdollActive || ragdollImmune) return;

        isRagdollActive = true;

        if (characterController) characterController.enabled = false;
        foreach (var s in scriptsToToggle)
            if (s != null) s.enabled = false;

        ragdollController.ActivateRagdoll();
        StartCoroutine(PushBackNextFrame());
        StartCoroutine(RestoreMovementAfterRagdoll());
    }

    private IEnumerator PushBackNextFrame()
    {
        yield return new WaitForEndOfFrame();

        // 🔹 aangepast: gebruik mainBone in plaats van hipsBone
        if (ragdollController.mainBone != null)
        {
            var rb = ragdollController.mainBone.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(transform.TransformDirection(pushBackForce), ForceMode.Impulse);
        }
    }

    private IEnumerator RestoreMovementAfterRagdoll()
    {
        yield return new WaitForSeconds(ragdollController.ragdollDuration + ragdollController.blendDuration + 0.1f);

        // 🔹 aangepast: gebruik mainBone in plaats van hipsBone
        if (ragdollController.mainBone != null)
        {
            Vector3 targetPos = ragdollController.mainBone.position;
            Vector3 safePos = targetPos;

            if (Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2f))
                safePos = groundHit.point;

            Collider[] overlaps = Physics.OverlapCapsule(safePos + Vector3.up * 0.5f, safePos + Vector3.up * 1.5f, 0.4f);
            foreach (var col in overlaps)
                if (!col.transform.IsChildOf(transform))
                    safePos += (transform.position - col.ClosestPoint(safePos)).normalized * 0.3f;

            if (characterController != null) characterController.enabled = false;
            transform.position = safePos;
            if (characterController != null) characterController.enabled = true;
        }

        foreach (var s in scriptsToToggle)
            if (s != null) s.enabled = true;

        isRagdollActive = false;
        StartTemporaryImmunity(ragdollImmunityDuration);
    }

    public void StartTemporaryImmunity(float duration)
    {
        StartCoroutine(TemporaryImmunityCoroutine(duration));
    }

    private IEnumerator TemporaryImmunityCoroutine(float duration)
    {
        ragdollImmune = true;
        yield return new WaitForSeconds(duration);
        ragdollImmune = false;
    }
}
