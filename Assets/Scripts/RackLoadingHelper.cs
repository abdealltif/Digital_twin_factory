using UnityEngine;

/// <summary>
/// Helper pour déposer facilement des pièces dans loadingPosition
/// Touche P : Spawner et déposer automatiquement une pièce
/// </summary>
public class RackLoadingHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RackSystemController rackSystem;
    
    [Header("Test Pieces")]
    [SerializeField] private GameObject testPiecePrefab;
    
    void Update()
    {
        // Touche P : Spawner une pièce de test dans loadingPosition
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnPieceToLoading();
        }
    }
    
    void SpawnPieceToLoading()
    {
        if (rackSystem == null || rackSystem.loadingPosition == null)
        {
            Debug.LogError("❌ RackSystem ou loadingPosition non assigné");
            return;
        }
        
        if (testPiecePrefab == null)
        {
            Debug.LogError("❌ testPiecePrefab non assigné");
            return;
        }
        
        // Créer la pièce à loadingPosition
        Vector3 spawnPos = rackSystem.loadingPosition.position;
        GameObject piece = Instantiate(testPiecePrefab, spawnPos, Quaternion.identity);
        
        Debug.Log($"✅ Pièce {piece.name} créée à loadingPosition - Appuyez P pour plus");
    }
}
