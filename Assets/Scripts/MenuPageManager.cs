using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gère la navigation entre les différentes pages du menu
/// </summary>
public class MenuPageManager : MonoBehaviour
{
    public static MenuPageManager Instance { get; private set; }

    [Header("Pages")]
    [Tooltip("Page d'accueil (avec le bouton Play)")]
    public GameObject homePage;

    [Tooltip("Page des QR codes")]
    public GameObject qrCodePage;

    [Tooltip("Page du lobby (attente des joueurs)")]
    public GameObject lobbyPage;

    [Tooltip("Page Comment Jouer (Tutoriel)")]
    public GameObject howToPlayPage;

    [Header("Settings")]
    [Tooltip("Page à afficher au démarrage")]
    public GameObject startingPage;

    [Header("Animation")]
    [Tooltip("Durée de la transition entre les pages")]
    public float transitionDuration = 0.3f;

    [Header("Debug")]
    public bool showDebug = true;

    private GameObject currentPage;
    private Dictionary<string, GameObject> pages = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Enregistre toutes les pages
        if (homePage != null) pages["home"] = homePage;
        if (qrCodePage != null) pages["qrcode"] = qrCodePage;
        if (lobbyPage != null) pages["lobby"] = lobbyPage;
        if (howToPlayPage != null) pages["howtoplay"] = howToPlayPage;

        // Cache toutes les pages
        foreach (var page in pages.Values)
        {
            if (page != null)
            {
                page.SetActive(false);
            }
        }

        // Affiche la page de départ (en WEB_BUILD comme en arcade : home page).
        // En WEB_BUILD, le bouton Play / ShowQRCodePage sera redirigé vers la LobbyPage
        // pour skipper la génération de QR code (inutile : la session vient déjà de l'URL).
        if (startingPage != null)
        {
            ShowPage(startingPage);
        }
        else if (homePage != null)
        {
            ShowPage(homePage);
        }
    }

    /// <summary>
    /// Affiche une page par son GameObject
    /// </summary>
    public void ShowPage(GameObject page)
    {
        if (page == null)
        {
            Debug.LogError("MenuPageManager: Page null!");
            return;
        }

        // Cache la page actuelle
        if (currentPage != null && currentPage != page)
        {
            currentPage.SetActive(false);
        }

        // Affiche la nouvelle page
        page.SetActive(true);
        currentPage = page;

        if (showDebug)
        {
            Debug.Log($"MenuPageManager: Affichage de la page {page.name}");
        }
    }

    /// <summary>
    /// Affiche une page par son nom
    /// </summary>
    public void ShowPageByName(string pageName)
    {
        if (pages.ContainsKey(pageName))
        {
            ShowPage(pages[pageName]);
        }
        else
        {
            Debug.LogError($"MenuPageManager: Page '{pageName}' introuvable!");
        }
    }

    /// <summary>
    /// Affiche la page d'accueil
    /// </summary>
    public void ShowHomePage()
    {
        ShowPage(homePage);
    }

    /// <summary>
    /// Affiche la page des QR codes (en WEB_BUILD on skip cette étape arcade et on
    /// va directement à la LobbyPage — la session vient déjà de l'URL, pas besoin de QR).
    /// </summary>
    public void ShowQRCodePage()
    {
#if WEB_BUILD
        if (WebBootstrap.IsReady && lobbyPage != null)
        {
            if (showDebug) Debug.Log("[MenuPageManager] WEB_BUILD: ShowQRCodePage → redirect to LobbyPage");
            ShowPage(lobbyPage);
            return;
        }
#endif
        ShowPage(qrCodePage);
    }

    /// <summary>
    /// Affiche la page du lobby
    /// </summary>
    public void ShowLobbyPage()
    {
        ShowPage(lobbyPage);
    }

    /// <summary>
    /// Affiche la page Comment Jouer
    /// </summary>
    public void ShowHowToPlayPage()
    {
        ShowPage(howToPlayPage);
    }

    /// <summary>
    /// Retourne à la page précédente
    /// </summary>
    public void GoBack()
    {
        // Par défaut, retourne à l'accueil
        ShowHomePage();
    }
}