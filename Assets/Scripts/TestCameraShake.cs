using UnityEngine;

/// <summary>
/// Script de test pour vérifier que le camera shake fonctionne
/// Appuyez sur ESPACE pour déclencher un shake
/// </summary>
public class TestCameraShake : MonoBehaviour
{
    void Update()
    {
        // Test avec ESPACE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.ShakeAllStrong();
                Debug.Log("✅ SHAKE FORT DÉCLENCHÉ !");
            }
            else
            {
                Debug.LogError("❌ CameraShakeManager.Instance est NULL !");
            }
        }

        // Test léger avec T
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.ShakeAllLight();
                Debug.Log("✅ Shake léger déclenché");
            }
        }

        // Test moyen avec M
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (CameraShakeManager.Instance != null)
            {
                CameraShakeManager.Instance.ShakeAllMedium();
                Debug.Log("✅ Shake moyen déclenché");
            }
        }
    }
}
