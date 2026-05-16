/// <summary>
/// Flag dev-only. Quand Active est true, la build WEB_BUILD démarre une partie solo
/// hors-ligne (aucune connexion Colyseus). Seul DevSoloLauncher (#if UNITY_EDITOR) le
/// passe à true ; dans une build livrée il reste false et tous les branchements
/// `if (DevSolo.Active)` sont du code mort inactif.
/// </summary>
public static class DevSolo
{
    public static bool Active = false;
}
