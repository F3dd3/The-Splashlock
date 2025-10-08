using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterMovement))]
public class Shroom : MonoBehaviour
{
    [Header("Trampoline Settings")]
    public float trampolineForce = 10f;
    public float raycastLength = 0.6f;

    private CharacterMovement characterMovement;
    private CharacterController controller;

    void Start()
    {
        characterMovement = GetComponent<CharacterMovement>();
        controller = GetComponent<CharacterController>();

        if (characterMovement == null || controller == null)
            Debug.LogError("ShroomJumpBoost vereist CharacterMovement + CharacterController!");
    }

    void Update()
    {
        if (controller == null || characterMovement == null || !characterMovement.IsOwner) return;

        Vector3 origin = transform.position + Vector3.up * controller.radius;

        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit, raycastLength))
        {
            if (hit.collider.CompareTag("shroom") && hit.normal.y > 0.7f)
            {
                float bounceVelocity = Mathf.Sqrt(trampolineForce * -2f * characterMovement.gravity);
                characterMovement.SetVerticalVelocity(bounceVelocity);
            }
        }

        Debug.DrawRay(origin, Vector3.down * raycastLength, Color.cyan);
    }
}
