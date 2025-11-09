using UnityEngine;

public class UITriggerZoneHandler : MonoBehaviour
{
    public RackSystemController rackController;
    
    private void OnTriggerEnter(Collider other)
    {
        if (rackController != null)
        {
            rackController.OnPlayerEnterUIZone(other);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (rackController != null)
        {
            rackController.OnPlayerExitUIZone(other);
        }
    }
}
