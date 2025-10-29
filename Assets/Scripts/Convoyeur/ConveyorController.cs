using System;
using UnityEngine;

public class ConveyorButtonController : MonoBehaviour
{
    [Header("Machine Reference")]
    public MachineController machine;  // La machine à contrôler
    
    [Header("Conveyor Reference")]
    public GameObject conveyorObject;  // L'objet du convoyeur (avec Renderer)
    
    [Header("Interaction Settings")]
    public float clickDistance = 10f;
    
    [Header("Animation Settings")]
    public float rotationAngle = -30f;
    public float rotationSpeed = 5f;
    
    [Header("Conveyor Settings")]
    public float conveyorSpeed = 0.5f;  // Vitesse de défilement de la texture
    
    private bool isOn = false;
    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private bool isRotating = false;
    
    // Pour le convoyeur
    private Renderer conveyorRenderer;
    private Vector2 textureOffset;
    private bool conveyorRunning = false;

    void Start()
    {
        // Rotation du bouton
        originalRotation = transform.rotation;
        targetRotation = originalRotation * Quaternion.Euler(rotationAngle, 0, 0);
        
        // Initialiser le convoyeur
        if (conveyorObject != null)
        {
            conveyorRenderer = conveyorObject.GetComponent<Renderer>();
            if (conveyorRenderer != null)
            {
                textureOffset = conveyorRenderer.material.mainTextureOffset;
            }
            else
            {
                Debug.LogError("❌ Aucun Renderer trouvé sur le convoyeur !");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Aucun objet convoyeur assigné !");
        }
    }

    void Update()
    {
        // Détection du clic souris
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, clickDistance))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    ToggleSystem();
                }
            }
        }

        // Animation de rotation du bouton
        if (isRotating)
        {
            Quaternion target = isOn ? targetRotation : originalRotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(transform.rotation, target) < 0.1f)
            {
                transform.rotation = target;
                isRotating = false;
            }
        }
        
        // Animation de la texture du convoyeur (offset Y)
        if (conveyorRunning && conveyorRenderer != null)
        {
            textureOffset.y += Time.deltaTime * conveyorSpeed;
            conveyorRenderer.material.mainTextureOffset = textureOffset;
        }
    }

    private void ToggleSystem()
    {
        isOn = !isOn;
        isRotating = true;

        if (isOn)
        {
            // ✅ Active le convoyeur
            conveyorRunning = true;
            
            // ✅ Démarre la production
            if (machine != null)
            {
                machine.StartMachine();
            }
            else
            {
                Debug.LogWarning("⚠️ Aucune machine assignée !");
            }
            
            Debug.Log("🟢 Bouton activé - Machine + Convoyeur ON");
        }
        else
        {
            // ❌ Désactive tout
            conveyorRunning = false;
            
            if (machine != null)
            {
                machine.StopMachine();
            }
            
            Debug.Log("🔴 Bouton désactivé - Machine + Convoyeur OFF");
        }
    }
    
    // ✅ Méthodes que MachineController peut appeler
    public bool IsConveyorRunning()
    {
        return conveyorRunning;
    }

    public float GetConveyorSpeed()
    {
        return conveyorRunning ? conveyorSpeed : 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}