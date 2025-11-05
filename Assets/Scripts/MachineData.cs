using UnityEngine;

public class MachineData : MonoBehaviour
{
    [Header("KPI")]
    public string product = "225P/H";
    public float temperatureC = 50f;
    [Range(0f,100f)] public float trsPercent = 80f;
    public bool isRunning = false; // true = en marche, false = arrêt
}
