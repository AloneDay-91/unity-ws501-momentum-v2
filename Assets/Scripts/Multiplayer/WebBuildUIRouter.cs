#if WEB_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// Re-routes the split-screen scene UI into a single-player fullscreen layout:
// - PlayerScoreUI for the local player → top-left of the root canvas (top-right for the opponent)
// - LuminescenceBar (any variant) for the local player → bottom-left of the root canvas
// - Opponent's lumi bars are disabled
// - Opponent's top-level UI panel is hidden
// - Local player's top-level UI panel is stretched fullscreen (so timer/rank/distance scale up)
// - "LUMIERE" static labels (split-screen relic) are hidden
// - Per-player elimination overlays are disabled (game over uses gameOverPanel instead)
public class WebBuildUIRouter : MonoBehaviour
{
    [Header("Margins (pixels from screen edges)")]
    public Vector2 scoreTopMargin = new Vector2(40, 40);
    public Vector2 lumiBottomMargin = new Vector2(40, 40);
    public Vector2 progressionBottomMargin = new Vector2(0, 80);
    public Vector2 timerTopMargin = new Vector2(0, 20);
    // Score panel spans top-right x:[-320,-40] y:[-130,-40]. Rank+distance sit BELOW it
    // (same right edge as score, stacked vertically) so they don't get hidden behind it.
    public Vector2 rankTopRightMargin = new Vector2(40, 150);
    public Vector2 distanceTopRightMargin = new Vector2(40, 210);

    [Header("Sizes")]
    public Vector2 scoreSize = new Vector2(280f, 90f);
    public Vector2 lumiSize = new Vector2(320f, 36f);
    public Vector2 timerSize = new Vector2(220f, 60f);
    public Vector2 rankSize = new Vector2(180f, 50f);
    public Vector2 distanceSize = new Vector2(230f, 50f);

    [Header("Debug")]
    public bool showDebug = true;

    private bool _applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.name != "main") return;
            var go = new GameObject("WebBuildUIRouter (auto)");
            go.AddComponent<WebBuildUIRouter>();
        };
    }

    void Start() { StartCoroutine(WaitAndApply()); }

    private IEnumerator WaitAndApply()
    {
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            if (GameSessionManager.Instance != null && GameSessionManager.Instance.LocalPlayerNumber > 0)
            {
                Apply(GameSessionManager.Instance.LocalPlayerNumber);
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.LogWarning("[WebBuildUIRouter] Timed out waiting for LocalPlayerNumber — UI not adjusted");
    }

    private void Apply(int localPlayerNumber)
    {
        if (_applied) return;
        _applied = true;
        int other = localPlayerNumber == 1 ? 2 : 1;

        // --- PHASE 1: identify top-level panels BEFORE moving any UI ---
        // (If we reparent PlayerScoreUI first, FindCanvasDirectChild would return
        //  PlayerScoreUI itself as the panel and we'd stretch it to fullscreen,
        //  which is what caused the player text to appear centered.)
        var localUiTransforms = CollectPlayerUiTransforms(localPlayerNumber);
        var otherUiTransforms = CollectPlayerUiTransforms(other);

        var localPanels = new HashSet<Transform>();
        var otherPanels = new HashSet<Transform>();
        foreach (var t in localUiTransforms)
        {
            var p = FindCanvasDirectChild(t);
            if (p != null) localPanels.Add(p);
        }
        foreach (var t in otherUiTransforms)
        {
            var p = FindCanvasDirectChild(t);
            if (p != null && !localPanels.Contains(p)) otherPanels.Add(p);
        }

        // --- PHASE 2: score panel → top-left (local) / top-right (opponent) ---
        foreach (var s in FindObjectsOfType<PlayerScoreUI>(includeInactive: true))
        {
            var rt = ReparentToRootCanvas(s.GetComponent<RectTransform>());
            if (rt == null) continue;
            rt.sizeDelta = scoreSize;
            if (s.playerID == localPlayerNumber) AnchorTopLeft(rt, scoreTopMargin);
            else AnchorTopRight(rt, scoreTopMargin);
        }

        // --- PHASE 3: lumi bar → bottom-left (local), hide opponent's ---
        foreach (var l in FindObjectsOfType<LuminescenceBarUI>(includeInactive: true))
        {
            if (l.playerIDToTrack == localPlayerNumber)
            {
                var rt = ReparentToRootCanvas(l.GetComponent<RectTransform>());
                if (rt == null) continue;
                rt.sizeDelta = lumiSize;
                AnchorBottomLeft(rt, lumiBottomMargin);
            }
            else if (l.playerIDToTrack == other)
            {
                l.gameObject.SetActive(false);
            }
        }
        foreach (var l in FindObjectsOfType<SegmentedLuminescenceBar>(includeInactive: true))
        {
            if (l.playerIDToTrack == localPlayerNumber)
            {
                var rt = ReparentToRootCanvas(l.GetComponent<RectTransform>());
                if (rt == null) continue;
                rt.sizeDelta = lumiSize;
                AnchorBottomLeft(rt, lumiBottomMargin);
            }
            else if (l.playerIDToTrack == other)
            {
                l.gameObject.SetActive(false);
            }
        }

        // --- PHASE 4: hide opponent's top-level panels ---
        foreach (var p in otherPanels)
        {
            p.gameObject.SetActive(false);
        }

        // --- PHASE 5: stretch local panel(s) so remaining inner UI (timer/rank/distance) fills the screen ---
        // PlayerScoreUI / Lumi are already off this hierarchy (reparented to root canvas),
        // so they are NOT affected by this stretch.
        foreach (var p in localPanels)
        {
            if (p != null && p.gameObject.activeInHierarchy) StretchFullscreen(p);
        }

        // --- PHASE 5.5: timer / rank / distance ---
        // In the scene the P1 versions are anchored top-center / top-right of Panel_P1,
        // which still reads correctly once we stretch Panel_P1 to fullscreen. The P2 versions
        // however are anchored at (0.5, 0.5) / (1, 0.5) for the split-screen right viewport,
        // so stretching Panel_P2 leaves them sitting near the centre/right-middle of the screen
        // instead of along the top. Reparent the LOCAL player's elements to the root canvas
        // with explicit top anchors, and hide the opponent's.
        RouteSuffixedElement<TMP_Text>("Player" + localPlayerNumber + "_TimerText",
            "Player" + other + "_TimerText", AnchorTopCenter, timerTopMargin, timerSize);
        RouteSuffixedElement<TMP_Text>("RankText_P" + localPlayerNumber,
            "RankText_P" + other, AnchorTopRightFromOffset, rankTopRightMargin, rankSize);
        RouteSuffixedElement<TMP_Text>("DistanceText_P" + localPlayerNumber,
            "DistanceText_P" + other, AnchorTopRightFromOffset, distanceTopRightMargin, distanceSize);

        // --- PHASE 6: hide "LUMIERE" static labels (split-screen relic) ---
        foreach (var t in FindObjectsOfType<TMP_Text>(includeInactive: true))
        {
            if (t == null) continue;
            string text = t.text?.Trim().ToUpperInvariant() ?? "";
            if (text == "LUMIERE" || text == "LUMIÈRE") t.gameObject.SetActive(false);
        }
        foreach (var t in FindObjectsOfType<UnityEngine.UI.Text>(includeInactive: true))
        {
            if (t == null) continue;
            string text = t.text?.Trim().ToUpperInvariant() ?? "";
            if (text == "LUMIERE" || text == "LUMIÈRE") t.gameObject.SetActive(false);
        }

        // --- PHASE 7: progression bar bottom-center band ---
        foreach (var p in FindObjectsOfType<ProgressionUI>(includeInactive: true))
        {
            var rt = ReparentToRootCanvas(p.GetComponent<RectTransform>());
            if (rt == null) continue;
            StretchBottomBand(rt, progressionBottomMargin.y, 90f);
        }

        // --- PHASE 8: per-player ELIMINATED overlays handled by gameOverPanel ---
        var gm = GameManager.Instance;
        if (gm != null)
        {
            if (gm.player1EliminatedOverlay != null) gm.player1EliminatedOverlay.SetActive(false);
            if (gm.player2EliminatedOverlay != null) gm.player2EliminatedOverlay.SetActive(false);
        }

        if (showDebug)
        {
            Debug.Log($"[WebBuildUIRouter] Applied for local P{localPlayerNumber}: disabled {otherPanels.Count} opponent panel(s), stretched {localPanels.Count} local panel(s)");
        }
    }

    private static List<Transform> CollectPlayerUiTransforms(int playerID)
    {
        var list = new List<Transform>();
        foreach (var s in FindObjectsOfType<PlayerScoreUI>(true)) if (s.playerID == playerID) list.Add(s.transform);
        foreach (var l in FindObjectsOfType<LuminescenceBarUI>(true)) if (l.playerIDToTrack == playerID) list.Add(l.transform);
        foreach (var l in FindObjectsOfType<SegmentedLuminescenceBar>(true)) if (l.playerIDToTrack == playerID) list.Add(l.transform);
        return list;
    }

    // Walk up from `leaf` to the GameObject that is a direct child of its Canvas.
    private static Transform FindCanvasDirectChild(Transform leaf)
    {
        if (leaf == null) return null;
        var canvas = leaf.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        Transform t = leaf;
        while (t != null && t.parent != null && t.parent != canvas.transform)
        {
            t = t.parent;
        }
        return (t != null && t != canvas.transform) ? t : null;
    }

    private static RectTransform ReparentToRootCanvas(RectTransform rt)
    {
        if (rt == null) return null;
        var canvas = rt.GetComponentInParent<Canvas>();
        var root = canvas != null ? canvas.rootCanvas : null;
        if (root != null && rt.parent != root.transform)
        {
            rt.SetParent(root.transform, worldPositionStays: false);
        }
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static void StretchFullscreen(Transform t)
    {
        var rt = t as RectTransform;
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static void StretchBottomBand(RectTransform rt, float bottomMargin, float height)
    {
        rt.anchorMin = new Vector2(0.1f, 0f);
        rt.anchorMax = new Vector2(0.9f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(0f, bottomMargin);
        rt.offsetMax = new Vector2(0f, bottomMargin + height);
    }

    private static void AnchorTopLeft(RectTransform rt, Vector2 margin)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(margin.x, -margin.y);
    }

    private static void AnchorTopRight(RectTransform rt, Vector2 margin)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-margin.x, -margin.y);
    }

    private static void AnchorBottomLeft(RectTransform rt, Vector2 margin)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(margin.x, margin.y);
    }

    private static void AnchorTopCenter(RectTransform rt, Vector2 margin)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(margin.x, -margin.y);
    }

    // margin.x = distance from right edge to the element's right side, margin.y = distance from top
    private static void AnchorTopRightFromOffset(RectTransform rt, Vector2 margin)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-margin.x, -margin.y);
    }

    // Reparent the local element (matched by name) to the root canvas with explicit anchors,
    // and disable the opponent's mirror element so it doesn't leak into the fullscreen layout.
    private void RouteSuffixedElement<T>(string localName, string opponentName,
        System.Action<RectTransform, Vector2> anchor, Vector2 margin, Vector2 size)
        where T : Component
    {
        foreach (var c in FindObjectsOfType<T>(includeInactive: true))
        {
            if (c == null) continue;
            if (c.gameObject.name == localName)
            {
                var rt = ReparentToRootCanvas(c.GetComponent<RectTransform>());
                if (rt == null) continue;
                rt.sizeDelta = size;
                anchor(rt, margin);
                c.gameObject.SetActive(true);
            }
            else if (c.gameObject.name == opponentName)
            {
                c.gameObject.SetActive(false);
            }
        }
    }
}
#endif
