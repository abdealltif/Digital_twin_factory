using UnityEngine;
using System.Collections;

public class PickUpSimple : MonoBehaviour
{
    [Header("Références")]
    public Transform hand;
    public Animator playerAnimator;
    
    [Header("Paramètres")]
    public float pickupRange = 5f;
    public float doubleClickTime = 0.3f;
    public string pickupAnimationName = "PressP";
    public float pickupAnimationDuration = 1f;
    
    private GameObject heldObject;
    private Rigidbody heldRb;
    private float lastClickTime;
    private GameObject lastClickedObject;
    private bool isAnimating;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Déposer si on tient un objet
            if (heldObject != null)
            {
                Drop();
                return;
            }

            // Ne pas cliquer pendant l'animation
            if (isAnimating) return;

            // Tenter de ramasser
            TryPickup();
        }
    }

    void TryPickup()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            GameObject obj = hit.collider.gameObject;

            // Vérifier le tag
            if (obj.CompareTag("Pickupable"))
            {
                float timeSinceLastClick = Time.time - lastClickTime;

                // Double-clic détecté
                if (obj == lastClickedObject && timeSinceLastClick < doubleClickTime)
                {
                    StartCoroutine(PickupWithAnimation(obj));
                    lastClickedObject = null;
                }
                else
                {
                    // Premier clic
                    lastClickedObject = obj;
                    lastClickTime = Time.time;
                }
            }
        }
    }

    IEnumerator PickupWithAnimation(GameObject obj)
    {
        isAnimating = true;

        // Jouer l'animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(pickupAnimationName);
        }

        // Attendre la fin de l'animation
        yield return new WaitForSeconds(pickupAnimationDuration);

        // Attacher l'objet
        Pickup(obj);
        
        isAnimating = false;
    }

    void Pickup(GameObject obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        // Désactiver la physique
        if (heldRb != null)
        {
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
        }

        // Désactiver les colliders
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // Attacher à la main
        obj.transform.SetParent(hand);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    void Drop()
    {
        if (heldObject == null) return;

        // Détacher
        heldObject.transform.SetParent(null);

        // Réactiver la physique
        if (heldRb != null)
        {
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
        }

        // Réactiver les colliders
        foreach (Collider col in heldObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        heldObject = null;
        heldRb = null;
    }
}