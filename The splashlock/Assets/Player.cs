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

    // De eigenaar van deze clone
    public NetworkVariable<ulong> ownerClientId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isHostPlayer = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<Vector3> playerColor = new NetworkVariable<Vector3>(
        new Vector3(1f, 1f, 1f),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isVisible = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isVisible.OnValueChanged += OnVisibilityChanged;
        ownerClientId.OnValueChanged += OnOwnerChanged;

        OnVisibilityChanged(!isVisible.Value, isVisible.Value);
        UpdateNameLabelLocal();
    }

    private void OnOwnerChanged(ulong oldValue, ulong newValue)
    {
        UpdateNameLabelLocal();
    }

    private void UpdateNameLabelLocal()
    {
        if (nameLabel == null) return;

        // Alleen “You” label voor de clone die bij deze client hoort
        if (NetworkManager.Singleton.LocalClientId == ownerClientId.Value)
        {
            nameLabel.text = "You";
            nameLabel.gameObject.SetActive(true);
        }
        else
        {
            // Laat label leeg voor andere clones
            nameLabel.text = "";
            nameLabel.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(isReady.Value);

        isReady.OnValueChanged += OnReadyChanged;
        playerColor.OnValueChanged += OnColorChanged;

        OnColorChanged(Vector3.zero, playerColor.Value);
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

    private void OnColorChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (playerRenderer != null)
            playerRenderer.material.color = new Color(newValue.x, newValue.y, newValue.z);
    }

    private void OnVisibilityChanged(bool oldValue, bool newValue)
    {
        gameObject.SetActive(newValue);
    }

    // ✅ Hier kan de client zijn eigen ready aanzetten
    public void SetReadyText(bool ready)
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(ready);

        if (IsServer)
            isReady.Value = ready;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        playerColor.Value = colorVec;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetVisibilityServerRpc(bool visible)
    {
        isVisible.Value = visible;
    }
}
