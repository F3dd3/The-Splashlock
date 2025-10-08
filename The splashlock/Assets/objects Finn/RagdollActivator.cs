using UnityEngine;
using System.Collections;

public class RagdollActivator : MonoBehaviour
{
    private RagdollControllerSmooth ragdollController;
    private CharacterController characterController;
    private Rigidbody mainRigidbody;
    private MonoBehaviour[] movementScripts;
    private bool isRagdollActive = false;

    [Header("Instellingen pushback")]
    public Vector3 pushBackForce = new Vector3(0, 0, -2f); // Z = naar achter, pas aan naar wens

    void Start()
    {
        ragdollController = GetComponent<RagdollControllerSmooth>();
        if (ragdollController == null)
        {
            Debug.LogError("Geen RagdollControllerSmooth gevonden!");
            return;
        }

        characterController = GetComponent<CharacterController>();
        mainRigidbody = GetComponent<Rigidbody>();
        movementScripts = GetComponents<MonoBehaviour>();
    }

    /// <summary>
    /// Activeer ragdoll en pushback
    /// </summary>
    public void EnableRagdoll()
    {
        if (isRagdollActive) return;
        isRagdollActive = true;

        // 1️⃣ Zet movement scripts en controller uit
        if (characterController) characterController.enabled = false;
        if (mainRigidbody)
        {
            mainRigidbody.isKinematic = true;
            mainRigidbody.detectCollisions = false;
        }

        foreach (var script in movementScripts)
        {
            if (script != this)
                script.enabled = false;
        }

        // 2️⃣ Activeer ragdoll
        ragdollController.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);

        // 3️⃣ Pushback in volgende frame toepassen
        StartCoroutine(PushBackNextFrame());

        // 4️⃣ Start coroutine om movement weer te herstellen na ragdoll + blend
        StartCoroutine(RestoreMovementAfterRagdoll());
    }

    /// <summary>
    /// Pushback naar achter op hips rigidbody
    /// </summary>
    private IEnumerator PushBackNextFrame()
    {
        yield return new WaitForEndOfFrame(); // wacht tot physics is geactiveerd

        if (ragdollController.hipsBone != null)
        {
            Rigidbody hipsRb = ragdollController.hipsBone.GetComponent<Rigidbody>();
            if (hipsRb != null)
            {
                hipsRb.AddForce(transform.TransformDirection(pushBackForce), ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// Herstel movement na ragdoll en blend
    /// </summary>
    private IEnumerator RestoreMovementAfterRagdoll()
    {
        yield return new WaitForSeconds(ragdollController.ragdollDuration + ragdollController.blendDuration + 0.1f);

        // Zet movement scripts en controller weer aan
        if (characterController) characterController.enabled = true;
        if (mainRigidbody)
        {
            mainRigidbody.isKinematic = false;
            mainRigidbody.detectCollisions = true;
        }

        foreach (var script in movementScripts)
        {
            if (script != this)
                script.enabled = true;
        }

        isRagdollActive = false;
    }
}
