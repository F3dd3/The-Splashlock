using UnityEngine;
using Unity.Netcode;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour
{
    [Header("Visuals")]
    public Renderer playerRenderer;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI readyLabel;

    [Header("Movement")]
    public float gravity = -9.81f; // valkracht
    private Vector3 velocity;
    private CharacterController controller;

    // ---------------- READY STATUS ----------------
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();
    }

    private void Start()
    {
        // "You" label
        if (IsOwner && nameLabel != null)
        {
            nameLabel.text = "You";
            nameLabel.gameObject.SetActive(true);
        }
        else if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(false);
        }

        // Ready tekst initieel uit
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(isReady.Value);

        // Subscribe op NetworkVariable verandering
        isReady.OnValueChanged += OnReadyChanged;
    }

    private void Update()
    {
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = 0f; // reset verticale snelheid bij landing
        }

        // gravity toepassen
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(newValue);
    }

    // ---------------- LOKALE FUNCTIE VOOR READY ----------------
    public void ToggleReady()
    {
        if (!IsOwner) return;
        isReady.Value = !isReady.Value; // Netcode synchronisatie naar iedereen
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        playerRenderer.material.color = color;
        ForceColorClientRpc(colorVec);
    }

    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec, ClientRpcParams clientRpcParams = default)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        playerRenderer.material.color = color;
    }
}
