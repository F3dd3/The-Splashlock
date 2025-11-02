using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("UI References")]
    public Canvas actionCanvas;           // Canvas met knoppen, sleep hier prefab of scene canvas
    public TextMeshProUGUI floatingText;  // Tekst die boven speler verschijnt

    [Header("Buttons & Messages")]
    public List<Button> actionButtons = new List<Button>(); // Sleep hier de buttons
    public List<string> messages = new List<string>();      // Tekst die verschijnt bij elke button

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        // Verberg tekst en canvas bij start
        if (floatingText != null)
            floatingText.gameObject.SetActive(false);

        if (actionCanvas != null)
            actionCanvas.gameObject.SetActive(false);

        SetupButtons();
    }

    private void Update()
    {
        if (floatingText != null && floatingText.gameObject.activeSelf)
        {
            // Zorg dat tekst altijd naar camera kijkt
            floatingText.transform.rotation = Quaternion.LookRotation(floatingText.transform.position - mainCam.transform.position);
        }
    }

    private void OnMouseDown()
    {
        if (!IsOwner) return; // Alleen lokale speler mag eigen canvas openen

        if (actionCanvas != null)
        {
            actionCanvas.gameObject.SetActive(!actionCanvas.gameObject.activeSelf);
        }
    }

    /// <summary>
    /// Zet de button-clicks op zodat elk button-element exact de corresponderende message-element toont
    /// </summary>
    private void SetupButtons()
    {
        if (actionButtons.Count != messages.Count)
        {
            Debug.LogWarning("Aantal buttons komt niet overeen met aantal messages!");
            return;
        }

        for (int i = 0; i < actionButtons.Count; i++)
        {
            int index = i; // Lokale kopie voor lambda

            // Verwijder bestaande listeners voor veiligheid
            actionButtons[i].onClick.RemoveAllListeners();

            actionButtons[i].onClick.AddListener(() =>
            {
                // Stuur message naar server
                SendActionServerRpc(messages[index]);

                // Sluit canvas na klik
                if (actionCanvas != null)
                    actionCanvas.gameObject.SetActive(false);
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendActionServerRpc(string message)
    {
        ShowFloatingTextClientRpc(message);
    }

    [ClientRpc]
    private void ShowFloatingTextClientRpc(string message)
    {
        if (floatingText == null) return;

        floatingText.text = message;
        floatingText.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(HideTextAfterSeconds(3f)); // 3 seconden zichtbaar
    }

    private IEnumerator HideTextAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);
        if (floatingText != null)
            floatingText.gameObject.SetActive(false);
    }
}
