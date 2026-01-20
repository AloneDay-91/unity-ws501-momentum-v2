using UnityEngine;
using Anatidae;

public class ArcadeScoreTester : MonoBehaviour
{
    [Header("Test Settings")]
    public string testName = "TEST_PLAYER";
    public int testScore = 1500;

    [ContextMenu("Envoyer Score de Test")]
    public void SendTestScore()
    {
        if (AnatidaeArcadeClient.Instance == null)
        {
            Debug.LogError("Erreur: Le script AnatidaeArcadeClient n'est pas présent dans la scène ! Ajoutez-le à un GameObject.");
            return;
        }

        Debug.Log($"Envoi du score de test : {testName} - {testScore}...");
        
        StartCoroutine(AnatidaeArcadeClient.Instance.PostHighscore(testName, testScore, (success) =>
        {
            if (success)
            {
                Debug.Log("✅ SUCCÈS : Score de test enregistré sur la borne !");
                Debug.Log("Vérifiez maintenant l'URL : http://localhost:3000/api/?game=" + AnatidaeArcadeClient.Instance.gameName);
            }
            else
            {
                Debug.LogError("❌ ÉCHEC : Impossible d'envoyer le score. Vérifiez que le serveur node.js tourne et que le nom du jeu est correct.");
            }
        }));
    }
    
    [ContextMenu("Lire les Highscores")]
    public void ReadHighscores()
    {
        if (AnatidaeArcadeClient.Instance == null) return;

        StartCoroutine(AnatidaeArcadeClient.Instance.GetHighscores((scores) =>
        {
            Debug.Log($"Reçu {scores.Count} scores :");
            foreach (var s in scores)
            {
                Debug.Log($"- {s.name} : {s.score}");
            }
        }, 
        (error) => Debug.LogError(error)));
    }
}