using UnityEngine;

/// <summary>
/// Script à mettre sur un objet PARENT de la caméra (CameraRig)
/// Ce rig suit le joueur, la caméra enfant peut ensuite shake librement
/// </summary>
public class CameraRigFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Le joueur à suivre")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Position relative par rapport au joueur")]
    public Vector3 offset = new Vector3(0, 3, -10);

    [Header("Smoothing")]
    [Tooltip("Active le lissage du mouvement")]
    public bool enableSmoothing = false;

    [Tooltip("Vitesse de lissage (plus élevé = plus rapide)")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (enableSmoothing)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }
    }
}
