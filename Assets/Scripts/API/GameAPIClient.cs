using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ScoreData
{
    public string pseudo;
    public int score;
    public float timeCompleted;
}

[Serializable]
public class SaveScoreRequest
{
    public string action = "save_score";
    public List<ScoreData> scores;
}

[Serializable]
public class APIResponse
{
    public bool success;
    public string message;
    public string userId;
    public bool isAnonymous;
}

public class GameAPIClient : MonoBehaviour
{
    private const string API_BASE_URL = "http://localhost:3000";
    private AuthManager authManager;

    void Start()
    {
        authManager = FindObjectOfType<AuthManager>();
    }

    public IEnumerator SaveScore(string playerName, int score, float time)
    {
        if (!authManager.HasValidToken())
        {
            Debug.LogError("Pas de token d'authentification!");
            yield break;
        }

        SaveScoreRequest request = new SaveScoreRequest
        {
            scores = new List<ScoreData>
            {
                new ScoreData
                {
                    pseudo = playerName,
                    score = score,
                    timeCompleted = time
                }
            }
        };

        string jsonData = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(API_BASE_URL + "/api/game", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + authManager.GetToken());

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                APIResponse response = JsonUtility.FromJson<APIResponse>(www.downloadHandler.text);

                if (response.success)
                {
                    Debug.Log($"Score sauvegardé! UserId: {response.userId}, Anonymous: {response.isAnonymous}");
                }
                else
                {
                    Debug.LogError("Erreur lors de la sauvegarde du score");
                }
            }
            else if (www.responseCode == 401)
            {
                Debug.LogWarning("Token expiré, regénération...");
                yield return authManager.GetAnonymousToken();
                // Réessayer après avoir obtenu un nouveau token
                yield return SaveScore(playerName, score, time);
            }
            else
            {
                Debug.LogError($"Erreur réseau: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
            }
        }
    }
}