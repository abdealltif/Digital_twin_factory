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

    // Texte pour afficher les messages d'erreur
    public Text errorText_UI;                 // UI Text classique
    public TextMeshProUGUI errorText_TMP;    // TMP Text

    // Nom de l'utilisateur à partager entre les scènes
    public static string PlayerName = "";

    private void Start()
    {
        // Vérifie si le Start button doit être actif au départ
        UpdateStartButtonState();

        // Ajouter un listener pour détecter les changements dans l'InputField
        if (nameInput_UI != null)
            nameInput_UI.onValueChanged.AddListener(delegate { UpdateStartButtonState(); });
        if (nameInput_TMP != null)
            nameInput_TMP.onValueChanged.AddListener(delegate { UpdateStartButtonState(); });

        // Cacher le texte d'erreur au départ
        if (errorText_UI != null) errorText_UI.gameObject.SetActive(false);
        if (errorText_TMP != null) errorText_TMP.gameObject.SetActive(false);
    }

    // Vérifie si le Start button doit être actif
    private void UpdateStartButtonState()
    {
        string currentName = "";

        if (nameInput_TMP != null)
            currentName = nameInput_TMP.text.Trim();
        else if (nameInput_UI != null)
            currentName = nameInput_UI.text.Trim();

        bool hasName = !string.IsNullOrEmpty(currentName);

        if (startButton != null)
            startButton.interactable = hasName;

        // Cacher le message d'erreur si le nom est saisi
        if (hasName)
        {
            if (errorText_UI != null) errorText_UI.gameObject.SetActive(false);
            if (errorText_TMP != null) errorText_TMP.gameObject.SetActive(false);
        }
    }

    // Méthode pour le bouton Start
    public void OnStartButton()
    {
        string name = "";

        if (nameInput_TMP != null)
            name = nameInput_TMP.text.Trim();
        else if (nameInput_UI != null)
            name = nameInput_UI.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            // Affiche le message d'erreur
            if (errorText_UI != null)
            {
                errorText_UI.text = "Enter your name";
                errorText_UI.gameObject.SetActive(true);
            }
            if (errorText_TMP != null)
            {
                errorText_TMP.text = "Enter your name";
                errorText_TMP.gameObject.SetActive(true);
            }
            return;
        }

        PlayerName = name;
        SceneManager.LoadScene("SampleScene"); // Charge la scène SampleScene
    }

    // Méthode pour le bouton Exit
    public void OnExitButton()
    {
        Debug.Log("Vous avez quitté l'usine.");
        // Si tu veux fermer l'application dans un build :
        // Application.Quit();
    }
}
