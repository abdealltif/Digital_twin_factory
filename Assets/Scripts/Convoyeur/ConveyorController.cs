using System;
using UnityEngine;

public class ConveyorButtonController : MonoBehaviour
{
    [Header("Machine Reference")]
    public MachineController machine;  // ✅ La machine à contrôler
    
    [Header("Conveyor Reference")]
    public Conveyor_Movement conveyor;
    
    [Header("Interaction Settings")]
    public float clickDistance = 10f;
    
    [Header("Animation Settings")]
    public float rotationAngle = 30f;  // ✅ 30° sur X
    public float rotationSpeed = 5f;
    
    private bool isOn = false;
    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private bool isRotating = false;

    void Start()
    {
        originalRotation = transform.rotation;
        // ✅ Rotation sur l'axe X
        targetRotation = originalRotation * Quaternion.Euler(rotationAngle, 0, 0);
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

        // Animation de rotation
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
    }

    private void ToggleSystem()
    {
        isOn = !isOn;
        isRotating = true;

        if (isOn)
        {
            // ✅ Active le convoyeur
            if (conveyor != null)
                conveyor.TurnOn();
            
            // ✅ Démarre la production
            if (machine != null)
                machine.StartMachine();
            
            Debug.Log("🟢 Bouton activé - Machine + Convoyeur ON");
        }
        else
        {
            // ❌ Désactive tout
            if (conveyor != null)
                conveyor.TurnOff();
            
            if (machine != null)
                machine.StopMachine();
            
            Debug.Log("🔴 Bouton désactivé - Machine + Convoyeur OFF");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}

public class Conveyor_Movement
{
    internal void TurnOff()
    {
        throw new NotImplementedException();
    }

    internal void TurnOn()
    {
        throw new NotImplementedException();
    }
}