using UnityEngine;
using TMPro;

public class MachineKPI_UI : MonoBehaviour
{
    [Header("Références")]
    public GameObject kpiPanelPrefab; // Ton prefab KPI_Panel
    public float panelHeight = 2f;    // Hauteur au-dessus de la machine

    private GameObject panelInstance;
    private TMP_Text productText;
    private TMP_Text temperatureText;
    private TMP_Text trsText;
    private TMP_Text statusText;

    private MachineData machineData;
    private bool playerInside = false;

    void Awake()
    {
        // Récupérer MachineData
        machineData = GetComponent<MachineData>();
        if(machineData == null)
        {
            Debug.LogError("MachineData manquant sur " + gameObject.name);
        }

        // Instancier le panel
        if(kpiPanelPrefab != null)
        {
            panelInstance = Instantiate(kpiPanelPrefab);
            panelInstance.SetActive(false); // invisible au départ
            Debug.Log("Panel instancié avec succès !");

            // Récupérer les textes TMP
            productText = panelInstance.transform.Find("ProductText").GetComponent<TMP_Text>();
            temperatureText = panelInstance.transform.Find("TemperatureText").GetComponent<TMP_Text>();
            trsText = panelInstance.transform.Find("TRSText").GetComponent<TMP_Text>();
            statusText = panelInstance.transform.Find("StatusText").GetComponent<TMP_Text>();

            if(productText == null || temperatureText == null || trsText == null || statusText == null)
            {
                Debug.LogError("Un ou plusieurs TextMeshPro non trouvés dans le panel.");
            }
        }
        else
        {
            Debug.LogError("Kpi Panel Prefab n'est pas assigné dans l'inspector !");
        }
    }

    void Update()
    {
        if(playerInside && panelInstance != null && machineData != null)
        {
            // Mettre à jour les textes
            productText.text = $"Product: {machineData.product}";
            temperatureText.text = $"Temperature: {machineData.temperatureC:0.##} C";
            trsText.text = $"TRS: {machineData.trsPercent:0}%";
            statusText.text = $"Status: {(machineData.isRunning ? "En marche" : "Arrêt")}";

            // Positionner le panel au-dessus de la machine
            panelInstance.transform.position = transform.position + new Vector3(0, panelHeight, 0);

            // Faire regarder le panel vers le joueur
            GameObject player = GameObject.Find("PlayerArmature");
            if(player != null)
            {
                Vector3 lookPos = player.transform.position - panelInstance.transform.position;
                lookPos.y = 0; // bloquer rotation verticale
                panelInstance.transform.rotation = Quaternion.LookRotation(lookPos);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.gameObject.name); // Vérifier le trigger
        if(other.gameObject.name == "PlayerArmature")
        {
            playerInside = true;
            if(panelInstance != null)
            {
                panelInstance.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger Exit: " + other.gameObject.name);
        if(other.gameObject.name == "PlayerArmature")
        {
            playerInside = false;
            if(panelInstance != null)
            {
                panelInstance.SetActive(false);
            }
        }
    }
}
