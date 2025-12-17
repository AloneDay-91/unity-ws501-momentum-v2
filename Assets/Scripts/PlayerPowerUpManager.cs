using UnityEngine;
using System.Collections;

/// <summary>
/// Gère les effets de power-ups actifs sur un joueur
/// S'attache au GameObject du joueur
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerPowerUpManager : MonoBehaviour
{
    [Header("Speed Boost Settings")]
    [Tooltip("Multiplicateur de vitesse pour le boost (ex: 1.5 = +50% de vitesse)")]
    public float speedBoostMultiplier = 1.5f;

    [Tooltip("Durée du speed boost en secondes")]
    public float speedBoostDuration = 5f;

    [Header("Speed Slow Settings")]
    [Tooltip("Multiplicateur de vitesse pour le ralentissement (ex: 0.5 = -50% de vitesse)")]
    public float speedSlowMultiplier = 0.5f;

    [Tooltip("Durée du ralentissement en secondes")]
    public float speedSlowDuration = 5f;

    [Header("Visual Feedback")]
    [Tooltip("Couleur de l'effet de boost (optionnel, pour un effet visuel)")]
    public Color boostColor = Color.cyan;

    [Tooltip("Couleur de l'effet de ralentissement (optionnel, pour un effet visuel)")]
    public Color slowColor = Color.red;

    // Références
    private PlayerMovement playerMovement;
    private PlayerInput playerInput;

    // État interne
    private float baseSpeed;
    private Coroutine activeSpeedBoostCoroutine;
    private Coroutine activeSpeedSlowCoroutine;

    // Propriétés publiques pour vérifier l'état
    public bool HasSpeedBoost { get; private set; }
    public bool HasSpeedSlow { get; private set; }

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();

        // On sauvegarde la vitesse de base
        baseSpeed = playerMovement.moveSpeed;
    }

    /// <summary>
    /// Applique un boost de vitesse au joueur
    /// </summary>
    public void ApplySpeedBoost()
    {
        // Si un boost est déjà actif, on le réinitialise
        if (activeSpeedBoostCoroutine != null)
        {
            StopCoroutine(activeSpeedBoostCoroutine);
        }

        activeSpeedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine());
    }

    /// <summary>
    /// Applique un ralentissement de vitesse au joueur
    /// </summary>
    public void ApplySpeedSlow()
    {
        // Si un ralentissement est déjà actif, on le réinitialise
        if (activeSpeedSlowCoroutine != null)
        {
            StopCoroutine(activeSpeedSlowCoroutine);
        }

        activeSpeedSlowCoroutine = StartCoroutine(SpeedSlowCoroutine());
    }

    private IEnumerator SpeedBoostCoroutine()
    {
        HasSpeedBoost = true;

        // On applique le boost
        playerMovement.moveSpeed = baseSpeed * speedBoostMultiplier;

        // Debug log
        Debug.Log($"Joueur {playerInput.playerID} : Speed Boost activé ! Vitesse = {playerMovement.moveSpeed}");

        // (Optionnel) Effet visuel - tu peux ajouter des particules ou changer la couleur ici
        // Exemple : GetComponentInChildren<Renderer>().material.color = boostColor;

        // On attend la durée
        yield return new WaitForSeconds(speedBoostDuration);

        // On remet la vitesse normale
        playerMovement.moveSpeed = baseSpeed;
        HasSpeedBoost = false;

        Debug.Log($"Joueur {playerInput.playerID} : Speed Boost terminé ! Vitesse = {playerMovement.moveSpeed}");

        activeSpeedBoostCoroutine = null;
    }

    private IEnumerator SpeedSlowCoroutine()
    {
        HasSpeedSlow = true;

        // On applique le ralentissement
        playerMovement.moveSpeed = baseSpeed * speedSlowMultiplier;

        // Debug log
        Debug.Log($"Joueur {playerInput.playerID} : Speed Slow activé ! Vitesse = {playerMovement.moveSpeed}");

        // (Optionnel) Effet visuel - tu peux ajouter des particules ou changer la couleur ici
        // Exemple : GetComponentInChildren<Renderer>().material.color = slowColor;

        // On attend la durée
        yield return new WaitForSeconds(speedSlowDuration);

        // On remet la vitesse normale
        playerMovement.moveSpeed = baseSpeed;
        HasSpeedSlow = false;

        Debug.Log($"Joueur {playerInput.playerID} : Speed Slow terminé ! Vitesse = {playerMovement.moveSpeed}");

        activeSpeedSlowCoroutine = null;
    }

    /// <summary>
    /// Annule tous les effets actifs (utile si le joueur meurt ou change de niveau)
    /// </summary>
    public void ClearAllEffects()
    {
        if (activeSpeedBoostCoroutine != null)
        {
            StopCoroutine(activeSpeedBoostCoroutine);
            activeSpeedBoostCoroutine = null;
        }

        if (activeSpeedSlowCoroutine != null)
        {
            StopCoroutine(activeSpeedSlowCoroutine);
            activeSpeedSlowCoroutine = null;
        }

        playerMovement.moveSpeed = baseSpeed;
        HasSpeedBoost = false;
        HasSpeedSlow = false;
    }

    void OnDisable()
    {
        // On nettoie les effets si le joueur est désactivé
        ClearAllEffects();
    }
}
