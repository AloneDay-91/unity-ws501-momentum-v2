#if WEB_BUILD
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Fullscreen black overlay shown to the first-loaded client while it waits for
// the opponent's scene to come up. Built at runtime so we don't depend on any
// arcade-era scene asset that might not be present.
public class WebWaitOverlay : MonoBehaviour
{
    private GameObject _root;

    public static WebWaitOverlay CreateAndShow()
    {
        var go = new GameObject("WebWaitOverlay");
        var inst = go.AddComponent<WebWaitOverlay>();
        inst.Build();
        return inst;
    }

    private void Build()
    {
        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = Color.black;
        var bgRt = bg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(canvasGo.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "EN ATTENTE DE L'AUTRE JOUEUR...";
        label.fontSize = 56f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        var labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0.1f, 0.4f);
        labelRt.anchorMax = new Vector2(0.9f, 0.6f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        _root = canvasGo;
    }

    public void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }

    public void Destroy()
    {
        if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
    }
}
#endif
