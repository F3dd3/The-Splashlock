using UnityEngine;

public class TimeScaleReset : MonoBehaviour
{
    void Awake()
    {
        Time.timeScale = 1f; // altijd terug naar normaal
    }
}