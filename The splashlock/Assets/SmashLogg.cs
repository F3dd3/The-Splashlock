using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SmashTrigger : MonoBehaviour
{
    [Header("Smash Instellingen")]
    public float smashForce = 40f; // kracht van de smash
    public bool onlyOnce = false;  // als true: slechts één keer triggeren per speler

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void ApplySmash(Collider col)
    {
        // Zoek de CharacterMovement component
        CharacterMovement player = col.GetComponent<CharacterMovement>();
        if (player == null) return;

        // Bereken de richting van de smash
        Vector3 smashDir = (col.transform.position - transform.position);
        smashDir.y = 0.5f; // iets omhoog duwen
        if (smashDir.magnitude < 0.1f)
            smashDir = transform.forward;

        smashDir.Normalize();

        // Pas kracht toe
        player.AddExternalForce(smashDir * smashForce);

        Debug.Log($"💥 SmashTrigger: {player.name} geraakt! Force = {smashForce}");

        // Optioneel: uitschakelen na eerste smash
        if (onlyOnce)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            ApplySmash(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
            ApplySmash(other);
    }
}
