using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RackSystemController : MonoBehaviour
{
    [Header("Cubes")]
    public Transform cube1;
    public Transform cube2;
    
    [Header("Positions de référence")]
    public Transform homePosition;
    public Transform loadingPosition;
    
    [Header("Positions du Rack - Ligne 1")]
    public Transform position_1_1;
    public Transform position_1_2;
    public Transform position_1_3;
    public Transform position_1_4;
    
    [Header("Positions du Rack - Ligne 2")]
    public Transform position_2_1;
    public Transform position_2_2;
    public Transform position_2_3;
    public Transform position_2_4;
    
    [Header("Paramètres de déplacement")]
    public float moveSpeed = 2f;
    public float cube1AdvanceDistance = 1f;
    
    [Header("UI")]
    public TMP_InputField commandInputField;
    public Button executeButton;
    
    [Header("Zone de détection UI")]
    [Tooltip("GameObject avec BoxCollider trigger pour activer UI")]
    public GameObject uiTriggerZone;
    
    [Header("Paramètres Physics")]
    public LayerMask pieceLayer;
    public float pushForce = 5f;
    
    [Header("Dépôt de Pièces")]
    [Tooltip("Types de pièces acceptés (assignez vos prefabs)")]
    public GameObject[] acceptedPiecePrefabs;
    
    [Tooltip("Distance pour déposer automatiquement")]
    public float autoDepositRange = 3f;
    
    [Tooltip("Référence au script du joueur")]
    public PickUpSimple playerPickup;
    
    private Transform[,] rackPositions;
    private bool isMoving = false;
    private Rigidbody rb1, rb2;
    private bool[,] occupiedPositions = new bool[2, 4];
    
    private GameObject loadedPiece;
    
    private float lastDepositTime = 0f;
    private float depositCooldown = 1f;
    
    private bool playerInUIZone = false;
    
    void Start()
    {
        rackPositions = new Transform[2, 4];
        
        rackPositions[0, 0] = position_1_1;
        rackPositions[0, 1] = position_1_2;
        rackPositions[0, 2] = position_1_3;
        rackPositions[0, 3] = position_1_4;
        
        rackPositions[1, 0] = position_2_1;
        rackPositions[1, 1] = position_2_2;
        rackPositions[1, 2] = position_2_3;
        rackPositions[1, 3] = position_2_4;
        
        rb1 = cube1.GetComponent<Rigidbody>();
        rb2 = cube2.GetComponent<Rigidbody>();
        
        if (rb1 == null || rb2 == null)
        {
            Debug.LogWarning("⚠️ Rigidbody manquant sur les cubes! Ajoutez des Rigidbody (Kinematic)");
        }
        
        if (executeButton != null)
        {
            executeButton.onClick.AddListener(OnExecuteButtonClicked);
        }
        
        HideUI();
        SetupUITriggerZone();
        EnsureCubesHaveColliders();
    }
    
    void Update()
    {
        if (!isMoving)
        {
            CheckAutoDepositByProximity();
        }
    }
    
    private void SetupUITriggerZone()
    {
        if (uiTriggerZone == null)
        {
            uiTriggerZone = new GameObject("UI_TriggerZone");
            uiTriggerZone.transform.SetParent(this.transform);
            uiTriggerZone.transform.localPosition = Vector3.zero;
            
            BoxCollider triggerCollider = uiTriggerZone.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(5f, 3f, 5f);
            
            Rigidbody triggerRb = uiTriggerZone.AddComponent<Rigidbody>();
            triggerRb.isKinematic = true;
            triggerRb.useGravity = false;
            
            UITriggerZoneHandler handler = uiTriggerZone.AddComponent<UITriggerZoneHandler>();
            handler.rackController = this;
            
            Debug.Log("✅ Zone de trigger UI créée automatiquement");
        }
        else
        {
            BoxCollider triggerCollider = uiTriggerZone.GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = uiTriggerZone.AddComponent<BoxCollider>();
                Debug.Log("✅ BoxCollider ajouté à la zone UI");
            }
            triggerCollider.isTrigger = true;
            
            if (uiTriggerZone.GetComponent<Rigidbody>() == null)
            {
                Rigidbody triggerRb = uiTriggerZone.AddComponent<Rigidbody>();
                triggerRb.isKinematic = true;
                triggerRb.useGravity = false;
            }
            
            UITriggerZoneHandler handler = uiTriggerZone.GetComponent<UITriggerZoneHandler>();
            if (handler == null)
            {
                handler = uiTriggerZone.AddComponent<UITriggerZoneHandler>();
            }
            handler.rackController = this;
        }
    }
    
    private void ShowUI()
    {
        if (commandInputField != null)
        {
            commandInputField.gameObject.SetActive(true);
        }
        
        if (executeButton != null)
        {
            executeButton.gameObject.SetActive(true);
        }
        
        Debug.Log("🎮 UI du Rack System affichée");
    }
    
    private void HideUI()
    {
        if (commandInputField != null)
        {
            commandInputField.gameObject.SetActive(false);
        }
        
        if (executeButton != null)
        {
            executeButton.gameObject.SetActive(false);
        }
        
        Debug.Log("🎮 UI du Rack System cachée");
    }
    
    public void OnPlayerEnterUIZone(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PickUpSimple>() != null)
        {
            playerInUIZone = true;
            ShowUI();
            Debug.Log("✅ Joueur entré dans la zone UI du Rack");
        }
    }
    
    public void OnPlayerExitUIZone(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PickUpSimple>() != null)
        {
            playerInUIZone = false;
            HideUI();
            Debug.Log("❌ Joueur sorti de la zone UI du Rack");
        }
    }
    
    private void EnsureCubesHaveColliders()
    {
        if (cube1.GetComponent<Collider>() == null)
        {
            BoxCollider col = cube1.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
            Debug.Log("✅ Collider ajouté à Cube1");
        }
        
        if (cube2.GetComponent<Collider>() == null)
        {
            BoxCollider col = cube2.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
            Debug.Log("✅ Collider ajouté à Cube2");
        }
    }
    
    private void CheckAutoDepositByProximity()
    {
        if (playerPickup == null) return;
        
        GameObject heldPiece = playerPickup.GetHeldObject();
        if (heldPiece == null) return;
        
        if (Time.time - lastDepositTime < depositCooldown) return;
        
        if (loadedPiece != null) return;
        
        float distanceToCube1 = Vector3.Distance(playerPickup.transform.position, cube1.position);
        float distanceToCube2 = Vector3.Distance(playerPickup.transform.position, cube2.position);
        
        float closestDistance = Mathf.Min(distanceToCube1, distanceToCube2);
        
        if (closestDistance <= autoDepositRange)
        {
            Debug.Log($"📏 Distance au cube: {closestDistance:F2}m - Dépôt automatique!");
            
            if (IsPieceAccepted(heldPiece))
            {
                DepositPieceToLoading(heldPiece);
                lastDepositTime = Time.time;
            }
            else
            {
                ShowFeedback("❌ Type de pièce non accepté!");
                lastDepositTime = Time.time;
            }
        }
    }
    
    private void TryDepositPieceOnCubeClick()
    {
    }
    
    private bool IsPieceAccepted(GameObject piece)
    {
        if (acceptedPiecePrefabs == null || acceptedPiecePrefabs.Length == 0)
        {
            Debug.LogWarning("⚠️ Aucun prefab accepté défini! Acceptation de toutes les pièces par défaut.");
            return true;
        }
        
        string pieceName = piece.name.Replace("(Clone)", "").Trim();
        
        foreach (GameObject prefab in acceptedPiecePrefabs)
        {
            if (prefab != null)
            {
                string prefabName = prefab.name.Trim();
                if (pieceName == prefabName)
                {
                    Debug.Log($"✅ Pièce acceptée: {pieceName}");
                    return true;
                }
            }
        }
        
        Debug.Log($"❌ Pièce refusée: {pieceName}");
        return false;
    }
    
    public void DepositPieceToLoading(GameObject piece)
    {
        if (piece == null)
        {
            Debug.LogError("❌ Erreur: piece est null!");
            return;
        }
        
        if (loadedPiece != null)
        {
            ShowFeedback("⚠️ Une pièce est déjà en attente dans loadingPosition!");
            return;
        }
        
        if (loadingPosition == null)
        {
            Debug.LogError("❌ loadingPosition n'est pas assignée!");
            ShowFeedback("Erreur: loadingPosition manquante!");
            return;
        }
        
        piece.transform.SetParent(null);
        
        piece.transform.SetParent(loadingPosition);
        piece.transform.localPosition = Vector3.zero;
        piece.transform.localRotation = Quaternion.identity;
        
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
            col.enabled = true;
        }
        
        loadedPiece = piece;
        
        if (playerPickup != null)
        {
            playerPickup.ReleaseHeldObject();
        }
        
        ShowFeedback($"✅ {piece.name} déposé dans loadingPosition!");
        
        Debug.Log($"📦 Pièce {piece.name} prête à être transportée vers le rack");
    }
    
    public GameObject GetLoadedPiece()
    {
        return loadedPiece;
    }
    
    public void ClearLoadedPiece()
    {
        loadedPiece = null;
    }
    
    public bool HasLoadedPiece()
    {
        return loadedPiece != null;
    }
    
    private void OnExecuteButtonClicked()
    {
        if (commandInputField == null)
        {
            Debug.LogError("Input Field n'est pas assigné!");
            return;
        }
        
        string command = commandInputField.text.Trim();
        
        if (string.IsNullOrEmpty(command))
        {
            ShowFeedback("Veuillez entrer une commande!");
            return;
        }
        
        ExecuteCommand(command);
    }
    
    public void ExecuteCommand(string command)
    {
        if (isMoving)
        {
            ShowFeedback("Système déjà en mouvement!");
            return;
        }
        
        if (loadedPiece == null)
        {
            ShowFeedback("⚠️ Aucune pièce dans loadingPosition!");
            return;
        }
        
        string[] parts = command.Split(':');
        if (parts.Length != 2)
        {
            ShowFeedback("Format invalide! Utilisez 'position:ligne' (ex: '4:2')");
            return;
        }
        
        int position, line;
        if (!int.TryParse(parts[0], out position) || !int.TryParse(parts[1], out line))
        {
            ShowFeedback("Les valeurs doivent être des nombres!");
            return;
        }
        
        if (position < 1 || position > 4 || line < 1 || line > 2)
        {
            ShowFeedback("Position: 1-4, Ligne: 1-2");
            return;
        }
        
        int posIndex = position - 1;
        int lineIndex = line - 1;
        
        Transform targetPosition = rackPositions[lineIndex, posIndex];
        
        if (targetPosition == null)
        {
            ShowFeedback($"Position {position}:{line} n'est pas configurée!");
            return;
        }
        
        if (occupiedPositions[lineIndex, posIndex])
        {
            ShowFeedback($"⚠️ Position {position}:{line} déjà occupée!");
            return;
        }
        
        ShowFeedback($"Déplacement vers position {position}:{line}...");
        StartCoroutine(MovementSequence(targetPosition, command, lineIndex, posIndex));
    }
    
    private void ShowFeedback(string message)
    {
        Debug.Log(message);
    }
    
    private IEnumerator MovementSequence(Transform targetPosition, string command, int lineIndex, int posIndex)
    {
        isMoving = true;
        
        if (executeButton != null)
        {
            executeButton.interactable = false;
        }
        
        Vector3 pieceOffset = Vector3.zero;
        Quaternion pieceRotationOffset = Quaternion.identity;
        
        if (loadedPiece != null)
        {
            Vector3 pieceWorldPos = loadedPiece.transform.position;
            Quaternion pieceWorldRot = loadedPiece.transform.rotation;
            
            loadedPiece.transform.SetParent(cube2, true);
            
            Debug.Log($"📦 Pièce {loadedPiece.name} attachée à cube2 pour transport");
        }
        
        yield return StartCoroutine(MoveBothCubesToTarget(targetPosition));
        
        ShowFeedback("Dépôt en cours...");
        yield return StartCoroutine(MoveCube2Forward());
        
        if (loadedPiece != null)
        {
            loadedPiece.transform.SetParent(null);
            
            loadedPiece.transform.position = targetPosition.position;
            loadedPiece.transform.rotation = targetPosition.rotation;
            
            Rigidbody pieceRb = loadedPiece.GetComponent<Rigidbody>();
            if (pieceRb != null)
            {
                pieceRb.isKinematic = false;
                pieceRb.useGravity = true;
                pieceRb.linearVelocity = Vector3.zero;
                pieceRb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log($"✅ Pièce {loadedPiece.name} déposée à la position {command}");
            ClearLoadedPiece();
        }
        
        yield return StartCoroutine(PushPieces());
        
        yield return new WaitForSeconds(0.5f);
        
        yield return StartCoroutine(MoveCube2Backward());
        
        yield return new WaitForSeconds(0.3f);
        
        ShowFeedback("Retour à la position zéro...");
        yield return StartCoroutine(MoveBothCubesToHome());
        
        occupiedPositions[lineIndex, posIndex] = true;
        
        ShowFeedback($"✅ Position {command} - Terminé!");
        
        if (executeButton != null)
        {
            executeButton.interactable = true;
        }
        
        if (commandInputField != null)
        {
            commandInputField.text = "";
        }
        
        isMoving = false;
        
        yield return new WaitForSeconds(2f);
        ShowFeedback("Entrez position:ligne (ex: 4:2)");
    }
    
    private IEnumerator MoveBothCubesToTarget(Transform target)
    {
        Vector3 startPos1 = cube1.position;
        Vector3 startPos2 = cube2.position;
        Vector3 targetPos = target.position;

        Vector3 intermediatePos1 = new Vector3(startPos1.x, startPos1.y, targetPos.z);
        Vector3 intermediatePos2 = new Vector3(startPos2.x, startPos2.y, targetPos.z);

        yield return StartCoroutine(MoveTwoToPositions(cube1, intermediatePos1, cube2, intermediatePos2));

        float deltaY = targetPos.y - intermediatePos1.y;
        Vector3 finalPos1 = new Vector3(intermediatePos1.x, intermediatePos1.y + deltaY, intermediatePos1.z);
        Vector3 finalPos2 = new Vector3(intermediatePos2.x, intermediatePos2.y + deltaY, intermediatePos2.z);

        yield return StartCoroutine(MoveTwoToPositions(cube1, finalPos1, cube2, finalPos2));
    }
    
    private IEnumerator MoveCube2Forward()
    {
        Vector3 targetPos = cube2.position + new Vector3(cube1AdvanceDistance, 0, 0);
        yield return StartCoroutine(MoveToPositionPhysics(cube2, rb2, targetPos));
    }
    
    private IEnumerator MoveCube2Backward()
    {
        Vector3 targetPos = cube2.position - new Vector3(cube1AdvanceDistance, 0, 0);
        yield return StartCoroutine(MoveToPositionPhysics(cube2, rb2, targetPos));
    }
    
    private IEnumerator PushPieces()
    {
        Collider[] hitColliders = Physics.OverlapBox(
            cube2.position + cube2.forward * 0.5f,
            new Vector3(0.5f, 0.5f, 0.5f),
            cube2.rotation,
            pieceLayer
        );
        
        foreach (Collider col in hitColliders)
        {
            Rigidbody pieceRb = col.GetComponent<Rigidbody>();
            if (pieceRb != null)
            {
                Vector3 pushDirection = (col.transform.position - cube2.position).normalized;
                pieceRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            }
        }
        
        yield return new WaitForSeconds(0.2f);
    }
    
    private IEnumerator MoveBothCubesToHome()
    {
        Vector3 homePos = homePosition.position;
        Vector3 startPos1 = cube1.position;
        Vector3 startPos2 = cube2.position;

        float deltaY = homePos.y - startPos1.y;
        Vector3 intermediatePos1 = new Vector3(startPos1.x, startPos1.y + deltaY, startPos1.z);
        Vector3 intermediatePos2 = new Vector3(startPos2.x, startPos2.y + deltaY, startPos2.z);

        yield return StartCoroutine(MoveTwoToPositions(cube1, intermediatePos1, cube2, intermediatePos2));

        Vector3 finalPos1 = new Vector3(intermediatePos1.x, intermediatePos1.y, homePos.z);
        Vector3 finalPos2 = new Vector3(intermediatePos2.x, intermediatePos2.y, homePos.z);

        yield return StartCoroutine(MoveTwoToPositions(cube1, finalPos1, cube2, finalPos2));
    }
    
    private IEnumerator MoveTwoToPositions(Transform a, Vector3 targetA, Transform b, Vector3 targetB)
    {
        Rigidbody rbA = a.GetComponent<Rigidbody>();
        Rigidbody rbB = b.GetComponent<Rigidbody>();
        
        while (Vector3.Distance(a.position, targetA) > 0.01f || Vector3.Distance(b.position, targetB) > 0.01f)
        {
            if (rbA != null && rbA.isKinematic)
            {
                rbA.MovePosition(Vector3.MoveTowards(a.position, targetA, moveSpeed * Time.deltaTime));
            }
            else
            {
                a.position = Vector3.MoveTowards(a.position, targetA, moveSpeed * Time.deltaTime);
            }
            
            if (rbB != null && rbB.isKinematic)
            {
                rbB.MovePosition(Vector3.MoveTowards(b.position, targetB, moveSpeed * Time.deltaTime));
            }
            else
            {
                b.position = Vector3.MoveTowards(b.position, targetB, moveSpeed * Time.deltaTime);
            }
            
            yield return null;
        }

        a.position = targetA;
        b.position = targetB;
    }

    private IEnumerator MoveToPositionPhysics(Transform obj, Rigidbody rb, Vector3 targetPosition)
    {
        while (Vector3.Distance(obj.position, targetPosition) > 0.01f)
        {
            if (rb != null && rb.isKinematic)
            {
                rb.MovePosition(Vector3.MoveTowards(obj.position, targetPosition, moveSpeed * Time.deltaTime));
            }
            else
            {
                obj.position = Vector3.MoveTowards(obj.position, targetPosition, moveSpeed * Time.deltaTime);
            }
            yield return null;
        }
        
        obj.position = targetPosition;
    }
    
    public void ResetOccupiedPositions()
    {
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                occupiedPositions[i, j] = false;
            }
        }
        ShowFeedback("Positions réinitialisées!");
    }
    
    public void ShowRackStatus()
    {
        string status = "=== État du Rack ===\n";
        for (int line = 0; line < 2; line++)
        {
            status += $"Ligne {line + 1}: ";
            for (int pos = 0; pos < 4; pos++)
            {
                status += occupiedPositions[line, pos] ? "[X] " : "[ ] ";
            }
            status += "\n";
        }
        Debug.Log(status);
        ShowFeedback("État affiché dans la console");
    }
}