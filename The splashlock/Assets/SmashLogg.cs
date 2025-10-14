using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SmashTrigger : MonoBehaviour
{
    public float smashForce = 40f; // kracht van de smash

    private void OnTriggerEnter(Collider other)
    {
        CharacterMovement_Local player = other.GetComponent<CharacterMovement_Local>();
        if (player != null)
        {
            Vector3 smashDir = (other.transform.position - transform.position).normalized;
            smashDir.y = 0.5f; // kleine lift
            player.AddExternalForce(smashDir * smashForce);
            Debug.Log("SMASH! Speler direct geraakt door rollende log!");
        }
    }
}
