using UnityEngine;
using System.Collections;

public class PickUpSimple : MonoBehaviour
{
    [Header("Références")]
    public Transform hand;
    public Animator playerAnimator;
    
    [Header("Paramètres")]
    public float pickupRange = 5f;
    public float dropRange = 5f;
    public float doubleClickTime = 0.3f;
    public string pickupAnimationName = "PressP";
    public float pickupAnimationDuration = 1f;
    
    private GameObject heldObject;
    private Rigidbody heldRb;
    private float lastClickTime;
    private GameObject lastClickedObject;
    private bool isAnimating;
    private ChariotController sourceChariot; // 🆕 Mémorise le chariot d'origine

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Si on tient un objet, essayer de le déposer
            if (heldObject != null)
            {
                TryDrop();
                return;
            }
            
            // Ne pas cliquer pendant l'animation
            if (isAnimating) return;
            
            // Tenter de ramasser (du sol OU du trolley)
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
            
            // 🆕 NOUVEAU : Vérifier si on clique sur une pièce sur un trolley
            ChariotController chariot = hit.collider.GetComponent<ChariotController>();
            if (chariot != null)
            {
                TryPickupFromTrolley(chariot);
                return;
            }
            
            // Ramassage classique du sol
            if (obj.CompareTag("Pickupable"))
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                
                // Double-clic détecté
                if (obj == lastClickedObject && timeSinceLastClick < doubleClickTime)
                {
                    StartCoroutine(PickupWithAnimation(obj, null));
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

    /// <summary>
    /// 🆕 Ramasse une pièce depuis le trolley
    /// </summary>
    void TryPickupFromTrolley(ChariotController chariot)
    {
        if (chariot.IsEmpty())
        {
            Debug.Log("⚠️ Le chariot est vide");
            return;
        }

        float timeSinceLastClick = Time.time - lastClickTime;
        
        // Double-clic sur le trolley = prendre la dernière pièce chargée
        if (lastClickedObject == chariot.gameObject && timeSinceLastClick < doubleClickTime)
        {
            GameObject piece = chariot.GetLastLoadedPiece();
            
            if (piece != null)
            {
                StartCoroutine(PickupWithAnimation(piece, chariot));
                lastClickedObject = null;
            }
        }
        else
        {
            // Premier clic sur le trolley
            lastClickedObject = chariot.gameObject;
            lastClickTime = Time.time;
            Debug.Log("💡 Double-cliquez pour prendre une pièce du chariot");
        }
    }

    IEnumerator PickupWithAnimation(GameObject obj, ChariotController fromChariot)
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
        Pickup(obj, fromChariot);
        
        isAnimating = false;
    }

    void Pickup(GameObject obj, ChariotController fromChariot)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();
        sourceChariot = fromChariot; // 🆕 Mémoriser la source
        
        // 🆕 Si ramassé depuis un trolley, le retirer proprement
        if (fromChariot != null)
        {
            fromChariot.RemovePiece(obj);
        }
        
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
        
        string source = fromChariot != null ? "du chariot" : "du sol";
        Debug.Log($"✅ Pièce ramassée {source}: {obj.name}");
    }

    /// <summary>
    /// 🆕 Gère le dépôt : sur trolley OU au sol
    /// </summary>
    void TryDrop()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Chercher un trolley sous le curseur
        if (Physics.Raycast(ray, out hit, dropRange))
        {
            ChariotController trolley = hit.collider.GetComponent<ChariotController>();
            
            if (trolley != null)
            {
                // Déposer sur le trolley
                bool loaded = trolley.TryLoadPieceFromPlayer(heldObject);
                
                if (loaded)
                {
                    Debug.Log($"📦 Pièce déposée sur le trolley!");
                    heldObject = null;
                    heldRb = null;
                    sourceChariot = null;
                    return;
                }
            }
        }
        
        // Sinon, déposer au sol
        DropToGround();
    }

    /// <summary>
    /// Dépose l'objet au sol
    /// </summary>
    void DropToGround()
    {
        if (heldObject == null) return;
        
        // Détacher
        heldObject.transform.SetParent(null);
        
        // Placer devant le joueur
        Vector3 dropPosition = transform.position + transform.forward * 1.5f;
        heldObject.transform.position = dropPosition;
        
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
        
        Debug.Log($"📤 Pièce déposée au sol: {heldObject.name}");
        
        heldObject = null;
        heldRb = null;
        sourceChariot = null;
    }

    /// <summary>
    /// Permet au ChariotController de vérifier si le joueur tient quelque chose
    /// </summary>
    public GameObject GetHeldObject()
    {
        return heldObject;
    }

    /// <summary>
    /// Libère l'objet tenu sans le rendre physique
    /// </summary>
    public void ReleaseHeldObject()
    {
        heldObject = null;
        heldRb = null;
        sourceChariot = null;
    }
}