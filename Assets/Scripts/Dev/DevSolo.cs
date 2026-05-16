using UnityEngine;

/// <summary>
/// Flag dev-only. Quand Active est true, la build WEB_BUILD démarre une partie solo
/// hors-ligne (aucune connexion Colyseus). Seul DevSoloLauncher (#if UNITY_EDITOR) le
/// passe à true ; dans une build livrée il reste false et tous les branchements
/// `if (DevSolo.Active)` sont du code mort inactif.
/// </summary>
public static class DevSolo
{
    public static bool Active = false;

#if UNITY_EDITOR
    // Réinitialise le flag à chaque entrée en Play Mode, même quand le rechargement
    // de domaine est désactivé (SubsystemRegistration s'exécute dans tous les cas).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode() => Active = false;
#endif
}
