using UnityEngine;

public class Shroom : MonoBehaviour
{
    [Header("Trampoline Settings")]
    public float trampolineForce = 10f;      // Hoe hoog je stuitert
    public float raycastLength = 0.6f;       // Hoe ver onder de speler we checken

    private CharacterMovement characterMovement;
    private CharacterController controller;

    void Start()
    {
        characterMovement = GetComponent<CharacterMovement>();
        controller = GetComponent<CharacterController>();

        if (characterMovement == null || controller == null)
        {
            Debug.LogError("⚠️ ShroomJumpBoost vereist CharacterMovement + CharacterController op speler!");
        }
    }

    void Update()
    {
        if (controller == null) return;

        // Startpositie net onder het midden van de capsule
        Vector3 origin = transform.position + Vector3.up * (controller.radius);

        // SphereCast met dezelfde radius als de CharacterController
        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit, raycastLength))
        {
            if (hit.collider.CompareTag("shroom"))
            {
                // Alleen triggeren als speler daadwerkelijk bovenop staat
                if (hit.normal.y > 0.7f)
                {
                    float bounceVelocity = Mathf.Sqrt(trampolineForce * -2f * characterMovement.gravity);
                    characterMovement.SetVerticalVelocity(bounceVelocity);
                }
            }
        }

        // Debug zodat je in Scene View de SphereCast ziet
        Debug.DrawRay(origin, Vector3.down * raycastLength, Color.cyan);
    }
}
