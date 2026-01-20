using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI text;
    public Color selectedColor = Color.black;
    public Color normalColor = Color.white;

    public void OnSelect(BaseEventData eventData)
    {
        text.color = selectedColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        text.color = normalColor;
    }
}
