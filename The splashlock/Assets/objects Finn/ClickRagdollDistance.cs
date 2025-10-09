using UnityEngine;
using System.Collections;

public class ClickRagdollDelayed : MonoBehaviour
{
    [Header("Instellingen")]
    public float maxDistance = 3f;             // maximale afstand om iemand te ragdollen
    public float maxAngle = 60f;               // maximale kijkhoek om iemand te raken
    public LayerMask targetLayer;              // alleen andere spelers/layer
    public Vector3 pushBackForce = new Vector3(0, 0, 5f); // kracht richting speler
    public float ragdollCooldown = 0.5f;      // minimale tijd tussen ragdoll op dezelfde speler
    public float ragdollDelay = 0.2f;         // tijd in seconden voor delay tussen klik en ragdoll

    private System.Collections.Generic.Dictionary<RagdollActivator, float> cooldowns = new System.Collections.Generic.Dictionary<RagdollActivator, float>();
    private Transform myTransform;

    void Start()
    {
        myTransform = transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryRagdollInView();
        }

        // update cooldown timers
        var keys = new System.Collections.Generic.List<RagdollActivator>(cooldowns.Keys);
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
            RagdollActivator activator = hit.GetComponentInParent<RagdollActivator>();
            if (activator == null) continue;

            // check cooldown
            if (cooldowns.ContainsKey(activator)) continue;

            // check dat het niet jezelf is
            if (activator.gameObject == gameObject) continue;

            // check of binnen kijkhoek
            Vector3 directionToTarget = (activator.transform.position - myTransform.position).normalized;
            float angle = Vector3.Angle(myTransform.forward, directionToTarget);
            if (angle > maxAngle / 2f) continue;

            // voeg toe aan cooldown
            cooldowns[activator] = ragdollCooldown;

            // start coroutine om ragdoll met delay te activeren
            StartCoroutine(DelayedRagdoll(activator, directionToTarget));

            Debug.Log("Ragdoll zal geactiveerd worden op " + activator.name + " na " + ragdollDelay + " sec");
        }
    }

    private IEnumerator DelayedRagdoll(RagdollActivator activator, Vector3 directionToTarget)
    {
        yield return new WaitForSeconds(ragdollDelay);

        // activeer ragdoll
        activator.EnableRagdoll();

        // pushback richting speler
        Rigidbody rb = activator.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 pushDir = directionToTarget;
            rb.AddForce(pushDir * pushBackForce.magnitude, ForceMode.Impulse);
        }

        Debug.Log("Ragdoll geactiveerd op " + activator.name);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        Vector3 leftDir = Quaternion.Euler(0, -maxAngle / 2f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, maxAngle / 2f, 0) * transform.forward;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * maxDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * maxDistance);
    }
}
