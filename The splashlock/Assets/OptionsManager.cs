using UnityEngine;
using UnityEngine.UI;

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

    // ----------------- Update loop -----------------
    private void Update()
    {
        if (spawnedOptionsPlayer == null) return;

        UpdateOptionsPlayerColor();
    }

    // Haal kleur van eigen LobbyPlayer en zet op OptionsPlayer
    private void UpdateOptionsPlayerColor()
    {
        Player lobbyPlayer = null;
        ulong clientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

        if (PlayerSpawner.Instance != null && PlayerSpawner.Instance.playerRefs.TryGetValue(clientId, out Player lp))
        {
            lobbyPlayer = lp;
        }

        if (lobbyPlayer != null)
        {
            Renderer optionsRenderer = spawnedOptionsPlayer.GetComponentInChildren<Renderer>();
            if (optionsRenderer != null)
            {
                if (optionsRenderer.material.color != lobbyPlayer.playerRenderer.material.color)
                {
                    optionsRenderer.material.color = lobbyPlayer.playerRenderer.material.color;
                }
            }
        }
    }
}
