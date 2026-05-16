/// <summary>
/// Signale qu'on revient à la scène menu pour un « Rejouer » : MenuPageManager doit
/// alors afficher directement la LobbyPage en mode attente au lieu de la page d'accueil.
/// Faux par défaut ; levé par GameOverUI, consommé par MenuPageManager.
/// </summary>
public static class RematchState
{
    public static bool ReturningForRematch = false;
}
