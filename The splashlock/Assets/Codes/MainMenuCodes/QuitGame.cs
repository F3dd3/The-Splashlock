using UnityEngine;

public class QuitGame : MonoBehaviour
{
    // Deze functie kun je koppelen aan een UI-knop in de Inspector
    public void Quit()
    {
        Debug.Log("Game wordt afgesloten..."); // handig voor in de Editor
        Application.Quit();
    }
}