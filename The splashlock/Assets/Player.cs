using UnityEngine;
using TMPro;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour
{
    [Header("Visuals")]
    public Renderer playerRenderer;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI readyLabel;

    [Header("Movement")]
    public float gravity = -9.81f;
    private Vector3 velocity;
    private CharacterController controller;

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();
    }

    private void Start()
    {
        if (IsOwner && nameLabel != null)
        {
            nameLabel.text = "You";
            nameLabel.gameObject.SetActive(true);
        }
        else if (nameLabel != null)
        {
            nameLabel.gameObject.SetActive(false);
        }

        if (readyLabel != null)
            readyLabel.gameObject.SetActive(isReady.Value);

        isReady.OnValueChanged += OnReadyChanged;
    }

    private void Update()
    {
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = 0f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        SetReadyText(newValue);
    }

    public void SetReadyText(bool ready)
    {
        if (readyLabel != null)
        {
            readyLabel.gameObject.SetActive(ready);
        }
        else
        {
            Debug.LogWarning($"readyLabel niet ingesteld op {name} ({OwnerClientId})");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        if (playerRenderer != null)
            playerRenderer.material.color = color;

        ForceColorClientRpc(colorVec);
    }

    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        if (playerRenderer != null)
            playerRenderer.material.color = color;
    }
}
