using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class OptionsManager : MonoBehaviour
{
    [Header("Prefabs & Camera")]
    public GameObject optionsPlayerPrefab;
    public Canvas lobbyCanvas;

    [Header("Options UI Canvas")]
    public Canvas optionsCanvas; // Canvas met exit knop
    public Button exitButton;    // Sleep hier de exit-knop in

    [Header("Spawn Points")]
    public Transform[] optionsSpawnPoints;

    [Header("Lobby Main Camera")]
    public Camera lobbyMainCamera; // Sleep hier de main lobby camera in

    private GameObject spawnedOptionsPlayer;

    private void Start()
    {
        // Lobby canvas initieel aan
        if (lobbyCanvas != null)
            lobbyCanvas.gameObject.SetActive(true);

        // Options canvas initieel uit
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        // Exit knop listener
        if (exitButton != null)
            exitButton.onClick.AddListener(CloseOptions);
    }

    // ---------------- PUBLIC METHODS ----------------

    public void OpenOptions()
    {
        if (spawnedOptionsPlayer != null) return;

        // Lobby canvas uit
        if (lobbyCanvas != null)
            lobbyCanvas.gameObject.SetActive(false);

        // Options canvas aan
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(true);

        // Lobby main camera uit
        if (lobbyMainCamera != null)
            lobbyMainCamera.enabled = false;

        SpawnOptionsPlayer();
    }

    public void CloseOptions()
    {
        DespawnOptionsPlayer();

        // Lobby canvas weer aan
        if (lobbyCanvas != null)
            lobbyCanvas.gameObject.SetActive(true);

        // Options canvas uit
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        // Lobby main camera weer aan
        if (lobbyMainCamera != null)
            lobbyMainCamera.enabled = true;
    }

    // ---------------- PRIVATE METHODS ----------------

    private void SpawnOptionsPlayer()
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (optionsSpawnPoints != null && optionsSpawnPoints.Length > 0)
        {
            int index = Random.Range(0, optionsSpawnPoints.Length);
            spawnPos = optionsSpawnPoints[index].position;
            spawnRot = optionsSpawnPoints[index].rotation;
        }

        spawnedOptionsPlayer = Instantiate(optionsPlayerPrefab, spawnPos, spawnRot);

        // Zet camera van prefab aan
        Camera optionsCamera = spawnedOptionsPlayer.GetComponentInChildren<Camera>(true);
        if (optionsCamera != null)
            optionsCamera.enabled = true;

        // Initialiseer kleur bij spawn
        UpdateOptionsPlayerColor();
    }

    private void DespawnOptionsPlayer()
    {
        if (spawnedOptionsPlayer != null)
        {
            Camera optionsCamera = spawnedOptionsPlayer.GetComponentInChildren<Camera>(true);
            if (optionsCamera != null)
                optionsCamera.enabled = false;

            Destroy(spawnedOptionsPlayer);
        }

        spawnedOptionsPlayer = null;
    }

    private void Update()
    {
        if (spawnedOptionsPlayer == null) return;

        UpdateOptionsPlayerColor();
    }

    /// <summary>
    /// Zoekt de juiste lobby Player-clone die bij deze client hoort
    /// en kopieert diens kleur naar de Options-player.
    /// </summary>
    private void UpdateOptionsPlayerColor()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient)
            return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        Player lobbyPlayer = null;

        // Zoek de player die aan deze clientId is toegewezen (ongeacht IsOwner)
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player.ownerClientId.Value == clientId)
            {
                lobbyPlayer = player;
                break;
            }
        }

        Renderer optionsRenderer = spawnedOptionsPlayer?.GetComponentInChildren<Renderer>();
        if (optionsRenderer == null) return;

        if (lobbyPlayer != null && lobbyPlayer.playerRenderer != null)
        {
            // Normale client: neem kleur van toegewezen clone
            Color targetColor = lobbyPlayer.playerRenderer.material.color;
            if (optionsRenderer.material.color != targetColor)
                optionsRenderer.material.color = targetColor;
        }
        else
        {
            // Host zonder eigen clone → toon neutrale kleur (bijv. wit of grijs)
            optionsRenderer.material.color = Color.gray;
        }
    }
}
