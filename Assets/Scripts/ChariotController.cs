using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Contrôleur de chariot avec système de chargement de pièces
/// Trolley controller with piece loading system
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChariotController : MonoBehaviour
{
    #region Inspector Configuration

    [Header("🎮 Control Settings")]
    [Tooltip("Distance maximale pour interagir avec le chariot")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Tooltip("Référence au joueur (auto-détecté si vide)")]
    [SerializeField] private Transform player;

    [Header("📦 Piece Types (Max 2 types)")]
    [Tooltip("Premier type de pièce accepté")]
    [SerializeField] private GameObject pieceType1Prefab;
    
    [Tooltip("Deuxième type de pièce accepté")]
    [SerializeField] private GameObject pieceType2Prefab;

    [Header("📍 Loading Slots (Empty GameObjects)")]
    [Tooltip("Glissez ici les 4 Empty GameObjects qui définissent les emplacements")]
    [SerializeField] private Transform[] loadingSlots = new Transform[4];

    [Header("🔧 Advanced Settings")]
    [Tooltip("Taille de la zone de détection de chargement")]
    [SerializeField] private Vector3 loadingZoneSize = new Vector3(2.5f, 2f, 2.5f);
    
    [Tooltip("Hauteur de la zone de chargement")]
    [SerializeField] private float loadingZoneHeight = 1f;
    
    [Tooltip("Activer les logs de debug")]
    [SerializeField] private bool debugMode = true;

    #endregion

    #region Constants

    // Position et rotation du chariot par rapport au joueur
    private const float PUSH_DISTANCE = 0.92f;
    private const float HEIGHT_OFFSET = 0.68f;
    private const float ROTATION_Y = 180f;
    
    private const int MAX_CAPACITY = 4;

    #endregion

    #region Private Variables

    private Rigidbody rb;
    private BoxCollider loadingZoneTrigger;
    private PickUpSimple playerPickupScript;
    
    private bool isPlayerControlling;
    private GameObject[] loadedPieces = new GameObject[MAX_CAPACITY]; // ✅ Array au lieu de List
    private HashSet<string> acceptedTags = new HashSet<string>();
    
    // ✅ NOUVEAU : Suivi du dernier objet détecté pour éviter les doubles chargements
    private GameObject lastDetectedPiece;
    private float lastLoadTime;
    private const float LOAD_COOLDOWN = 0.5f; // Temps entre deux chargements

    #endregion

    #region Initialization

    void Awake()
    {
        InitializeComponents();
        BuildAcceptedTagsList();
    }

    void Start()
    {
        FindPlayerIfNeeded();
        ValidateConfiguration();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Zone de chargement (Trigger)
        loadingZoneTrigger = gameObject.AddComponent<BoxCollider>();
        loadingZoneTrigger.isTrigger = true;
        loadingZoneTrigger.center = new Vector3(0f, loadingZoneHeight, 0f);
        loadingZoneTrigger.size = loadingZoneSize;

        LogDebug("✅ Composants initialisés");
    }

    void BuildAcceptedTagsList()
    {
        acceptedTags.Clear();

        if (pieceType1Prefab != null)
        {
            acceptedTags.Add(pieceType1Prefab.tag);
            LogDebug($"✅ Type 1: {pieceType1Prefab.name} (Tag: {pieceType1Prefab.tag})");
        }

        if (pieceType2Prefab != null)
        {
            acceptedTags.Add(pieceType2Prefab.tag);
            LogDebug($"✅ Type 2: {pieceType2Prefab.name} (Tag: {pieceType2Prefab.tag})");
        }
    }

    void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                LogDebug($"✅ Joueur trouvé automatiquement: {player.name}");
            }
            else
            {
                Debug.LogError("❌ Aucun joueur trouvé !");
                return;
            }
        }

        playerPickupScript = player.GetComponentInChildren<PickUpSimple>();
        if (playerPickupScript == null)
        {
            Debug.LogWarning("⚠️ Script PickUpSimple non trouvé");
        }
    }

    void ValidateConfiguration()
    {
        if (acceptedTags.Count == 0)
        {
            Debug.LogWarning("⚠️ Aucun type de pièce configuré !");
        }

        // ✅ NOUVEAU : Vérifier les slots
        bool allSlotsAssigned = true;
        for (int i = 0; i < loadingSlots.Length; i++)
        {
            if (loadingSlots[i] == null)
            {
                Debug.LogError($"❌ Loading Slot {i + 1} non assigné !");
                allSlotsAssigned = false;
            }
        }

        if (allSlotsAssigned)
        {
            LogDebug($"✅ {loadingSlots.Length} emplacements configurés");
        }
    }

    #endregion

    #region Update Loop

    void Update()
    {
        if (player == null) return;
        HandlePlayerInteraction();
    }

    void HandlePlayerInteraction()
    {
        float distance = Vector3.Distance(player.position, transform.position);
        bool isInRange = distance <= interactionDistance;

        // Touche E : Pousser/Relâcher le chariot
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isPlayerControlling)
            {
                ReleaseTrolley();
            }
            else if (isInRange)
            {
                StartPushing();
            }
        }

        // Touche R : Décharger toutes les pièces
        if (Input.GetKeyDown(KeyCode.R) && isInRange && GetLoadedCount() > 0)
        {
            UnloadAllPieces();
        }

        // Touche T : Afficher les statistiques
        if (Input.GetKeyDown(KeyCode.T) && isInRange)
        {
            PrintStatistics();
        }

        // ✅ NOUVEAU : Touche Q pour charger manuellement la pièce tenue
        if (Input.GetKeyDown(KeyCode.Q) && isInRange)
        {
            TryLoadHeldPiece();
        }
    }

    #endregion

    #region Piece Loading System

    /// <summary>
    /// ✅ NOUVEAU : Tente de charger la pièce tenue manuellement (touche Q)
    /// </summary>
    void TryLoadHeldPiece()
    {
        if (playerPickupScript == null || playerPickupScript.hand == null)
        {
            LogDebug("❌ Pas de système de ramassage disponible");
            return;
        }

        // Vérifier si le joueur tient une pièce
        if (playerPickupScript.hand.childCount == 0)
        {
            LogDebug("❌ Vous ne tenez aucune pièce");
            return;
        }

        GameObject heldPiece = playerPickupScript.hand.GetChild(0).gameObject;
        
        if (CanLoadPiece(heldPiece))
        {
            LoadPiece(heldPiece);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // ✅ MODIFIÉ : Vérification du cooldown pour éviter les doubles chargements
        if (Time.time - lastLoadTime < LOAD_COOLDOWN)
        {
            return;
        }

        if (other.gameObject == lastDetectedPiece)
        {
            return;
        }

        if (!CanLoadPiece(other.gameObject)) return;

        LoadPiece(other.gameObject);
        lastDetectedPiece = other.gameObject;
        lastLoadTime = Time.time;
    }

    bool CanLoadPiece(GameObject piece)
    {
        if (IsFull())
        {
            LogDebug("⚠️ Chariot plein !");
            return false;
        }

        if (!IsAcceptedPieceType(piece))
        {
            LogDebug($"❌ Type non accepté: {piece.name}");
            return false;
        }

        if (!IsHeldByPlayer(piece))
        {
            LogDebug("❌ La pièce doit être tenue par le joueur");
            return false;
        }

        // ✅ NOUVEAU : Vérifier que la pièce n'est pas déjà chargée
        for (int i = 0; i < loadedPieces.Length; i++)
        {
            if (loadedPieces[i] == piece)
            {
                LogDebug("❌ Cette pièce est déjà chargée");
                return false;
            }
        }

        return true;
    }

    bool IsAcceptedPieceType(GameObject piece)
    {
        return acceptedTags.Contains(piece.tag);
    }

    bool IsHeldByPlayer(GameObject piece)
    {
        if (playerPickupScript == null || playerPickupScript.hand == null)
            return false;

        return piece.transform.parent == playerPickupScript.hand;
    }

    /// <summary>
    /// ✅ MODIFIÉ : Utilise les Empty GameObjects comme emplacements
    /// </summary>
    void LoadPiece(GameObject piece)
    {
        // Trouver le premier emplacement libre
        int freeSlotIndex = GetFirstFreeSlot();
        
        if (freeSlotIndex == -1)
        {
            LogDebug("❌ Aucun emplacement libre !");
            return;
        }

        Transform targetSlot = loadingSlots[freeSlotIndex];

        // Détacher de la main du joueur
        piece.transform.SetParent(targetSlot);

        // ✅ Positionner EXACTEMENT à l'emplacement du slot
        piece.transform.localPosition = Vector3.zero;
        piece.transform.localRotation = Quaternion.identity;

        // Désactiver la physique
        DisablePiecePhysics(piece);

        // Ajouter à l'array
        loadedPieces[freeSlotIndex] = piece;

        LogDebug($"📦 {piece.name} chargé dans Slot {freeSlotIndex + 1} [{GetLoadedCount()}/{MAX_CAPACITY}]");
    }

    /// <summary>
    /// ✅ NOUVEAU : Trouve le premier emplacement libre
    /// </summary>
    int GetFirstFreeSlot()
    {
        for (int i = 0; i < loadedPieces.Length; i++)
        {
            if (loadedPieces[i] == null)
            {
                return i;
            }
        }
        return -1; // Aucun emplacement libre
    }

    void DisablePiecePhysics(GameObject piece)
    {
        Rigidbody pieceRb = piece.GetComponent<Rigidbody>();
        if (pieceRb != null)
        {
            pieceRb.isKinematic = true;
            pieceRb.useGravity = false;
            pieceRb.linearVelocity = Vector3.zero;
            pieceRb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in piece.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    void EnablePiecePhysics(GameObject piece)
    {
        Rigidbody pieceRb = piece.GetComponent<Rigidbody>();
        if (pieceRb != null)
        {
            pieceRb.isKinematic = false;
            pieceRb.useGravity = true;
        }

        foreach (Collider col in piece.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
    }

    public void UnloadAllPieces()
    {
        int count = 0;

        for (int i = 0; i < loadedPieces.Length; i++)
        {
            if (loadedPieces[i] != null)
            {
                GameObject piece = loadedPieces[i];
                
                // Déparenter
                piece.transform.SetParent(null);
                
                // Positionner au-dessus du chariot
                piece.transform.position = transform.position + Vector3.up * (count * 0.5f + 1f);

                // Réactiver la physique
                EnablePiecePhysics(piece);

                loadedPieces[i] = null;
                count++;
            }
        }

        // Réinitialiser le cooldown
        lastDetectedPiece = null;
        
        LogDebug($"📤 {count} pièce(s) déchargée(s)");
    }

    #endregion

    #region Trolley Control

    void StartPushing()
    {
        isPlayerControlling = true;
        
        transform.SetParent(player);
        rb.isKinematic = true;
        
        transform.localPosition = new Vector3(0f, HEIGHT_OFFSET, PUSH_DISTANCE);
        transform.localRotation = Quaternion.Euler(0f, ROTATION_Y, 0f);
        
        LogDebug($"🚛 Chariot saisi ! Pièces: {GetLoadedCount()}/{MAX_CAPACITY}");
    }

    void ReleaseTrolley()
    {
        isPlayerControlling = false;
        
        transform.SetParent(null);
        rb.isKinematic = false;
        
        LogDebug("🚶 Chariot relâché");
    }

    public void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    #endregion

    #region Public API

    /// <summary>
    /// ✅ MODIFIÉ : Compte les pièces non-null
    /// </summary>
    public int GetLoadedCount()
    {
        int count = 0;
        for (int i = 0; i < loadedPieces.Length; i++)
        {
            if (loadedPieces[i] != null)
                count++;
        }
        return count;
    }

    public bool IsFull() => GetLoadedCount() >= MAX_CAPACITY;
    public bool IsEmpty() => GetLoadedCount() == 0;
    public int GetRemainingCapacity() => MAX_CAPACITY - GetLoadedCount();

    public void PrintStatistics()
    {
        if (GetLoadedCount() == 0)
        {
            Debug.Log("📊 Chariot vide");
            return;
        }

        Dictionary<string, int> stats = new Dictionary<string, int>();
        
        for (int i = 0; i < loadedPieces.Length; i++)
        {
            if (loadedPieces[i] != null)
            {
                string typeName = loadedPieces[i].name.Replace("(Clone)", "").Trim();
                if (!stats.ContainsKey(typeName))
                    stats[typeName] = 0;
                stats[typeName]++;
            }
        }

        Debug.Log($"📊 Chargement: {GetLoadedCount()}/{MAX_CAPACITY}");
        foreach (var stat in stats)
        {
            Debug.Log($"   • {stat.Key}: {stat.Value}x");
        }
    }

    #endregion

    #region Utilities

    void LogDebug(string message)
    {
        if (debugMode)
            Debug.Log($"[Chariot] {message}");
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Zone d'interaction
        Gizmos.color = isPlayerControlling ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Position de poussée
        if (player != null && !isPlayerControlling && Application.isPlaying)
        {
            Vector3 pushPos = player.position + player.forward * PUSH_DISTANCE + Vector3.up * HEIGHT_OFFSET;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pushPos, Vector3.one);
            Gizmos.DrawLine(player.position, pushPos);
        }

        // Zone de chargement
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, loadingZoneHeight, 0f), loadingZoneSize);

        // ✅ MODIFIÉ : Afficher les emplacements depuis les Empty GameObjects
        for (int i = 0; i < loadingSlots.Length; i++)
        {
            if (loadingSlots[i] != null)
            {
                Vector3 worldPos = loadingSlots[i].position;
                
                // Couleur selon l'état
                bool isOccupied = Application.isPlaying && i < loadedPieces.Length && loadedPieces[i] != null;
                Gizmos.color = isOccupied ? Color.green : new Color(1f, 0f, 0f, 0.5f);
                
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.5f);
                
                // Afficher le numéro du slot
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, $"Slot {i + 1}");
                #endif
            }
        }
    }

    #endregion
}