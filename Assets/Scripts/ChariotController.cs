using UnityEngine;

/// <summary>
/// Contrôleur pour un chariot avec contrôle manuel ou IA
/// Controller for a trolley with manual or AI control
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChariotController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Vitesse de déplacement / Movement speed")]
    public float moveSpeed = 5f;
    
    [Tooltip("Vitesse de rotation / Rotation speed")]
    public float rotationSpeed = 100f;

    [Header("Control Settings")]
    [Tooltip("Distance pour interagir avec le chariot / Distance to interact with trolley")]
    public float interactionDistance = 3f;
    
    [Tooltip("Référence au joueur / Player reference")]
    public Transform player;

    [Header("Wheel References (Optional)")]
    [Tooltip("Roues avant gauche et droite / Front left and right wheels")]
    public Transform[] frontWheels;
    
    [Tooltip("Roues arrière gauche et droite / Rear left and right wheels")]
    public Transform[] rearWheels;
    
    [Tooltip("Vitesse de rotation des roues / Wheel rotation speed")]
    public float wheelRotationSpeed = 360f;

    [Header("AI Settings")]
    [Tooltip("Distance d'arrêt pour l'IA / AI stopping distance")]
    public float stoppingDistance = 1f;

    // Composants privés / Private components
    private Rigidbody rb;
    private bool isPlayerControlling = false;
    private Vector3 aiDestination;
    private Transform aiTarget;
    private bool isMovingToDestination = false;
    private bool isFollowingTarget = false;

    void Start()
    {
        // Initialisation du Rigidbody / Initialize Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Trouver le joueur automatiquement si non assigné / Find player automatically if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        // Vérifier l'interaction avec le chariot / Check interaction with trolley
        CheckInteraction();

        // Contrôle du joueur / Player control
        if (isPlayerControlling)
        {
            HandlePlayerInput();
        }
        // Contrôle IA / AI control
        else
        {
            HandleAIMovement();
        }

        // Debug visuel / Visual debug
        DrawDebugInfo();
    }

    /// <summary>
    /// Vérifie si le joueur peut interagir avec le chariot
    /// Checks if player can interact with the trolley
    /// </summary>
    void CheckInteraction()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Affichage de la distance en debug / Display distance in debug
        Debug.Log($"Distance to trolley: {distance:F2}m - Press 'E' to {(isPlayerControlling ? "exit" : "enter")}");

        // Détection de la touche E / E key detection
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Si le joueur est assez proche / If player is close enough
            if (distance <= interactionDistance)
            {
                isPlayerControlling = !isPlayerControlling;

                if (isPlayerControlling)
                {
                    Debug.Log("🚛 Player is now controlling the trolley!");
                    Stop(); // Arrêter le mouvement IA / Stop AI movement
                }
                else
                {
                    Debug.Log("🚶 Player exited trolley control");
                    Stop(); // Arrêter le mouvement / Stop movement
                }
            }
            else
            {
                Debug.LogWarning($"❌ Too far from trolley! Distance: {distance:F2}m (Required: {interactionDistance}m)");
            }
        }
    }

    /// <summary>
    /// Gestion des inputs du joueur (ZQSD/WASD + Flèches)
    /// Handle player input (ZQSD/WASD + Arrows)
    /// </summary>
    void HandlePlayerInput()
    {
        // Récupérer les inputs / Get inputs
        float moveInput = Input.GetAxis("Vertical");   // Z/S ou Flèches Haut/Bas
        float turnInput = Input.GetAxis("Horizontal"); // Q/D ou Flèches Gauche/Droite

        // Déplacement / Movement
        Vector3 movement = transform.forward * moveInput * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        // Rotation / Rotation
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float rotation = turnInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotation, 0);
        }

        // Rotation des roues / Wheel rotation
        RotateWheels(moveInput);

        // Debug info
        if (moveInput != 0 || turnInput != 0)
        {
            Debug.Log($"🎮 Player Input - Move: {moveInput:F2}, Turn: {turnInput:F2}");
        }
    }

    /// <summary>
    /// Gestion du mouvement IA (destination ou suivi)
    /// Handle AI movement (destination or follow)
    /// </summary>
    void HandleAIMovement()
    {
        // Déplacement vers une destination / Move to destination
        if (isMovingToDestination)
        {
            MoveTowardsDestination(aiDestination);
        }
        // Suivi d'une cible / Follow target
        else if (isFollowingTarget && aiTarget != null)
        {
            MoveTowardsDestination(aiTarget.position);
        }
        else
        {
            // Arrêter le mouvement si aucune action IA / Stop if no AI action
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Déplace le chariot vers une destination
    /// Move trolley towards a destination
    /// </summary>
    void MoveTowardsDestination(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, destination);

        // Vérifier si on a atteint la destination / Check if reached destination
        if (distance <= stoppingDistance)
        {
            Stop();
            Debug.Log("🎯 AI reached destination!");
            return;
        }

        // Déplacement / Movement
        rb.linearVelocity = direction * moveSpeed;

        // Rotation vers la destination / Rotate towards destination
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 0.01f);

        // Rotation des roues / Wheel rotation
        RotateWheels(1f);

        Debug.Log($"🤖 AI Moving - Distance: {distance:F2}m");
    }

    /// <summary>
    /// Fait tourner les roues du chariot
    /// Rotate trolley wheels
    /// </summary>
    void RotateWheels(float moveInput)
    {
        if (frontWheels == null && rearWheels == null) return;

        float wheelRotation = moveInput * wheelRotationSpeed * Time.deltaTime;

        // Rotation des roues avant / Front wheels rotation
        if (frontWheels != null)
        {
            foreach (Transform wheel in frontWheels)
            {
                if (wheel != null)
                    wheel.Rotate(wheelRotation, 0, 0);
            }
        }

        // Rotation des roues arrière / Rear wheels rotation
        if (rearWheels != null)
        {
            foreach (Transform wheel in rearWheels)
            {
                if (wheel != null)
                    wheel.Rotate(wheelRotation, 0, 0);
            }
        }
    }

    #region Public Methods - IA Control

    /// <summary>
    /// Déplace le chariot vers une destination spécifique (IA)
    /// Move trolley to a specific destination (AI)
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (isPlayerControlling)
        {
            Debug.LogWarning("⚠️ Cannot use AI control while player is controlling!");
            return;
        }

        aiDestination = destination;
        isMovingToDestination = true;
        isFollowingTarget = false;
        Debug.Log($"🤖 AI MoveTo: {destination}");
    }

    /// <summary>
    /// Fait suivre une cible au chariot (IA)
    /// Make trolley follow a target (AI)
    /// </summary>
    public void Follow(Transform target)
    {
        if (isPlayerControlling)
        {
            Debug.LogWarning("⚠️ Cannot use AI control while player is controlling!");
            return;
        }

        aiTarget = target;
        isFollowingTarget = true;
        isMovingToDestination = false;
        Debug.Log($"🤖 AI Following: {target.name}");
    }

    /// <summary>
    /// Arrête tout mouvement du chariot
    /// Stop all trolley movement
    /// </summary>
    public void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isMovingToDestination = false;
        isFollowingTarget = false;
        Debug.Log("🛑 Trolley stopped");
    }

    #endregion

    /// <summary>
    /// Affiche les informations de debug visuelles
    /// Display visual debug information
    /// </summary>
    void DrawDebugInfo()
    {
        // Sphere d'interaction / Interaction sphere
        Debug.DrawRay(transform.position, Vector3.up * 2f, isPlayerControlling ? Color.green : Color.yellow);
        
        // Direction de destination IA / AI destination direction
        if (isMovingToDestination)
        {
            Debug.DrawLine(transform.position, aiDestination, Color.blue);
        }
        
        // Direction de suivi IA / AI follow direction
        if (isFollowingTarget && aiTarget != null)
        {
            Debug.DrawLine(transform.position, aiTarget.position, Color.cyan);
        }
    }

    // Afficher la zone d'interaction dans l'éditeur / Show interaction zone in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isPlayerControlling ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}