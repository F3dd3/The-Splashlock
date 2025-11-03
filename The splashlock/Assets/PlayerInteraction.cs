using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("UI References")]
    public Canvas actionCanvas;           // Canvas met buttons (inactief in prefab)
    public TextMeshProUGUI floatingText;  // Tekst die boven speler verschijnt

    [Header("Buttons & Messages")]
    public List<Button> actionButtons = new List<Button>(); // Sleep hier de buttons
    public List<string> messages = new List<string>();      // Tekst bij elke button

    private Camera mainCam;
    private Player playerScript;

    private void Start()
    {
        mainCam = Camera.main;

        // Verberg canvas en floating text
        if (actionCanvas != null)
            actionCanvas.gameObject.SetActive(false);

        if (floatingText != null)
            floatingText.gameObject.SetActive(false);

        playerScript = GetComponent<Player>();

        SetupButtons();
    }

    private void Update()
    {
        if (floatingText != null && floatingText.gameObject.activeSelf)
        {
            floatingText.transform.LookAt(mainCam.transform);
            floatingText.transform.Rotate(0, 180, 0); // draai zodat tekst goed staat
        }
    }

    private void OnMouseDown()
    {
        // Alleen op je eigen clone klikken
        if (playerScript != null && playerScript.ownerClientId.Value != NetworkManager.Singleton.LocalClientId)
            return;

        if (actionCanvas != null)
            actionCanvas.gameObject.SetActive(!actionCanvas.gameObject.activeSelf);
    }

    private void SetupButtons()
    {
        if (actionButtons.Count != messages.Count)
        {
            Debug.LogWarning("Aantal buttons komt niet overeen met aantal messages!");
            return;
        }

        for (int i = 0; i < actionButtons.Count; i++)
        {
            int index = i;
            actionButtons[i].onClick.RemoveAllListeners();

            actionButtons[i].onClick.AddListener(() =>
            {
                // Stuur de actie naar de server
                SendActionServerRpc(messages[index]);

                // ✅ NIEUW: sluit het canvas zodra een knop wordt geklikt
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
        StartCoroutine(HideTextAfterSeconds(3f));
    }

    private IEnumerator HideTextAfterSeconds(float time)
    {
        yield return new WaitForSeconds(time);
        if (floatingText != null)
            floatingText.gameObject.SetActive(false);
    }
}
