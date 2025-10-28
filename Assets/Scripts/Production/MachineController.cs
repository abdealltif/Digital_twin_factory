using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MachineController : MonoBehaviour
{
    [Header("Points de spawn")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    [Range(0.1f, 5f)]
    public float spawnDelay = 1.5f;
    [Range(0.5f, 5f)]
    public float moveSpeed = 2f;

    [Header("Route GAUCHE (4 waypoints)")]
    public Transform leftWaypoint1;
    public Transform leftWaypoint2;
    public Transform leftWaypoint3;
    public Transform leftWaypoint4;

    [Header("Route DROITE (4 waypoints)")]
    public Transform rightWaypoint1;
    public Transform rightWaypoint2;
    public Transform rightWaypoint3;
    public Transform rightWaypoint4;

    [Header("Stockage (4 positions)")]
    public Transform storagePoint1;
    public Transform storagePoint2;
    public Transform storagePoint3;
    public Transform storagePoint4;

    [Header("Prefab")]
    public GameObject piecePrefab;

    [Header("Contrôle externe")]
    public ConveyorController conveyorButton;

    [Header("Méthode de déplacement")]
    public MovementMethod movementMethod = MovementMethod.SmoothFollow;
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    public bool alignToPath = true;
    
    [Header("Physique au stockage")]
    public bool enableGravityAtStorage = true;  // ✅ Activer la chute au stockage
    public float dropHeight = 0.5f;  // Hauteur de chute au-dessus du point de stockage

    [Header("Options avancées")]
    public bool autoRestartWhenSpaceAvailable = true;
    public int minPiecesToRestart = 2;

    [Header("Événements")]
    public UnityEvent OnMachineStarted;
    public UnityEvent OnMachineStopped;
    public UnityEvent OnStockFull;
    public UnityEvent<int> OnStockChanged;

    private bool isProducing = false;
    private bool leftNext = true;
    private Coroutine productionCoroutine;
    private Coroutine monitorCoroutine;
    
    private Dictionary<int, GameObject> storedPieces = new Dictionary<int, GameObject>();
    private Queue<int> availableSlots = new Queue<int>();
    private Transform[] storagePositions = new Transform[4];
    private Transform[] leftRouteWaypoints = new Transform[4];
    private Transform[] rightRouteWaypoints = new Transform[4];

    public enum MovementMethod
    {
        SmoothFollow,      // Rotation fluide qui suit le chemin
        ParentToWaypoint,  // Parent la pièce au waypoint (meilleur pour inclinaison)
        RigidbodyPhysics   // Utilise la physique Unity
    }

    #region Initialization
    void Start()
    {
        ValidateSetup();
        InitializeRoutes();
        InitializeStorage();
        
        if (autoRestartWhenSpaceAvailable)
        {
            monitorCoroutine = StartCoroutine(MonitorStock());
        }
    }

    void OnDestroy()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
        }
    }

    void ValidateSetup()
    {
        if (piecePrefab == null)
            Debug.LogError("[MachineController] ❌ Aucun prefab de pièce assigné !");
        
        if (leftSpawnPoint == null || rightSpawnPoint == null)
            Debug.LogError("[MachineController] ❌ Points de spawn manquants !");

        if (leftWaypoint1 == null || leftWaypoint2 == null || leftWaypoint3 == null || leftWaypoint4 == null)
            Debug.LogError("[MachineController] ❌ Route GAUCHE incomplète ! Assignez les 4 waypoints.");

        if (rightWaypoint1 == null || rightWaypoint2 == null || rightWaypoint3 == null || rightWaypoint4 == null)
            Debug.LogError("[MachineController] ❌ Route DROITE incomplète ! Assignez les 4 waypoints.");

        if (storagePoint1 == null || storagePoint2 == null || storagePoint3 == null || storagePoint4 == null)
            Debug.LogError("[MachineController] ❌ Points de stockage incomplets ! Assignez les 4 positions.");
    }

    void InitializeRoutes()
    {
        leftRouteWaypoints[0] = leftWaypoint1;
        leftRouteWaypoints[1] = leftWaypoint2;
        leftRouteWaypoints[2] = leftWaypoint3;
        leftRouteWaypoints[3] = leftWaypoint4;

        rightRouteWaypoints[0] = rightWaypoint1;
        rightRouteWaypoints[1] = rightWaypoint2;
        rightRouteWaypoints[2] = rightWaypoint3;
        rightRouteWaypoints[3] = rightWaypoint4;
    }

    void InitializeStorage()
    {
        storagePositions[0] = storagePoint1;
        storagePositions[1] = storagePoint2;
        storagePositions[2] = storagePoint3;
        storagePositions[3] = storagePoint4;

        for (int i = 0; i < 4; i++)
        {
            availableSlots.Enqueue(i);
        }
    }
    #endregion

    #region Public Controls
    public void StartMachine()
    {
        if (!isProducing && CanProduce())
        {
            isProducing = true;
            productionCoroutine = StartCoroutine(ProducePieces());
            OnMachineStarted?.Invoke();
            Debug.Log("🟢 Machine démarrée");
        }
        else if (!CanProduce())
        {
            Debug.LogWarning("⚠️ Impossible de démarrer : stock plein (4/4)");
        }
    }

    public void StopMachine()
    {
        if (isProducing)
        {
            isProducing = false;
            if (productionCoroutine != null)
            {
                StopCoroutine(productionCoroutine);
                productionCoroutine = null;
            }
            OnMachineStopped?.Invoke();
            Debug.Log("🔴 Machine arrêtée");
        }
    }

    public void ToggleMachine()
    {
        if (isProducing)
            StopMachine();
        else
            StartMachine();
    }
    #endregion

    #region Production
    IEnumerator ProducePieces()
    {
        while (isProducing)
        {
            if (!CanProduce())
            {
                Debug.Log("⚠️ Stock plein (4/4) : arrêt automatique");
                OnStockFull?.Invoke();
                StopMachine();
                yield break;
            }

            SpawnAndAnimatePiece();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnAndAnimatePiece()
    {
        if (piecePrefab == null || availableSlots.Count == 0) return;

        Transform spawnPoint = leftNext ? leftSpawnPoint : rightSpawnPoint;
        Transform[] routeWaypoints = leftNext ? leftRouteWaypoints : rightRouteWaypoints;
        
        if (spawnPoint == null)
        {
            Debug.LogError("[MachineController] ❌ Point de spawn invalide !");
            return;
        }

        int slotIndex = availableSlots.Dequeue();
        GameObject piece = Instantiate(piecePrefab, spawnPoint.position, spawnPoint.rotation);
        
        if (piece != null)
        {
            // ✅ DÉSACTIVER LA PHYSIQUE pendant le déplacement
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Désactiver les collisions temporairement
            Collider[] colliders = piece.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            PieceTracker tracker = piece.AddComponent<PieceTracker>();
            tracker.slotIndex = slotIndex;
            tracker.controller = this;
            
            // Choisir la méthode de déplacement
            switch (movementMethod)
            {
                case MovementMethod.SmoothFollow:
                    StartCoroutine(AnimatePieceSmoothFollow(piece, routeWaypoints, slotIndex));
                    break;
                case MovementMethod.ParentToWaypoint:
                    StartCoroutine(AnimatePieceParented(piece, routeWaypoints, slotIndex));
                    break;
                case MovementMethod.RigidbodyPhysics:
                    StartCoroutine(AnimatePiecePhysics(piece, routeWaypoints, slotIndex));
                    break;
            }
            
            leftNext = !leftNext;
            
            string side = leftNext ? "DROITE" : "GAUCHE";
            Debug.Log($"✨ Pièce créée côté {side} → Slot {slotIndex + 1}/4");
        }
        else
        {
            availableSlots.Enqueue(slotIndex);
        }
    }

    // MÉTHODE 1: Rotation fluide améliorée
    IEnumerator AnimatePieceSmoothFollow(GameObject piece, Transform[] waypoints, int targetSlot)
    {
        if (piece == null || waypoints == null)
        {
            ReleaseSlot(targetSlot);
            yield break;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null || piece == null)
            {
                ReleaseSlot(targetSlot);
                yield break;
            }

            Vector3 startPos = piece.transform.position;
            Vector3 targetPos = waypoints[i].position;
            Quaternion startRot = piece.transform.rotation;
            Quaternion targetRot = waypoints[i].rotation; // Utiliser la rotation du waypoint
            
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / moveSpeed;
            float progress = 0f;

            while (progress < 1f && piece != null)
            {
                progress += Time.deltaTime / duration;
                
                // Position
                piece.transform.position = Vector3.Lerp(startPos, targetPos, progress);
                
                // Rotation - s'aligner sur le waypoint
                if (alignToPath)
                {
                    piece.transform.rotation = Quaternion.Slerp(startRot, targetRot, progress);
                }
                else
                {
                    // Rotation basée sur la direction
                    Vector3 direction = (targetPos - startPos).normalized;
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(direction);
                        piece.transform.rotation = Quaternion.Slerp(piece.transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
                    }
                }
                
                yield return null;
            }

            // Assurer la position/rotation finale
            if (piece != null)
            {
                piece.transform.position = targetPos;
                if (alignToPath) piece.transform.rotation = targetRot;
            }
        }

        if (piece != null)
        {
            yield return MoveToStorage(piece, targetSlot);
        }
    }

    // MÉTHODE 2: Parent au waypoint (RECOMMANDÉ pour inclinaisons)
    IEnumerator AnimatePieceParented(GameObject piece, Transform[] waypoints, int targetSlot)
    {
        if (piece == null || waypoints == null)
        {
            ReleaseSlot(targetSlot);
            yield break;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null || piece == null)
            {
                ReleaseSlot(targetSlot);
                yield break;
            }

            // Parent temporaire au waypoint
            Transform originalParent = piece.transform.parent;
            piece.transform.SetParent(waypoints[i]);
            
            Vector3 localStartPos = piece.transform.localPosition;
            Quaternion localStartRot = piece.transform.localRotation;
            Vector3 localTargetPos = Vector3.zero;
            Quaternion localTargetRot = Quaternion.identity;
            
            float distance = Vector3.Distance(piece.transform.position, waypoints[i].position);
            float duration = distance / moveSpeed;
            float progress = 0f;

            while (progress < 1f && piece != null)
            {
                progress += Time.deltaTime / duration;
                
                piece.transform.localPosition = Vector3.Lerp(localStartPos, localTargetPos, progress);
                piece.transform.localRotation = Quaternion.Slerp(localStartRot, localTargetRot, progress);
                
                yield return null;
            }

            // Détacher du waypoint
            if (piece != null)
            {
                piece.transform.SetParent(originalParent);
            }
        }

        if (piece != null)
        {
            yield return MoveToStorage(piece, targetSlot);
        }
    }

    // MÉTHODE 3: Physique Unity (nécessite Rigidbody)
    IEnumerator AnimatePiecePhysics(GameObject piece, Transform[] waypoints, int targetSlot)
    {
        if (piece == null || waypoints == null)
        {
            ReleaseSlot(targetSlot);
            yield break;
        }

        // Ajouter Rigidbody si nécessaire
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = piece.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null || piece == null)
            {
                ReleaseSlot(targetSlot);
                yield break;
            }

            while (piece != null && Vector3.Distance(piece.transform.position, waypoints[i].position) > 0.1f)
            {
                Vector3 direction = (waypoints[i].position - piece.transform.position).normalized;
                rb.MovePosition(piece.transform.position + direction * moveSpeed * Time.deltaTime);
                
                if (alignToPath)
                {
                    Quaternion targetRot = waypoints[i].rotation;
                    rb.MoveRotation(Quaternion.Slerp(piece.transform.rotation, targetRot, Time.deltaTime * rotationSpeed));
                }
                
                yield return new WaitForFixedUpdate();
            }
        }

        if (piece != null)
        {
            Destroy(rb); // Retirer le Rigidbody
            yield return MoveToStorage(piece, targetSlot);
        }
    }

    // Déplacement vers le stockage (commun à toutes les méthodes)
    IEnumerator MoveToStorage(GameObject piece, int targetSlot)
    {
        if (piece == null || targetSlot >= storagePositions.Length || storagePositions[targetSlot] == null)
        {
            ReleaseSlot(targetSlot);
            yield break;
        }

        Vector3 startPos = piece.transform.position;
        Vector3 dropPos = storagePositions[targetSlot].position + Vector3.up * dropHeight; // Position au-dessus
        Quaternion startRot = piece.transform.rotation;
        Quaternion storageRot = storagePositions[targetSlot].rotation;
        
        float distance = Vector3.Distance(startPos, dropPos);
        float duration = distance / moveSpeed;
        float progress = 0f;

        // Déplacer jusqu'au point de chute (au-dessus du stockage)
        while (progress < 1f && piece != null)
        {
            progress += Time.deltaTime / duration;
            piece.transform.position = Vector3.Lerp(startPos, dropPos, progress);
            piece.transform.rotation = Quaternion.Slerp(startRot, storageRot, progress);
            yield return null;
        }

        if (piece != null)
        {
            // ✅ Position finale au-dessus du stockage
            piece.transform.position = dropPos;
            piece.transform.rotation = storageRot;

            // ✅ ACTIVER LA PHYSIQUE pour la chute
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = piece.AddComponent<Rigidbody>();
            }

            if (enableGravityAtStorage)
            {
                rb.isKinematic = false;      // ✅ Activer la physique
                rb.useGravity = true;        // ✅ Activer la gravité → CHUTE !
                rb.linearDamping = 0.5f;      // Un peu d'amortissement
                rb.angularDamping = 0.5f;    // Rotation ralentie
            }
            else
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // ✅ Réactiver les collisions pour qu'elle tombe et se pose
            Collider[] colliders = piece.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }

            // Enregistrer dans le stockage
            storedPieces[targetSlot] = piece;
            OnStockChanged?.Invoke(CurrentStock());
            Debug.Log($"📦 Pièce en chute libre vers Storage{targetSlot + 1} ({CurrentStock()}/4)");

            if (CurrentStock() >= 4)
            {
                StopMachine();
            }
        }
        else
        {
            ReleaseSlot(targetSlot);
        }
    }
    #endregion

    #region Stock Management
    bool CanProduce()
    {
        return availableSlots.Count > 0;
    }

    IEnumerator MonitorStock()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (!isProducing && CurrentStock() < minPiecesToRestart && availableSlots.Count > 0)
            {
                StartMachine();
            }
        }
    }

    public void RemovePiece(int slotIndex)
    {
        if (storedPieces.ContainsKey(slotIndex))
        {
            GameObject piece = storedPieces[slotIndex];
            if (piece != null)
            {
                Destroy(piece);
            }
            storedPieces.Remove(slotIndex);
            ReleaseSlot(slotIndex);
            
            OnStockChanged?.Invoke(CurrentStock());
        }
    }

    void ReleaseSlot(int slotIndex)
    {
        if (!availableSlots.Contains(slotIndex))
        {
            availableSlots.Enqueue(slotIndex);
        }
    }

    public void ClearStorage()
    {
        foreach (var kvp in storedPieces)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        
        storedPieces.Clear();
        availableSlots.Clear();
        
        for (int i = 0; i < 4; i++)
        {
            availableSlots.Enqueue(i);
        }
        
        OnStockChanged?.Invoke(0);
    }
    #endregion

    #region Public Getters
    public bool IsProducing() => isProducing;
    public int CurrentStock() => storedPieces.Count;
    public int MaxCapacity() => 4;
    public float StockPercentage() => (float)CurrentStock() / 4f;
    public int AvailableSlots() => availableSlots.Count;
    #endregion

    #region Debug Gizmos
    void OnDrawGizmosSelected()
    {
        if (leftSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(leftSpawnPoint.position, 0.15f);
        }

        if (rightSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rightSpawnPoint.position, 0.15f);
        }

        DrawRoute(leftSpawnPoint, new Transform[] { leftWaypoint1, leftWaypoint2, leftWaypoint3, leftWaypoint4 }, Color.green);
        DrawRoute(rightSpawnPoint, new Transform[] { rightWaypoint1, rightWaypoint2, rightWaypoint3, rightWaypoint4 }, Color.blue);

        Gizmos.color = Color.yellow;
        if (storagePoint1 != null) DrawStoragePoint(storagePoint1, 0);
        if (storagePoint2 != null) DrawStoragePoint(storagePoint2, 1);
        if (storagePoint3 != null) DrawStoragePoint(storagePoint3, 2);
        if (storagePoint4 != null) DrawStoragePoint(storagePoint4, 3);
    }

    void DrawRoute(Transform spawn, Transform[] waypoints, Color color)
    {
        if (spawn == null) return;

        Gizmos.color = color;
        Vector3 lastPos = spawn.position;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.1f);
                Gizmos.DrawLine(lastPos, waypoints[i].position);
                lastPos = waypoints[i].position;
            }
        }
    }

    void DrawStoragePoint(Transform storage, int index)
    {
        if (storage == null) return;

        if (Application.isPlaying && storedPieces.ContainsKey(index))
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(storage.position, 0.12f);
        Gizmos.DrawWireCube(storage.position, Vector3.one * 0.2f);
    }
    #endregion
}

public class ConveyorController
{
}

public class PieceTracker : MonoBehaviour
{
    public int slotIndex;
    public MachineController controller;

    void OnDestroy()
    {
        if (controller != null)
        {
            controller.RemovePiece(slotIndex);
        }
    }
}