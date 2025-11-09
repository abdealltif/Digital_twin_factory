using UnityEngine;


public class OpenWithO : MonoBehaviour
{
    [Tooltip("Objet qui va tourner")]
    public Transform objectToRotate;

    [Tooltip("Angle de rotation sur Z")]
    public float rotationAngle = 90f;

    [Tooltip("Vitesse d'ouverture")]
    public float openSpeed = 2f;

    private bool playerInZone;
    private bool isOpen;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        if (objectToRotate == null)
        {
            Debug.LogError($"Rotate non assigné !");
            enabled = false;
            return;
        }

        closedRotation = objectToRotate.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, 0, rotationAngle);
    }

    void Update()
    {
        if (playerInZone && Input.GetKeyDown(KeyCode.O))
        {
            isOpen = !isOpen;
            Debug.Log($"🚪 Porte {(isOpen ? "Ouvert" : "Fermé")}");
        }

        objectToRotate.rotation = Quaternion.Lerp(
            objectToRotate.rotation,
            isOpen ? openRotation : closedRotation,
            Time.deltaTime * openSpeed
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log($"Appuyez sur O Pour Ouvrir Porte");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }
}
