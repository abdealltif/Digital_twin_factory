using System;
using UnityEngine;

public class ConveyorButtonController : MonoBehaviour
{
    [Header("Conveyor")]
    public GameObject conveyorObject;
    public float conveyorSpeed = 0.5f;
    
    [Header("Button Animation")]
    public float rotationAngle = -30f;
    public float rotationSpeed = 5f;
    
    [Header("Interaction")]
    public float clickDistance = 10f;
    
    private bool isOn;
    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private Renderer conveyorRenderer;
    private Vector2 textureOffset;

    void Start()
    {
        originalRotation = transform.rotation;
        targetRotation = originalRotation * Quaternion.Euler(rotationAngle, 0, 0);
        
        if (conveyorObject != null)
        {
            conveyorRenderer = conveyorObject.GetComponent<Renderer>();
            if (conveyorRenderer != null)
            {
                textureOffset = conveyorRenderer.material.mainTextureOffset;
            }
        }
    }

    void Update()
    {
        // Détection clic
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, clickDistance) && hit.collider.gameObject == gameObject)
            {
                Toggle();
            }
        }

        // Animation bouton
        transform.rotation = Quaternion.Lerp(transform.rotation, isOn ? targetRotation : originalRotation, Time.deltaTime * rotationSpeed);
        
        // Animation convoyeur
        if (isOn && conveyorRenderer != null)
        {
            textureOffset.y += Time.deltaTime * conveyorSpeed;
            conveyorRenderer.material.mainTextureOffset = textureOffset;
        }
    }

    void Toggle()
    {
        isOn = !isOn;
        Debug.Log(isOn ? "🟢 Convoyeur ON" : "🔴 Convoyeur OFF");
    }

    public bool IsConveyorRunning() => isOn;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}