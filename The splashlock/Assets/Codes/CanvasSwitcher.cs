using UnityEngine;

public class CanvasSwitcher : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject canvasToOpen;
    public GameObject canvasToClose;

    // Deze functie wordt aangeroepen door de button
    public void SwitchCanvas()
    {
        if (canvasToOpen != null)
            canvasToOpen.SetActive(true);

        if (canvasToClose != null)
            canvasToClose.SetActive(false);
    }
}
