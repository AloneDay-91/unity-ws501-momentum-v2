using UnityEngine;

/// <summary>
/// Suit le joueur en gardant un offset constant
/// Compatible avec CameraShake - utilise localPosition pour permettre le shake
/// </summary>
public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; // Le joueur à suivre
    public Vector3 offset = new Vector3(0, 3, -10); // Position de la caméra par rapport au joueur

    [Header("Smoothing")]
    [Tooltip("Active le lissage du mouvement (optionnel)")]
    public bool enableSmoothing = false;

    [Tooltip("Vitesse de lissage (plus élevé = plus rapide)")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;

    private CameraShake cameraShake;
    private Vector3 desiredPosition;

    void Start()
    {
        // Récupère le composant CameraShake s'il existe
        cameraShake = GetComponent<CameraShake>();
    }

    // LateUpdate est appelé après tous les Update(),
    // c'est le meilleur endroit pour une caméra.
    void LateUpdate()
    {
        if (target != null)
        {
            // Calcule la position souhaitée
            desiredPosition = target.position + offset;

            // Applique le lissage si activé
            if (enableSmoothing)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = desiredPosition;
            }

            // Met à jour la position d'origine du shake après le mouvement
            // Cela permet au shake de fonctionner correctement
            if (cameraShake != null)
            {
                cameraShake.UpdateOriginalPosition();
            }
        }
    }
}