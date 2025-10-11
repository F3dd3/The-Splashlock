using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TemporaryRagdollTrigger : MonoBehaviour
{
    [Header("Instellingen")]
    [Tooltip("Kijkhoek in graden; alleen targets binnen deze cone ragdollen.")]
    [Range(0f, 180f)]
    public float attackAngle = 90f;

    [Tooltip("Optionele vertraging voor synchronisatie met animatie.")]
    public float triggerDelay = 0f;

    [Tooltip("Naam van de aanvalsaninatie in de Animator.")]
    public string attackAnimationName = "Attack";

    private bool ragdollEnabled = false;
    private Animator animator;
    private Transform myRoot;
    private Transform myTransform;
    private RagdollActivatorNetworked selfRagdoll;

    private readonly Dictionary<RagdollActivatorNetworked, float> targetCooldowns = new();

    void Start()
    {
        myTransform = transform;
        myRoot = transform.root;
        animator = myRoot.GetComponent<Animator>();
        selfRagdoll = myRoot.GetComponent<RagdollActivatorNetworked>();
    }

    void Update()
    {
        if (selfRagdoll != null && selfRagdoll.isRagdollActive)
        {
            ragdollEnabled = false;
            return;
        }

        // Alleen actief zolang de animatie speelt
        if (ragdollEnabled && animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(attackAnimationName) || state.normalizedTime >= 1f)
                ragdollEnabled = false;
        }

        // Cooldowns afbouwen
        var keys = new List<RagdollActivatorNetworked>(targetCooldowns.Keys);
        foreach (var key in keys)
        {
            targetCooldowns[key] -= Time.deltaTime;
            if (targetCooldowns[key] <= 0f)
                targetCooldowns.Remove(key);
        }
    }

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
        if (selfRagdoll != null)
            selfRagdoll.StartTemporaryImmunity(0.3f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!ragdollEnabled) return;
        TryEnableRagdoll(other);
    }

    private void TryEnableRagdoll(Collider col)
    {
        if (col.transform.root == myRoot) return;

        var activator = col.GetComponentInParent<RagdollActivatorNetworked>();
        if (activator == null || activator.isRagdollActive) return;
        if (targetCooldowns.ContainsKey(activator)) return;

        Vector3 dir = (activator.transform.position - myTransform.position).normalized;
        float angle = Vector3.Angle(myTransform.forward, dir);
        if (angle > attackAngle * 0.5f) return;

        activator.EnableRagdoll();
        // target cooldown = lengte van de animatie
        if (animator != null)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            targetCooldowns[activator] = state.length;
        }
        else
            targetCooldowns[activator] = 0.5f;
    }
}
