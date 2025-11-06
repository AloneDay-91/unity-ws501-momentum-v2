using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; // Le joueur à suivre
    public Vector3 offset = new Vector3(0, 3, -10); // Position de la caméra par rapport au joueur

    // LateUpdate est appelé après tous les Update(),
    // c'est le meilleur endroit pour une caméra.
    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}