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
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(newValue);
    }

    public void SetReadyText(bool ready)
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(ready);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestToggleReadyServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Player player = client.PlayerObject.GetComponent<Player>();
            if (player != null)
            {
                player.isReady.Value = !player.isReady.Value;
                player.ForceReadyClientRpc(player.isReady.Value, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
            }
        }
    }

    [ClientRpc]
    public void ForceReadyClientRpc(bool ready, ClientRpcParams clientRpcParams = default)
    {
        isReady.Value = ready;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        ApplyColor(new Color(colorVec.x, colorVec.y, colorVec.z));
        ForceColorClientRpc(colorVec);
    }

    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec, ClientRpcParams clientRpcParams = default)
    {
        ApplyColor(new Color(colorVec.x, colorVec.y, colorVec.z));
    }

    private void ApplyColor(Color color)
    {
        if (playerRenderer != null)
            playerRenderer.material.color = color;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer && playerRenderer != null)
            ApplyColor(Color.gray);
    }
}
