using UnityEngine;

public class VirtualSensoor : MonoBehaviour
{
    public int pieceCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        // Détecte quand une pièce entre dans la zone
        if (other.CompareTag("Piece"))
        {
            pieceCount = Mathf.Max(0, pieceCount - 1); // Évite les valeurs négatives
            Debug.Log($"Pièce détectée ! Total : {pieceCount}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Détecte quand une pièce sort de la zone
        if (other.CompareTag("Piece"))
        {
            pieceCount--;
            Debug.Log($"Pièce retirée ! Total : {pieceCount}");
        }
    }
}