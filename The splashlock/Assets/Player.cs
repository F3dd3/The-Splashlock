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

    // NetworkVariable voor kleur, wordt automatisch gesynct naar nieuwe clients
    public NetworkVariable<Vector3> playerColor = new NetworkVariable<Vector3>(
        new Vector3(1f, 1f, 1f), // default wit
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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
        playerColor.OnValueChanged += OnColorChanged;

        // Zorg dat de renderer meteen juiste kleur heeft bij start
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

    public void SetReadyText(bool ready)
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(ready);
    }

    private void OnColorChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (playerRenderer != null)
            playerRenderer.material.color = new Color(newValue.x, newValue.y, newValue.z);
    }

    // Server zet de kleur van deze player
    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        playerColor.Value = colorVec;
    }
}
