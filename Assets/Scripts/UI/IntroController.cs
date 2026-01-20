using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class IntroController : MonoBehaviour
{
    [Header("Settings")]
    public string nextSceneName = "MainMenu"; // Le nom exact de votre scène de menu
    public float delayAfterVideo = 0.5f;
    public bool allowSkip = true;

    [Header("References")]
    public VideoPlayer videoPlayer;
    public RawImage displayImage; // L'image qui affiche la vidéo

    private bool hasStartedLoading = false;

    void Start()
    {
        // Configuration automatique si non assigné
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        
        // Prépare l'événement de fin de vidéo
        if (videoPlayer != null)
        {
            // On s'assure que la vidéo joue dans la RawImage si configuré
            if (displayImage != null && videoPlayer.renderMode == VideoRenderMode.RenderTexture)
            {
                // Si vous utilisez une Render Texture, assurez-vous de l'assigner ici ou dans l'inspecteur
            }
            else if (displayImage != null)
            {
                // Mode simple: Camera Near Plane ou Overlay est souvent plus simple pour les intros 2D
                // Mais si on veut mapper sur l'UI :
                videoPlayer.renderMode = VideoRenderMode.APIOnly;
                videoPlayer.prepareCompleted += (source) => {
                    displayImage.texture = source.texture;
                    source.Play();
                };
            }

            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Prepare();
        }
        else
        {
            // Pas de vidéo ? On lance direct
            LoadNextScene();
        }
    }

    void Update()
    {
        if (hasStartedLoading) return;

        // Skip avec n'importe quelle touche ou clic (Boutons Arcade)
        if (allowSkip && Input.anyKeyDown)
        {
            LoadNextScene();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Invoke("LoadNextScene", delayAfterVideo);
    }

    void LoadNextScene()
    {
        if (hasStartedLoading) return;
        hasStartedLoading = true;

        SceneManager.LoadScene(nextSceneName);
    }
}