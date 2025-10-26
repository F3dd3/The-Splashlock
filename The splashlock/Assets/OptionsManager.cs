using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Prefabs & Camera")]
    public GameObject optionsPlayerPrefab;
    public Canvas lobbyCanvas;

    [Header("Spawn Points")]
    public Transform[] optionsSpawnPoints;

    [Header("UI Buttons")]
    public Button exitButton; // De knop die terug gaat naar lobby

    private GameObject spawnedOptionsPlayer;

    private void Start()
    {
        // Zorg dat exit knop initieel onzichtbaar is
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
            exitButton.onClick.AddListener(CloseOptions);
        }
    }

    // ---------------- PUBLIC METHODS ----------------

    // Alleen openen van options (Controls knop)
    public void OpenOptions()
    {
        if (spawnedOptionsPlayer != null) return;

        // Lobby canvas uit
        if (lobbyCanvas != null)
            lobbyCanvas.gameObject.SetActive(false);

        SpawnOptionsPlayer();

        // Exit knop zichtbaar maken
        if (exitButton != null)
            exitButton.gameObject.SetActive(true);
    }

    // Sluit options en gaat terug naar lobby
    public void CloseOptions()
    {
        DespawnOptionsPlayer();

        // Lobby canvas weer aan
        if (lobbyCanvas != null)
            lobbyCanvas.gameObject.SetActive(true);

        // Exit knop weer verbergen
        if (exitButton != null)
            exitButton.gameObject.SetActive(false);
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
}
