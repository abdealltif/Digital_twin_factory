using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineController : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform leftSpawnPoint, rightSpawnPoint;

    [Header("Waypoints Left")]
    public Transform leftWaypoint1;
    public Transform leftWaypoint2;
    public Transform leftWaypoint3;
    public Transform leftWaypoint4;

    [Header("Waypoints Right")]
    public Transform rightWaypoint1;
    public Transform rightWaypoint2;
    public Transform rightWaypoint3;
    public Transform rightWaypoint4;

    [Header("Storage (4 positions)")]
    public Transform storagePoint1;
    public Transform storagePoint2;
    public Transform storagePoint3;
    public Transform storagePoint4;

    [Header("Settings")]
    [Range(0.1f, 5f)] public float spawnDelay = 1.5f;
    [Range(0.5f, 5f)] public float moveSpeed = 2f;

    [Header("Prefabs")]
    public GameObject pieceTypePrincipal, pieceTypeSecondaire;
    [Range(0f, 100f)] public float pourcentageTypePrincipal = 80f;

    [Header("Button & Conveyor")]
    public GameObject buttonObject, conveyorObject;
    public float conveyorSpeed = 0.5f;
    public float buttonRotationAngle = -30f;
    public float buttonRotationSpeed = 5f;

    // État du système
    private bool isRunning = false;
    private int piecesInTransit = 0;
    
    // Routes et positions
    private Transform[] leftRoute, rightRoute, storagePoints;
    
    // Animation bouton
    private Quaternion buttonUp, buttonDown;
    
    // Animation convoyeur
    private Renderer conveyorRenderer;
    private Vector2 textureOffset;

    void Start()
    {
        // Initialiser les routes
        leftRoute = new Transform[] { leftWaypoint1, leftWaypoint2, leftWaypoint3, leftWaypoint4 };
        rightRoute = new Transform[] { rightWaypoint1, rightWaypoint2, rightWaypoint3, rightWaypoint4 };
        storagePoints = new Transform[] { storagePoint1, storagePoint2, storagePoint3, storagePoint4 };

        // Initialiser le bouton
        if (buttonObject != null)
        {
            buttonUp = buttonObject.transform.rotation;
            buttonDown = buttonUp * Quaternion.Euler(buttonRotationAngle, 0, 0);
        }

        // Initialiser le convoyeur
        if (conveyorObject != null)
        {
            conveyorRenderer = conveyorObject.GetComponent<Renderer>();
            if (conveyorRenderer != null)
                textureOffset = conveyorRenderer.material.mainTextureOffset;
        }
    }

    void Update()
    {
        // Clic sur le bouton
        if (Input.GetMouseButtonDown(0) && buttonObject != null && !isRunning)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == buttonObject)
            {
                StartCycle();
            }
        }

        // Animer le bouton
        if (buttonObject != null)
        {
            Quaternion target = isRunning ? buttonDown : buttonUp;
            buttonObject.transform.rotation = Quaternion.Lerp(
                buttonObject.transform.rotation, target, Time.deltaTime * buttonRotationSpeed);
        }

        // Animer le convoyeur
        if (isRunning && conveyorRenderer != null)
        {
            textureOffset.y += Time.deltaTime * conveyorSpeed;
            conveyorRenderer.material.mainTextureOffset = textureOffset;
        }
    }

    void StartCycle()
    {
        isRunning = true;
        Debug.Log("🟢 Cycle démarré - Production de 4 pièces");
        StartCoroutine(ProductionCycle());
    }

    IEnumerator ProductionCycle()
    {
        piecesInTransit = 0;

        // Produire 4 pièces (alternance gauche/droite)
        for (int i = 0; i < 4; i++)
        {
            bool isLeft = (i % 2 == 0);
            CreatePiece(i, isLeft);
            yield return new WaitForSeconds(spawnDelay);
        }

        // Attendre que toutes les pièces arrivent
        while (piecesInTransit > 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Arrêter le système
        isRunning = false;
        Debug.Log("🔴 Cycle terminé - Convoyeur arrêté");
    }

    void CreatePiece(int slotIndex, bool isLeft)
    {
        // Choisir le type de pièce
        bool isPrincipal = Random.Range(0f, 100f) < pourcentageTypePrincipal;
        GameObject prefab = isPrincipal ? pieceTypePrincipal : pieceTypeSecondaire;
        if (prefab == null) return;

        // Spawn et route
        Transform spawnPoint = isLeft ? leftSpawnPoint : rightSpawnPoint;
        Transform[] route = isLeft ? leftRoute : rightRoute;

        // Créer la pièce
        GameObject piece = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        
        // Désactiver la physique pendant le transport
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Démarrer l'animation
        piecesInTransit++;
        StartCoroutine(MovePiece(piece, route, slotIndex));
    }

    IEnumerator MovePiece(GameObject piece, Transform[] route, int slotIndex)
    {
        if (piece == null) yield break;

        // Suivre les waypoints
        foreach (Transform waypoint in route)
        {
            if (waypoint == null || piece == null) yield break;
            yield return MoveToPosition(piece, waypoint.position);
        }

        // Aller au point de stockage
        Transform storage = storagePoints[slotIndex];
        if (storage != null && piece != null)
        {
            yield return MoveToPosition(piece, storage.position);

            // Activer la physique
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            Debug.Log($"📦 Pièce {slotIndex + 1} stockée (physique activée)");
        }

        // Une pièce de moins en transit
        piecesInTransit--;
    }

    IEnumerator MoveToPosition(GameObject obj, Vector3 target)
    {
        if (obj == null) yield break;

        float distance = Vector3.Distance(obj.transform.position, target);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        Vector3 startPos = obj.transform.position;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Afficher les positions de stockage
        Transform[] storage = new Transform[] { storagePoint1, storagePoint2, storagePoint3, storagePoint4 };
        for (int i = 0; i < storage.Length; i++)
        {
            if (storage[i] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(storage[i].position, Vector3.one * 0.5f);
                Gizmos.DrawLine(storage[i].position, storage[i].position + Vector3.up * 2f);
            }
        }

        // Afficher le bouton
        if (buttonObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(buttonObject.transform.position, 0.5f);
        }
    }
}