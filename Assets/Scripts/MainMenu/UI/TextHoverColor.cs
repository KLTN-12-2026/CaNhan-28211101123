using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TextHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public GameObject arrowObject;        // GameObject chứa dấu >
    public TextMeshProUGUI labelText;      // Text chính

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    void Start()
    {
        if (arrowObject != null)
            arrowObject.SetActive(false);

        if (labelText != null)
            labelText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (arrowObject != null)
            arrowObject.SetActive(true);

        if (labelText != null)
            labelText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowObject != null)
            arrowObject.SetActive(false);

        if (labelText != null)
            labelText.color = normalColor;
    }
}
