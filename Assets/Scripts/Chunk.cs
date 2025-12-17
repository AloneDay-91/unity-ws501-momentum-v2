using UnityEngine;

/// <summary>
/// Représente un morceau de map (chunk) qui peut être activé/désactivé
/// Attache ce script à chaque morceau de ta map
/// </summary>
public class Chunk : MonoBehaviour
{
    [Header("Chunk Info")]
    [Tooltip("Position X de ce chunk (utilisé pour déterminer l'ordre)")]
    public float chunkPosition;

    [Tooltip("Distance à laquelle ce chunk doit être activé (override la distance globale si > 0)")]
    public float customActivationDistance = 0f;

    [Header("Debug")]
    [Tooltip("Afficher des gizmos pour visualiser ce chunk")]
    public bool showDebugGizmos = true;

    [Tooltip("Couleur du gizmo")]
    public Color gizmoColor = Color.green;

    // État interne
    private bool isActivated = true;

    void Awake()
    {
        // Si pas de position définie, on utilise la position X de l'objet
        if (chunkPosition == 0f)
        {
            chunkPosition = transform.position.x;
        }
    }

    /// <summary>
    /// Active ce chunk (rend tous les enfants visibles et actifs)
    /// </summary>
    public void Activate()
    {
        if (!isActivated)
        {
            gameObject.SetActive(true);
            isActivated = true;
            // Debug.Log($"Chunk à position {chunkPosition} activé");
        }
    }

    /// <summary>
    /// Désactive ce chunk (économise des ressources)
    /// </summary>
    public void Deactivate()
    {
        if (isActivated)
        {
            gameObject.SetActive(false);
            isActivated = false;
            // Debug.Log($"Chunk à position {chunkPosition} désactivé");
        }
    }

    /// <summary>
    /// Vérifie si ce chunk est actuellement activé
    /// </summary>
    public bool IsActivated => isActivated;

    /// <summary>
    /// Obtient la position de ce chunk
    /// </summary>
    public float GetPosition()
    {
        return chunkPosition;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Dessine un cube représentant la zone du chunk
        Gizmos.color = isActivated ? gizmoColor : Color.red;

        // Calcule les bounds du chunk
        Bounds bounds = CalculateBounds();

        // Dessine le cube
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Dessine une ligne verticale pour marquer la position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(chunkPosition, bounds.min.y, 0),
            new Vector3(chunkPosition, bounds.max.y, 0)
        );
    }

    private Bounds CalculateBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Si pas de renderer, utilise une taille par défaut
        if (renderers.Length == 0)
        {
            bounds = new Bounds(transform.position, new Vector3(10, 10, 10));
        }

        return bounds;
    }
}
