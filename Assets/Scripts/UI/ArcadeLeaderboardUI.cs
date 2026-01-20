using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Anatidae;

public class ArcadeLeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Le conteneur où les lignes de score seront instanciées")]
    public Transform scoreContainer;
    
    [Tooltip("Le prefab d'une ligne de score (doit avoir un script avec 2 TMP_Text : Name et Score)")]
    public GameObject scoreEntryPrefab;

    [Tooltip("Message à afficher si chargement ou vide")]
    public TMP_Text statusText;

    void Start()
    {
        // Optionnel : charger au démarrage
        // RefreshScores();
    }

    public void RefreshScores()
    {
        if (statusText) statusText.text = "Chargement...";
        
        // Nettoyer l'existant
        foreach (Transform child in scoreContainer)
        {
            Destroy(child.gameObject);
        }

        if (AnatidaeArcadeClient.Instance == null)
        {
            if (statusText) statusText.text = "Erreur: Client introuvable";
            return;
        }

        StartCoroutine(AnatidaeArcadeClient.Instance.GetHighscores(OnScoresReceived, OnError));
    }

    private void OnScoresReceived(List<ArcadeHighscore> scores)
    {
        if (scores.Count == 0)
        {
            if (statusText) statusText.text = "Aucun score enregistré.";
            return;
        }

        if (statusText) statusText.text = ""; // Cacher le statut

        int rank = 1;
        foreach (var score in scores)
        {
            GameObject entry = Instantiate(scoreEntryPrefab, scoreContainer);
            
            // On essaie de trouver des composants TextMeshPro
            // Vous pouvez adapter cette partie selon votre Prefab
            var texts = entry.GetComponentsInChildren<TMP_Text>();
            
            if (texts.Length >= 2)
            {
                // Supposition : Le premier texte est le Nom, le deuxième le Score
                // Ou cherchez par nom : entry.transform.Find("NameText").GetComponent<TMP_Text>() ...
                texts[0].text = $"{rank}. {score.name}";
                texts[1].text = score.score.ToString();
            }
            else if (texts.Length == 1)
            {
                texts[0].text = $"{rank}. {score.name} - {score.score}";
            }

            rank++;
        }
    }

    private void OnError(string error)
    {
        Debug.LogError(error);
        if (statusText) statusText.text = "Erreur de connexion.";
    }
}