using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Affiche le pseudo et le score d'un joueur dans l'UI
/// Ajoute des effets "juice" quand le score augmente
/// </summary>
public class PlayerScoreUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ID du joueur (1 ou 2)")]
    public int playerID = 1;

    [Tooltip("TextMeshPro pour afficher le pseudo")]
    public TMP_Text pseudoText;

    [Tooltip("TextMeshPro pour afficher le score")]
    public TMP_Text scoreText;

    [Header("Juice Settings")]
    [Tooltip("Scale maximum lors du gain de points")]
    [Range(1f, 2f)]
    public float maxScale = 1.3f;

    [Tooltip("Durée de l'animation de scale (secondes)")]
    [Range(0.1f, 1f)]
    public float scaleDuration = 0.3f;

    [Tooltip("Couleur du flash lors du gain de points")]
    public Color flashColor = Color.yellow;

    [Tooltip("Durée du flash de couleur (secondes)")]
    [Range(0.1f, 1f)]
    public float flashDuration = 0.2f;

    [Tooltip("Activer le shake effect")]
    public bool enableShake = true;

    [Tooltip("Intensité du shake")]
    [Range(0f, 20f)]
    public float shakeIntensity = 5f;

    [Tooltip("Durée du shake (secondes)")]
    [Range(0.1f, 0.5f)]
    public float shakeDuration = 0.15f;

    [Header("Sound")]
    [Tooltip("Nom du son à jouer lors du gain de points")]
    public string scoreSoundName = "scoreGain";

    [Tooltip("Jouer un son lors du gain de points")]
    public bool playSoundOnScore = true;

    // État interne
    private int currentScore = 0;
    private Color originalScoreColor;
    private Vector3 originalScoreScale;
    private Vector3 originalScorePosition;
    private bool isAnimating = false;
    private bool isEliminated = false;

    void Start()
    {
        // Initialise les valeurs par défaut
        if (scoreText != null)
        {
            originalScoreColor = scoreText.color;
            originalScoreScale = scoreText.transform.localScale;
            originalScorePosition = scoreText.transform.localPosition;
        }

        // Charge le pseudo du joueur
        UpdatePlayerName();

        // Initialise le score à 0
        UpdateScoreDisplay(0);

        // S'abonne aux changements de score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        }
    }

    void OnDestroy()
    {
        // Désabonne
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }

    /// <summary>
    /// Met à jour le pseudo du joueur
    /// </summary>
    private void UpdatePlayerName()
    {
        if (pseudoText == null) return;

        string playerName = $"Joueur {playerID}";

        // Tente de récupérer depuis PlayerNameManager
        if (PlayerNameManager.Instance != null)
        {
            playerName = PlayerNameManager.Instance.GetPlayerName(playerID);
        }

        // Tente de récupérer depuis GameSessionManager si disponible
        if (GameSessionManager.Instance != null)
        {
            if (playerID == 1 && !string.IsNullOrEmpty(GameSessionManager.Instance.player1Pseudo))
            {
                playerName = GameSessionManager.Instance.player1Pseudo;
            }
            else if (playerID == 2 && !string.IsNullOrEmpty(GameSessionManager.Instance.player2Pseudo))
            {
                playerName = GameSessionManager.Instance.player2Pseudo;
            }
        }

        pseudoText.text = playerName;
    }

    /// <summary>
    /// Appelé quand le score d'un joueur change
    /// </summary>
    private void OnScoreChanged(int changedPlayerID, int newScore)
    {
        if (changedPlayerID != playerID) return;

        int scoreDifference = newScore - currentScore;
        currentScore = newScore;

        UpdateScoreDisplay(newScore);

        // Lance les effets "juice" seulement si le score a augmenté
        if (scoreDifference > 0)
        {
            StartCoroutine(PlayScoreGainEffects());
        }
    }

    /// <summary>
    /// Met à jour l'affichage du score
    /// </summary>
    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = isEliminated ? "✖" : score.ToString();
        }
    }

    /// <summary>
    /// Appelée quand le joueur surveillé est éliminé : le score est remplacé par une croix.
    /// Hook public utilisable depuis GameManager.OnPlayerEliminated.
    /// </summary>
    public void MarkEliminated()
    {
        if (isEliminated) return;
        isEliminated = true;
        if (scoreText != null)
        {
            scoreText.color = originalScoreColor;
            scoreText.transform.localScale = originalScoreScale;
            scoreText.transform.localPosition = originalScorePosition;
            scoreText.text = "✖";
        }
    }

    /// <summary>
    /// Joue tous les effets "juice" lors du gain de points
    /// </summary>
    private IEnumerator PlayScoreGainEffects()
    {
        if (isAnimating || scoreText == null) yield break;

        isAnimating = true;

        // Joue le son
        if (playSoundOnScore && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(scoreSoundName);
        }

        // Lance les effets en parallèle
        Coroutine scaleCoroutine = StartCoroutine(ScaleEffect());
        Coroutine flashCoroutine = StartCoroutine(FlashEffect());
        Coroutine shakeCoroutine = null;

        if (enableShake)
        {
            shakeCoroutine = StartCoroutine(ShakeEffect());
        }

        // Attend que tous les effets soient terminés
        yield return scaleCoroutine;
        yield return flashCoroutine;
        if (shakeCoroutine != null)
        {
            yield return shakeCoroutine;
        }

        isAnimating = false;
    }

    /// <summary>
    /// Effet de scale : le texte grossit puis revient à la normale
    /// </summary>
    private IEnumerator ScaleEffect()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScoreScale;
        Vector3 targetScale = originalScoreScale * maxScale;

        // Scale up
        while (elapsed < scaleDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (scaleDuration / 2f);
            scoreText.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < scaleDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (scaleDuration / 2f);
            scoreText.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
            yield return null;
        }

        // Assure que le scale est exactement à l'original
        scoreText.transform.localScale = originalScoreScale;
    }

    /// <summary>
    /// Effet de flash : le texte change de couleur puis revient à la normale
    /// </summary>
    private IEnumerator FlashEffect()
    {
        float elapsed = 0f;

        // Flash vers la couleur cible
        while (elapsed < flashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration / 2f);
            scoreText.color = Color.Lerp(originalScoreColor, flashColor, t);
            yield return null;
        }

        elapsed = 0f;

        // Retour à la couleur originale
        while (elapsed < flashDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration / 2f);
            scoreText.color = Color.Lerp(flashColor, originalScoreColor, t);
            yield return null;
        }

        // Assure que la couleur est exactement à l'original
        scoreText.color = originalScoreColor;
    }

    /// <summary>
    /// Effet de shake : le texte tremble
    /// </summary>
    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Calcule un offset aléatoire
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float y = Random.Range(-shakeIntensity, shakeIntensity);

            // Applique l'offset avec une atténuation progressive
            float intensity = 1f - (elapsed / shakeDuration);
            scoreText.transform.localPosition = originalScorePosition + new Vector3(x, y, 0) * intensity;

            yield return null;
        }

        // Remet la position exactement à l'original
        scoreText.transform.localPosition = originalScorePosition;
    }

    /// <summary>
    /// Force la mise à jour du pseudo (utile quand les pseudos sont chargés après le Start)
    /// </summary>
    public void RefreshPlayerName()
    {
        UpdatePlayerName();
    }

    /// <summary>
    /// Force la mise à jour du score
    /// </summary>
    public void RefreshScore()
    {
        if (ScoreManager.Instance != null)
        {
            int score = ScoreManager.Instance.GetPlayerScore(playerID);
            UpdateScoreDisplay(score);
        }
    }
}
