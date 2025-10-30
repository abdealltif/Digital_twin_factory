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

    [Header("Prefabs - Types de pièces")]
    public GameObject pieceTypePrincipal;  // 80%
    public GameObject pieceTypeSecondaire;  // 20%
    [Range(0f, 100f)]
    public float pourcentageTypePrincipal = 80f;

    [Header("Contrôle externe")]
    public ConveyorButtonController conveyorButton;

    [Header("Méthode de déplacement")]
    public MovementMethod movementMethod = MovementMethod.SmoothFollow;
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    public bool alignToPath = true;

    [Header("Options avancées")]
    public bool autoRestartWhenSpaceAvailable = true;
    public int minPiecesToRestart = 2;

    [Header("Événements")]
    public UnityEvent OnMachineStarted;
    public UnityEvent OnMachineStopped;
    public UnityEvent OnStockFull;
    public UnityEvent<int> OnStockChanged;
    public UnityEvent<PieceType> OnPieceProduced;  // Nouveau événement

    private bool isProducing = false;
    private bool leftNext = true;
    private Coroutine productionCoroutine;
    private Coroutine monitorCoroutine;
    
    private Dictionary<int, PieceData> storedPieces = new Dictionary<int, PieceData>();
    private Queue<int> availableSlots = new Queue<int>();
    private Transform[] storagePositions;
    private Transform[] leftRouteWaypoints;
    private Transform[] rightRouteWaypoints;

    // Statistiques de production
    private int totalProduced = 0;
    private int principalProduced = 0;
    private int secondaireProduced = 0;

    public enum MovementMethod
    {
        SmoothFollow,
        ParentToWaypoint,
        RigidbodyPhysics
    }

    public enum PieceType
    {
        Principal,
        Secondaire
    }

    private class PieceData
    {
        public GameObject piece;
        public PieceType type;

        public PieceData(GameObject p, PieceType t)
        {
            piece = p;
            type = t;
        }
    }

    #region Initialization
    void Start()
    {
        ValidateSetup();
        InitializeArrays();
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
        if (pieceTypePrincipal == null || pieceTypeSecondaire == null)
            Debug.LogError("[MachineController] ❌ Prefabs de pièces manquants ! Assignez les deux types.");
        
        if (leftSpawnPoint == null || rightSpawnPoint == null)
            Debug.LogError("[MachineController] ❌ Points de spawn manquants !");

        if (leftWaypoint1 == null || leftWaypoint2 == null || leftWaypoint3 == null || leftWaypoint4 == null)
            Debug.LogError("[MachineController] ❌ Route GAUCHE incomplète !");

        if (rightWaypoint1 == null || rightWaypoint2 == null || rightWaypoint3 == null || rightWaypoint4 == null)
            Debug.LogError("[MachineController] ❌ Route DROITE incomplète !");

        if (storagePoint1 == null || storagePoint2 == null || storagePoint3 == null || storagePoint4 == null)
            Debug.LogError("[MachineController] ❌ Points de stockage incomplets !");
        
        if (conveyorButton == null)
            Debug.LogWarning("[MachineController] ⚠️ Aucun bouton de contrôle assigné !");
    }

    void InitializeArrays()
    {
        storagePositions = new Transform[4];
        leftRouteWaypoints = new Transform[4];
        rightRouteWaypoints = new Transform[4];
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
        if (conveyorButton != null && !conveyorButton.IsConveyorRunning())
        {
            Debug.LogWarning("⚠️ Impossible de démarrer : convoyeur arrêté !");
            return;
        }

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
            if (conveyorButton != null && !conveyorButton.IsConveyorRunning())
            {
                Debug.LogWarning("⚠️ Convoyeur arrêté : pause de la production !");
                StopMachine();
                yield break;
            }

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
        if (availableSlots.Count == 0) return;

        // Déterminer le type de pièce (80/20)
        PieceType type = DetermineProductType();
        GameObject prefabToUse = (type == PieceType.Principal) ? pieceTypePrincipal : pieceTypeSecondaire;

        if (prefabToUse == null)
        {
            Debug.LogError($"[MachineController] ❌ Prefab {type} manquant !");
            return;
        }

        Transform spawnPoint = leftNext ? leftSpawnPoint : rightSpawnPoint;
        Transform[] routeWaypoints = leftNext ? leftRouteWaypoints : rightRouteWaypoints;
        
        if (spawnPoint == null) return;

        int slotIndex = availableSlots.Dequeue();
        GameObject piece = Instantiate(prefabToUse, spawnPoint.position, spawnPoint.rotation);
        
        if (piece != null)
        {
            ConfigurePiecePhysics(piece, false);

            PieceTracker tracker = piece.AddComponent<PieceTracker>();
            tracker.slotIndex = slotIndex;
            tracker.controller = this;
            tracker.pieceType = type;
            
            // Mise à jour des statistiques
            totalProduced++;
            if (type == PieceType.Principal)
                principalProduced++;
            else
                secondaireProduced++;

            OnPieceProduced?.Invoke(type);

            StartCoroutine(AnimatePiece(piece, routeWaypoints, slotIndex, type));
            
            leftNext = !leftNext;
            
            string side = leftNext ? "DROITE" : "GAUCHE";
            string typeName = (type == PieceType.Principal) ? "Principal" : "Secondaire";
            Debug.Log($"✨ Pièce {typeName} créée côté {side} → Slot {slotIndex + 1}/4 (Stats: {principalProduced}/{secondaireProduced})");
        }
        else
        {
            availableSlots.Enqueue(slotIndex);
        }
    }

    PieceType DetermineProductType()
    {
        float random = Random.Range(0f, 100f);
        return (random < pourcentageTypePrincipal) ? PieceType.Principal : PieceType.Secondaire;
    }

    void ConfigurePiecePhysics(GameObject piece, bool enablePhysics)
    {
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enablePhysics;
            rb.useGravity = enablePhysics;
        }

        Collider[] colliders = piece.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = enablePhysics;
        }
    }

    IEnumerator AnimatePiece(GameObject piece, Transform[] waypoints, int targetSlot, PieceType type)
    {
        switch (movementMethod)
        {
            case MovementMethod.SmoothFollow:
                yield return AnimatePieceSmoothFollow(piece, waypoints);
                break;
            case MovementMethod.ParentToWaypoint:
                yield return AnimatePieceParented(piece, waypoints);
                break;
            case MovementMethod.RigidbodyPhysics:
                yield return AnimatePiecePhysics(piece, waypoints);
                break;
        }

        if (piece != null)
        {
            yield return MoveToStorage(piece, targetSlot, type);
        }
        else
        {
            ReleaseSlot(targetSlot);
        }
    }

    IEnumerator AnimatePieceSmoothFollow(GameObject piece, Transform[] waypoints)
    {
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null || piece == null) yield break;

            Vector3 startPos = piece.transform.position;
            Quaternion startRot = piece.transform.rotation;
            
            float distance = Vector3.Distance(startPos, waypoint.position);
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                piece.transform.position = Vector3.Lerp(startPos, waypoint.position, t);
                
                if (alignToPath)
                {
                    piece.transform.rotation = Quaternion.Slerp(startRot, waypoint.rotation, t);
                }
                else
                {
                    Vector3 direction = (waypoint.position - startPos).normalized;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(direction);
                        piece.transform.rotation = Quaternion.RotateTowards(piece.transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
                    }
                }
                
                yield return null;
            }

            if (piece != null)
            {
                piece.transform.position = waypoint.position;
                if (alignToPath) piece.transform.rotation = waypoint.rotation;
            }
        }
    }

    IEnumerator AnimatePieceParented(GameObject piece, Transform[] waypoints)
    {
        Transform originalParent = piece.transform.parent;

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null || piece == null) yield break;

            piece.transform.SetParent(waypoint);
            
            Vector3 startPos = piece.transform.localPosition;
            Quaternion startRot = piece.transform.localRotation;
            
            float distance = startPos.magnitude;
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration && piece != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                piece.transform.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
                piece.transform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
                
                yield return null;
            }
        }

        if (piece != null)
        {
            piece.transform.SetParent(originalParent);
        }
    }

    IEnumerator AnimatePiecePhysics(GameObject piece, Transform[] waypoints)
    {
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = piece.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null || piece == null) yield break;

            while (piece != null && Vector3.SqrMagnitude(piece.transform.position - waypoint.position) > 0.01f)
            {
                Vector3 direction = (waypoint.position - piece.transform.position).normalized;
                rb.MovePosition(piece.transform.position + direction * moveSpeed * Time.fixedDeltaTime);
                
                if (alignToPath)
                {
                    rb.MoveRotation(Quaternion.RotateTowards(piece.transform.rotation, waypoint.rotation, rotationSpeed * Time.fixedDeltaTime));
                }
                
                yield return new WaitForFixedUpdate();
            }
        }

        if (piece != null && rb != null)
        {
            Destroy(rb);
        }
    }

    IEnumerator MoveToStorage(GameObject piece, int targetSlot, PieceType type)
    {
        if (piece == null || targetSlot >= storagePositions.Length || storagePositions[targetSlot] == null)
        {
            ReleaseSlot(targetSlot);
            yield break;
        }

        Transform storageTransform = storagePositions[targetSlot];
        Vector3 startPos = piece.transform.position;
        Quaternion startRot = piece.transform.rotation;
        
        float distance = Vector3.Distance(startPos, storageTransform.position);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration && piece != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            piece.transform.position = Vector3.Lerp(startPos, storageTransform.position, t);
            piece.transform.rotation = Quaternion.Slerp(startRot, storageTransform.rotation, t);
            yield return null;
        }

        if (piece != null)
        {
            piece.transform.SetPositionAndRotation(storageTransform.position, storageTransform.rotation);
            ConfigurePiecePhysics(piece, true);  // ✅ Physique activée pour le picking
            piece.transform.SetParent(storageTransform);

            storedPieces[targetSlot] = new PieceData(piece, type);
            OnStockChanged?.Invoke(CurrentStock());
            
            string typeName = (type == PieceType.Principal) ? "Principal" : "Secondaire";
            Debug.Log($"📦 Pièce {typeName} stockée → Storage{targetSlot + 1} ({CurrentStock()}/4)");

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
    bool CanProduce() => availableSlots.Count > 0;

    IEnumerator MonitorStock()
    {
        WaitForSeconds delay = new WaitForSeconds(0.5f);
        
        while (true)
        {
            yield return delay;

            if (!isProducing && 
                CurrentStock() < minPiecesToRestart && 
                availableSlots.Count > 0 &&
                (conveyorButton == null || conveyorButton.IsConveyorRunning()))
            {
                StartMachine();
            }
        }
    }

    public void RemovePiece(int slotIndex)
    {
        if (storedPieces.TryGetValue(slotIndex, out PieceData data))
        {
            if (data.piece != null)
            {
                Destroy(data.piece);
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
            if (kvp.Value?.piece != null)
                Destroy(kvp.Value.piece);
        }
        
        storedPieces.Clear();
        availableSlots.Clear();
        
        for (int i = 0; i < 4; i++)
        {
            availableSlots.Enqueue(i);
        }
        
        OnStockChanged?.Invoke(0);
        ResetStatistics();
    }

    void ResetStatistics()
    {
        totalProduced = 0;
        principalProduced = 0;
        secondaireProduced = 0;
    }
    #endregion

    #region Public Getters
    public bool IsProducing() => isProducing;
    public int CurrentStock() => storedPieces.Count;
    public int MaxCapacity() => 4;
    public float StockPercentage() => storedPieces.Count / 4f;
    public int AvailableSlots() => availableSlots.Count;
    
    // Nouvelles statistiques
    public int GetTotalProduced() => totalProduced;
    public int GetPrincipalProduced() => principalProduced;
    public int GetSecondaireProduced() => secondaireProduced;
    public float GetActualPrincipalPercentage() => totalProduced > 0 ? (principalProduced * 100f / totalProduced) : 0f;
    
    public PieceType GetStoredPieceType(int slotIndex)
    {
        return storedPieces.TryGetValue(slotIndex, out PieceData data) ? data.type : PieceType.Principal;
    }
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

        DrawStoragePoints();
    }

    void DrawRoute(Transform spawn, Transform[] waypoints, Color color)
    {
        if (spawn == null) return;

        Gizmos.color = color;
        Vector3 lastPos = spawn.position;

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawWireSphere(waypoint.position, 0.1f);
                Gizmos.DrawLine(lastPos, waypoint.position);
                lastPos = waypoint.position;
            }
        }
    }

    void DrawStoragePoints()
    {
        Transform[] points = { storagePoint1, storagePoint2, storagePoint3, storagePoint4 };
        
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                if (Application.isPlaying && storedPieces.ContainsKey(i))
                {
                    PieceType type = storedPieces[i].type;
                    Gizmos.color = (type == PieceType.Principal) ? Color.cyan : Color.magenta;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }

                Gizmos.DrawWireSphere(points[i].position, 0.12f);
                Gizmos.DrawWireCube(points[i].position, Vector3.one * 0.2f);
            }
        }
    }
    #endregion
}

public class PieceTracker : MonoBehaviour
{
    public int slotIndex;
    public MachineController controller;
    public MachineController.PieceType pieceType;

    void OnDestroy()
    {
        if (controller != null)
        {
            controller.RemovePiece(slotIndex);
        }
    }
}