using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Prefabs & Canvas")]
    public GameObject optionsPlayerPrefab;
    public Canvas lobbyCanvas;
    public Canvas optionsCanvas; // Het volledige options UI canvas

    [Header("Spawn Points")]
    public Transform[] optionsSpawnPoints;

    private GameObject spawnedOptionsPlayer;

    private Button exitButton; // Wordt automatisch gevonden in het canvas

    private void Start()
    {
        // Zorg dat options canvas initieel uit staat
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);

        // Zoek automatisch de exit knop binnen het options canvas
        if (optionsCanvas != null)
        {
            exitButton = optionsCanvas.GetComponentInChildren<Button>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(CloseOptions);
            }
            else
            {
                Debug.LogWarning("Exit button niet gevonden in optionsCanvas!");
            }
        }
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
