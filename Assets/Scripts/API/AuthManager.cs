using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Anatidae;

[Serializable]
public class AnonymousAuthRequest
{
    public string deviceId;
    public string gameVersion;
}

[Serializable]
public class AnonymousAuthResponse
{
    public bool success;
    public string token;
    public int expiresIn;
    public string message;
}

public class AuthManager : MonoBehaviour
{
    private const string API_BASE_URL = "http://localhost:3000";
    private const string TOKEN_PREFS_KEY = "AnonymousToken";
    private const string DEVICE_ID_KEY = "DeviceId";

    private string authToken;

    void Start()
    {
        // Charger le token sauvegardé s'il existe
        authToken = PlayerPrefs.GetString(TOKEN_PREFS_KEY, "");

        if (string.IsNullOrEmpty(authToken))
        {
            // Pas de token, en générer un nouveau
            StartCoroutine(GetAnonymousToken());
        }
        else
        {
            Debug.Log("Token chargé depuis PlayerPrefs");
        }
    }

    private string GetOrCreateDeviceId()
    {
        string deviceId = PlayerPrefs.GetString(DEVICE_ID_KEY, "");

        if (string.IsNullOrEmpty(deviceId))
        {
            // Générer un nouveau deviceId unique
#if UNITY_ANDROID || UNITY_IOS
            deviceId = SystemInfo.deviceUniqueIdentifier;
#else
            deviceId = Guid.NewGuid().ToString();
#endif
            PlayerPrefs.SetString(DEVICE_ID_KEY, deviceId);
            PlayerPrefs.Save();
        }

        return deviceId;
    }

    public IEnumerator GetAnonymousToken()
    {
        string deviceId = GetOrCreateDeviceId();

        AnonymousAuthRequest request = new AnonymousAuthRequest
        {
            deviceId = deviceId,
            gameVersion = Application.version
        };

        string jsonData = JsonUtility.ToJson(request);

        // Utilise AnatidaeProxyWebRequest pour contourner CORS en WebGL
        using (UnityWebRequest www = AnatidaeProxyWebRequest.Post(API_BASE_URL + "/api/unity/auth", jsonData, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AnonymousAuthResponse response = JsonUtility.FromJson<AnonymousAuthResponse>(www.downloadHandler.text);

                if (response.success)
                {
                    authToken = response.token;
                    PlayerPrefs.SetString(TOKEN_PREFS_KEY, authToken);
                    PlayerPrefs.Save();

                    Debug.Log("Token anonyme obtenu avec succès!");
                    Debug.Log($"Device ID: {deviceId}");
                    Debug.Log($"Token expire dans: {response.expiresIn}s");
                }
                else
                {
                    Debug.LogError("Erreur lors de l'obtention du token");
                }
            }
            else
            {
                Debug.LogError($"Erreur réseau: {www.error}");
                Debug.LogError($"Code: {www.responseCode}");
            }
        }
    }

    public string GetToken()
    {
        return authToken;
    }

    public bool HasValidToken()
    {
        return !string.IsNullOrEmpty(authToken);
    }
}