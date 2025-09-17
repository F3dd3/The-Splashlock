using UnityEngine;

public class Die : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;       // Hoeveel boven "Start" punt spawnen
    public float checkDistance = 1f;       // Hoe ver de raycast onder de speler checkt
    public LayerMask waterLayer;           // Layer voor water (zorg dat je Water objecten hierin zitten)

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckWaterBelow();
    }

    void CheckWaterBelow()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f; // iets boven de voeten
        Ray ray = new Ray(origin, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, checkDistance, waterLayer))
        {
            if (hit.collider.CompareTag("Water"))
            {
                Respawn();
            }
        }

        // Debug zichtbaar maken in Scene
        Debug.DrawRay(origin, Vector3.down * checkDistance, Color.blue);
    }

    void Respawn()
    {
        GameObject startObj = GameObject.FindGameObjectWithTag("Start");
        if (startObj != null)
        {
            Vector3 respawnPos = startObj.transform.position + Vector3.up * respawnHeight;

            if (controller != null)
            {
                controller.enabled = false;
                transform.position = respawnPos;
                controller.enabled = true;
            }
            else
            {
                transform.position = respawnPos;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Geen object met tag 'Start' gevonden!");
        }
    }
}
