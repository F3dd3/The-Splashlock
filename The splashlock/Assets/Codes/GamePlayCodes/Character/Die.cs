using UnityEngine;

public class Die : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;
    public float checkDistance = 1f;
    public LayerMask waterLayer;

    [HideInInspector]
    public Transform respawnPoint; // ingesteld door GamePlayerSpawner

    private CharacterController controller;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        CheckWaterBelow();
    }

    private void CheckWaterBelow()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, waterLayer))
        {
            if (hit.collider.CompareTag("Water"))
                Respawn();
        }
        Debug.DrawRay(origin, Vector3.down * checkDistance, Color.blue);
    }

    private void Respawn()
    {
        if (respawnPoint == null)
        {
            GameObject startObj = GameObject.FindGameObjectWithTag("Start");
            if (startObj != null)
                respawnPoint = startObj.transform;
            else
                return;
        }

        Vector3 respawnPos = respawnPoint.position + Vector3.up * respawnHeight;

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
}
