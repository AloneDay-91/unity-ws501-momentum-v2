#if !WEB_BUILD
using UnityEngine;

/// <summary>
/// Gère le split screen pour le multijoueur local
/// Place ce script sur un GameObject vide dans ta scène (ex: "SplitScreenManager")
/// </summary>
public class SplitScreenManager : MonoBehaviour
{
    [Header("Caméras")]
    [Tooltip("Caméra du joueur 1 (trouvée automatiquement si vide)")]
    public Camera player1Camera;

    [Tooltip("Caméra du joueur 2 (trouvée automatiquement si vide)")]
    public Camera player2Camera;

    [Header("Configuration")]
    [Tooltip("Type de split screen")]
    public SplitScreenMode splitMode = SplitScreenMode.Vertical;

    [Tooltip("Activer le split screen au démarrage")]
    public bool enableOnStart = true;

    [Header("Bordure (optionnel)")]
    [Tooltip("Ajouter une bordure visuelle entre les écrans")]
    public bool showBorder = true;

    [Tooltip("Couleur de la bordure")]
    public Color borderColor = Color.black;

    [Tooltip("Épaisseur de la bordure (en pixels)")]
    public float borderThickness = 2f;

    public enum SplitScreenMode
    {
        Vertical,      // Gauche/Droite
        Horizontal     // Haut/Bas
    }

    void Start()
    {
        // Trouve les caméras automatiquement si pas assignées
        if (player1Camera == null || player2Camera == null)
        {
            FindCameras();
        }

        if (enableOnStart)
        {
            ApplySplitScreen();
        }
    }

    private void FindCameras()
    {
        // Cherche les caméras par leurs noms ou tags
        Camera[] allCameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in allCameras)
        {
            // Cherche par nom
            if (cam.name.Contains("J1") || cam.name.Contains("Player1") || cam.name.Contains("P1"))
            {
                player1Camera = cam;
            }
            else if (cam.name.Contains("J2") || cam.name.Contains("Player2") || cam.name.Contains("P2"))
            {
                player2Camera = cam;
            }
        }

        if (player1Camera == null || player2Camera == null)
        {
            Debug.LogWarning("SplitScreenManager: Impossible de trouver les 2 caméras automatiquement. Assigne-les manuellement dans l'inspecteur.");
        }
        else
        {
            Debug.Log($"SplitScreenManager: Caméras trouvées - {player1Camera.name} et {player2Camera.name}");
        }
    }

    /// <summary>
    /// Applique la configuration de split screen
    /// </summary>
    public void ApplySplitScreen()
    {
        if (player1Camera == null || player2Camera == null)
        {
            Debug.LogError("SplitScreenManager: Les caméras ne sont pas assignées !");
            return;
        }

        if (splitMode == SplitScreenMode.Vertical)
        {
            ApplyVerticalSplit();
        }
        else
        {
            ApplyHorizontalSplit();
        }

        Debug.Log($"Split screen appliqué : {splitMode}");
    }

    private void ApplyVerticalSplit()
    {
        // Joueur 1 : moitié gauche
        player1Camera.rect = new Rect(0, 0, 0.5f, 1);

        // Joueur 2 : moitié droite
        player2Camera.rect = new Rect(0.5f, 0, 0.5f, 1);
    }

    private void ApplyHorizontalSplit()
    {
        // Joueur 1 : moitié haute
        player1Camera.rect = new Rect(0, 0.5f, 1, 0.5f);

        // Joueur 2 : moitié basse
        player2Camera.rect = new Rect(0, 0, 1, 0.5f);
    }

    /// <summary>
    /// Désactive le split screen (plein écran pour player1Camera)
    /// </summary>
    public void DisableSplitScreen()
    {
        if (player1Camera != null)
        {
            player1Camera.rect = new Rect(0, 0, 1, 1);
        }

        if (player2Camera != null)
        {
            player2Camera.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Change le mode de split screen à la volée
    /// </summary>
    public void ToggleSplitMode()
    {
        splitMode = (splitMode == SplitScreenMode.Vertical)
            ? SplitScreenMode.Horizontal
            : SplitScreenMode.Vertical;

        ApplySplitScreen();
    }

    void OnGUI()
    {
        if (!showBorder || player1Camera == null || player2Camera == null) return;

        // Dessine une bordure entre les deux écrans
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, borderColor);
        texture.Apply();

        GUI.skin.box.normal.background = texture;

        if (splitMode == SplitScreenMode.Vertical)
        {
            // Ligne verticale au milieu
            float xPos = Screen.width / 2f - borderThickness / 2f;
            GUI.Box(new Rect(xPos, 0, borderThickness, Screen.height), GUIContent.none);
        }
        else
        {
            // Ligne horizontale au milieu
            float yPos = Screen.height / 2f - borderThickness / 2f;
            GUI.Box(new Rect(0, yPos, Screen.width, borderThickness), GUIContent.none);
        }
    }

    void OnValidate()
    {
        // Applique les changements en temps réel dans l'éditeur
        if (Application.isPlaying && enableOnStart)
        {
            ApplySplitScreen();
        }
    }
}
#endif
