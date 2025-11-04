using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScriptSwetcher : MonoBehaviour
{
    // InputField pour le nom
    public InputField nameInput_UI;           // InputField classique
    public TMP_InputField nameInput_TMP;      // TMP_InputField

    // Bouton Start
    public Button startButton;

    // ✅ MODIFIÉ : Seulement TMP pour l'erreur
    public TextMeshProUGUI errorText_TMP;     // TMP Text pour erreur

    // Nom de l'utilisateur à partager entre les scènes
    public static string PlayerName = "";

    // Cache pour éviter les répétitions
    private TMP_InputField activeInput;

    private void Start()
    {
        // Déterminer quel InputField est utilisé
        activeInput = nameInput_TMP != null ? nameInput_TMP : null;

        // Bouton toujours actif
        if (startButton != null)
            startButton.interactable = true;

        // Ajouter un listener pour cacher l'erreur quand l'utilisateur tape
        if (nameInput_UI != null)
            nameInput_UI.onValueChanged.AddListener(OnNameChanged);
        if (nameInput_TMP != null)
            nameInput_TMP.onValueChanged.AddListener(OnNameChanged);

        // Cacher le texte d'erreur au départ
        HideError();
    }

    private void OnNameChanged(string _)
    {
        // Cacher l'erreur dès que l'utilisateur tape
        HideError();
    }

    private string GetCurrentName()
    {
        if (activeInput != null)
            return activeInput.text.Trim();
        if (nameInput_UI != null)
            return nameInput_UI.text.Trim();
        return "";
    }

    // ✅ MODIFIÉ : Afficher l'erreur en TMP uniquement
    private void ShowError(string message)
    {
        if (errorText_TMP != null)
        {
            errorText_TMP.text = message;
            errorText_TMP.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ Error Text TMP non assigné !");
        }
    }

    // ✅ MODIFIÉ : Cacher l'erreur en TMP uniquement
    private void HideError()
    {
        if (errorText_TMP != null)
            errorText_TMP.gameObject.SetActive(false);
    }

    public void OnStartButton()
    {
        string name = GetCurrentName();

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter your name !");
            return;
        }

        PlayerName = name;
        Debug.Log($"Welcome {PlayerName}!");
        SceneManager.LoadScene("SampleScene");
    }

    public void OnExitButton()
    {
        Debug.Log("Vous avez quitté l'usine.");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDestroy()
    {
        if (nameInput_UI != null)
            nameInput_UI.onValueChanged.RemoveListener(OnNameChanged);
        if (nameInput_TMP != null)
            nameInput_TMP.onValueChanged.RemoveListener(OnNameChanged);
    }
}
