/*
 Gère le "bouton blanc" et le minuteur d'inactivité.
*/

using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [SerializeField] TMP_Text quitText;
    const float AfkTime = 60f;
    float afkTimer = 0f;
    const float HeldQuitTime = 1.5f;
    float heldQuitTimer = 0f;
    const string MenuMessage = "Retour au menu";

    [DllImport("__Internal")]
    public static extern void BackToMenu();

    void Update()
    {
#if WEB_BUILD
        // Pas de borne d'arcade en WebGL : ni minuteur d'inactivité ni retour menu forcé.
        if (quitText != null && quitText.gameObject.activeSelf) quitText.gameObject.SetActive(false);
        return;
#else
        if (Input.GetButton("Coin"))
        {
            heldQuitTimer += Time.unscaledDeltaTime;
            if (heldQuitTimer > 0.1f && (int)(heldQuitTimer * 5) != (int)((heldQuitTimer - Time.unscaledDeltaTime) * 5))
            {
                Debug.Log($"MenuManager: Bouton Coin maintenu... {heldQuitTimer:F1}/{HeldQuitTime:F1}");
            }
        }
        else
        {
            if (heldQuitTimer > 0) Debug.Log("MenuManager: Bouton Coin relâché");
            heldQuitTimer = 0f;
        }

        if (heldQuitTimer >= HeldQuitTime || afkTimer >= AfkTime) {
            Debug.Log($"MenuManager: Action Quitter déclenchée! Held: {heldQuitTimer >= HeldQuitTime}, AFK: {afkTimer >= AfkTime}");
            BackToMenu();
        }

        if (Mathf.Abs(Input.GetAxisRaw("P1_Horizontal")) > 0.5f || Mathf.Abs(Input.GetAxisRaw("P1_Vertical")) > 0.5f || Input.GetButton("P1_Start") || Input.GetButton("P1_B1") || Input.GetButton("P1_B2") || Input.GetButton("P1_B3") || Input.GetButton("P1_B4") || Input.GetButton("P1_B5") || Input.GetButton("P1_B6") ||
            Mathf.Abs(Input.GetAxisRaw("P2_Horizontal")) > 0.5f || Mathf.Abs(Input.GetAxisRaw("P2_Vertical")) > 0.5f || Input.GetButton("P2_Start") || Input.GetButton("P2_B1") || Input.GetButton("P2_B2") || Input.GetButton("P2_B3") || Input.GetButton("P2_B4") || Input.GetButton("P2_B5") || Input.GetButton("P2_B6"))
            afkTimer = 0f;
        else
            afkTimer += Time.unscaledDeltaTime;

        if (heldQuitTimer != 0 || afkTimer - AfkTime + 6f > 0f) {
            quitText.gameObject.SetActive(true);
            quitText.text = MenuMessage + new string('.', (int)Mathf.Min(Mathf.Max(heldQuitTimer * 3f, afkTimer - AfkTime + 10f * 0.4f), 3));
        } else quitText.gameObject.SetActive(false);
#endif
    }

    public void OnApplicationQuit()
    {
#if !WEB_BUILD
        BackToMenu();
#endif
    }
}
