using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Anatidae
{
    [Serializable]
    public class ArcadeHighscore
    {
        public string name;
        public int score;
        public long timestamp;
    }

    [Serializable]
    public class ArcadeHighscoreResponse
    {
        public List<ArcadeHighscore> highscores;
    }

    public class AnatidaeArcadeClient : MonoBehaviour
    {
        public static AnatidaeArcadeClient Instance { get; private set; }

        [Header("Configuration Borne")]
        public string arcadeBaseUrl = "http://localhost:3000";
        public string gameName = "momentum"; // Doit correspondre au nom du dossier dans public/

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Récupère les highscores depuis la borne d'arcade locale
        /// </summary>
        public IEnumerator GetHighscores(Action<List<ArcadeHighscore>> onSuccess, Action<string> onError)
        {
            string url = $"{arcadeBaseUrl}/api/?game={gameName}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        // L'API retourne directement un objet contenant un tableau "highscores"
                        ArcadeHighscoreResponse response = JsonUtility.FromJson<ArcadeHighscoreResponse>(json);
                        
                        // Si JsonUtility échoue car le root est différent, on peut essayer de wrapper manuellement
                        // Mais selon la doc: Returns { highscores: [...] } donc ArcadeHighscoreResponse est correct.
                        
                        if (response != null && response.highscores != null)
                        {
                            onSuccess?.Invoke(response.highscores);
                        }
                        else
                        {
                            // Cas où l'API retournerait vide ou un format inattendu
                             onError?.Invoke("Format de réponse inattendu ou pas de highscores.");
                        }
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke("Erreur de parsing JSON: " + e.Message);
                    }
                }
                else
                {
                    onError?.Invoke("Erreur arcade: " + request.error);
                }
            }
        }

        /// <summary>
        /// Sauvegarde un score sur la borne d'arcade locale
        /// </summary>
        public IEnumerator PostHighscore(string name, int score, Action<bool> onComplete)
        {
            string url = $"{arcadeBaseUrl}/api/?game={gameName}";
            
            // Formatage Arcade : 3 lettres maximum, en majuscules
            string formattedName = name;
            if (formattedName.Length > 3)
            {
                formattedName = formattedName.Substring(0, 3);
            }
            formattedName = formattedName.ToUpper();

            // Création d'un JSON simple manuellement
            string json = "{\"name\":\"" + formattedName + "\",\"score\":" + score + "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError("Erreur sauvegarde score borne: " + request.error);
                    onComplete?.Invoke(false);
                }
            }
        }
    }
}