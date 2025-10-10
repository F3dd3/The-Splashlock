using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TemporaryRagdollTrigger : MonoBehaviour
{
    [Header("Instellingen")]
    [Tooltip("Hoe lang de ragdoll trigger actief is nadat geactiveerd.")]
    public float activeDuration = 0.5f;

    [Tooltip("Kijkhoek in graden; alleen targets binnen deze cone ragdollen.")]
    [Range(0f, 180f)]
    public float attackAngle = 90f;

    [Tooltip("Optionele vertraging voor synchronisatie met animatie.")]
    public float triggerDelay = 0f;

    [Tooltip("Cooldown per target zodat RPC niet meerdere keren per frame wordt gestuurd.")]
    public float perTargetCooldown = 0.2f;

    private bool ragdollEnabled = false;
    private float timer = 0f;

    private Transform myRoot;
    private Transform myTransform;
    private RagdollActivatorNetworked selfRagdoll;
    private CharacterController characterController;

    private Dictionary<RagdollActivatorNetworked, float> targetCooldowns = new Dictionary<RagdollActivatorNetworked, float>();

    void Start()
    {
        myTransform = transform;
        myRoot = transform.root;
        selfRagdoll = myRoot.GetComponent<RagdollActivatorNetworked>();
        characterController = myRoot.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (selfRagdoll != null && selfRagdoll.isRagdollActive)
        {
            ragdollEnabled = false;
            return;
        }

        if (ragdollEnabled)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                ragdollEnabled = false;
        }

        var keys = new List<RagdollActivatorNetworked>(targetCooldowns.Keys);
        foreach (var key in keys)
        {
            targetCooldowns[key] -= Time.deltaTime;
            if (targetCooldowns[key] <= 0f)
                targetCooldowns.Remove(key);
        }
    }

    // Wordt aangeroepen door animatie event of script
    public void ActivateTrigger()
    {
        if (selfRagdoll != null && selfRagdoll.isRagdollActive) return;

        if (triggerDelay > 0f)
            StartCoroutine(ActivateAfterDelay(triggerDelay));
        else
            StartRagdollTrigger();
    }

    private IEnumerator ActivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartRagdollTrigger();
    }

    private void StartRagdollTrigger()
    {
        ragdollEnabled = true;
        timer = activeDuration;

        // Immuniteit voor jezelf tijdens de slag
        if (selfRagdoll != null)
            selfRagdoll.StartTemporaryImmunity(activeDuration);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!ragdollEnabled) return;
        TryEnableRagdoll(other);
    }

    private void TryEnableRagdoll(Collider col)
    {
        if (col.transform.root == myRoot) return;

        RagdollActivatorNetworked activator = col.GetComponentInParent<RagdollActivatorNetworked>();
        if (activator == null) return;
        if (activator.isRagdollActive) return;
        if (targetCooldowns.ContainsKey(activator)) return;

        Vector3 directionToTarget = (activator.transform.position - myTransform.position).normalized;
        float angle = Vector3.Angle(myTransform.forward, directionToTarget);
        if (angle > attackAngle * 0.5f) return;

        // ✅ forceer hit, ook tijdens lopen
        activator.EnableRagdoll();

        targetCooldowns[activator] = perTargetCooldown;
    }
}
