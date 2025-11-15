using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class setVideoURL : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("Utilisé pour les builds standalone (non-WebGL)")]
    [SerializeField] private VideoClip videoClip;

    [Tooltip("Nom du fichier vidéo dans StreamingAssets (ex: menu_background.mp4)")]
    [SerializeField] private string videoFileName = "menu_background.mp4";

    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private RawImage rawImage;

    private VideoPlayer videoPlayer;

    void Start()
    {
        // Récupérer ou ajouter le VideoPlayer
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // Configuration du VideoPlayer
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.isLooping = true;

        // Assigner la RenderTexture
        if (renderTexture != null)
        {
            videoPlayer.targetTexture = renderTexture;

            // Assigner la RenderTexture au RawImage
            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
            }
        }
        else
        {
            Debug.LogError("RenderTexture non assignée dans l'inspecteur !");
        }

        // Déterminer le mode : VideoClip (standalone) ou URL (WebGL/StreamingAssets)
#if UNITY_WEBGL
        // En WebGL, utiliser une URL vers StreamingAssets
        videoPlayer.source = VideoSource.Url;
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        videoPlayer.url = videoPath;
        Debug.Log($"Mode WebGL - Chargement vidéo depuis : {videoPath}");
#else
        // Pour les autres plateformes, utiliser VideoClip si disponible, sinon StreamingAssets
        if (videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            Debug.Log("Mode VideoClip");
        }
        else
        {
            // Fallback sur StreamingAssets si VideoClip non assigné
            videoPlayer.source = VideoSource.Url;
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
            videoPlayer.url = videoPath;
            Debug.Log($"Mode URL - Chargement vidéo depuis : {videoPath}");
        }
#endif

        // Préparer et jouer la vidéo
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("Vidéo préparée, lecture en cours...");
        source.Play();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}
