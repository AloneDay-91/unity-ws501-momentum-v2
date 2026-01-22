using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class TutorialStep
{
    public string stepName;
    public string title;
    [TextArea(3, 10)] 
    public string description;
    [TextArea(3, 10)]
    public string description1;
    public Sprite illustration;
}

public class HowToPlayPageUI : MonoBehaviour
{
    [Header("Configuration UI")]
    public Image illustrationImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI description1Text;
    public TextMeshProUGUI pageCounter;

    [Header("Navigation")]
    // Ce sont juste des GameObjects (Images), pas besoin de boutons cliquables
    public GameObject leftArrow; 
    public GameObject rightArrow;
    [Tooltip("Le bouton UI pour revenir au menu")]
    public Button backButton;

    [Header("Données")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Réglages Input (Joystick)")]
    [Tooltip("Nom exact de l'axe dans l'Input Manager")]
    public string inputAxisP1 = "P1_Horizontal"; 
    public string inputAxisP2 = "P2_Horizontal";

    [Tooltip("À quel point il faut pousser le stick (0.1 à 1.0)")]
    public float threshold = 0.5f; 

    [Tooltip("Temps d'attente entre deux pages (en secondes)")]
    public float cooldownDuration = 0.3f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private int currentIndex = 0;
    private float currentCooldown = 0f; // Timer actuel

    void OnEnable()
    {
        currentIndex = 0;
        currentCooldown = 0f; // Reset du timer à l'ouverture

        // Abonnement au clic du bouton retour
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        UpdateUI();
    }

    void OnDisable()
    {
        // Nettoyage de l'abonnement
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 1. Si on est en cooldown, on attend
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            return; 
        }

        // 2. Lecture des Inputs (P1 ou P2)
        float inputVal = 0f;

        try 
        {
            float p1 = Input.GetAxisRaw(inputAxisP1);
            float p2 = 0f;
            
            // On tente de lire P2, si ça échoue on ignore
            try { p2 = Input.GetAxisRaw(inputAxisP2); } catch {}

            // On garde l'input le plus fort
            if (Mathf.Abs(p1) > Mathf.Abs(p2)) inputVal = p1;
            else inputVal = p2;
        }
        catch (System.ArgumentException)
        {
            if (showDebugLogs) Debug.LogWarning($"Erreur Input : Vérifiez les noms '{inputAxisP1}'/'{inputAxisP2}' dans Project Settings.");
            return;
        }

        // 3. Vérification du seuil (Threshold)
        if (Mathf.Abs(inputVal) > threshold)
        {
            if (inputVal > 0) // Vers la DROITE
            {
                NextStep();
            }
            else // Vers la GAUCHE
            {
                PreviousStep();
            }

            // On active le cooldown pour ne pas sauter 10 pages d'un coup
            currentCooldown = cooldownDuration;
        }
    }

    public void NextStep()
    {
        if (steps.Count == 0) return;
        
        if (currentIndex < steps.Count - 1)
        {
            currentIndex++;
            if (showDebugLogs) Debug.Log($"Page Suivante : {currentIndex}");
            UpdateUI();
        }
    }

    public void PreviousStep()
    {
        if (steps.Count == 0) return;
        
        if (currentIndex > 0)
        {
            currentIndex--;
            if (showDebugLogs) Debug.Log($"Page Précédente : {currentIndex}");
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (steps.Count == 0) return;

        TutorialStep currentStep = steps[currentIndex];

        // Textes
        if (titleText != null) titleText.text = currentStep.title;
        if (descriptionText != null) descriptionText.text = currentStep.description;
        if (description1Text != null) description1Text.text = currentStep.description1;
        if (pageCounter != null) pageCounter.text = $"{currentIndex + 1} / {steps.Count}";

        // Illustrations
        if (illustrationImage != null)
        {
            illustrationImage.sprite = currentStep.illustration;
            illustrationImage.gameObject.SetActive(currentStep.illustration != null);
        }

        // Flèches visuelles
        if (leftArrow != null) leftArrow.SetActive(currentIndex > 0);
        if (rightArrow != null) rightArrow.SetActive(currentIndex < steps.Count - 1);
    }

    // Fonction appelée lors du clic sur le bouton retour
    private void OnBackButtonClicked()
    {
        if (showDebugLogs) Debug.Log("Tuto: Retour au menu précédent.");

        if (MenuPageManager.Instance != null)
        {
            MenuPageManager.Instance.ShowHomePage();
        }
    }
}