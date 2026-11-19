using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ButtonTextColor : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI buttonText;

    public Color normalTextColor = Color.black;
    public Color hoverTextColor = Color.white;

    public Color normalBackgroundColor = Color.white;
    public Color hoverBackgroundColor = Color.black;

    private bool isSelected = false;
    private Image backgroundImage;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Reset trạng thái
        isSelected = false;
        buttonText.color = normalTextColor;
        if (backgroundImage != null)
            backgroundImage.color = normalBackgroundColor;

        // Chờ Unity chọn First Selected rồi ép màu
        StartCoroutine(ForceSelectedColorNextFrame());
    }

    private System.Collections.IEnumerator ForceSelectedColorNextFrame()
    {
        yield return null; // đợi EventSystem chọn first selected

        // Nếu nút này chính là nút đang được chọn
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            ApplyHoverStyle();
            isSelected = true;
        }
    }

    // Áp dụng style hover/select
    private void ApplyHoverStyle()
    {
        buttonText.color = hoverTextColor;

        if (backgroundImage != null)
            backgroundImage.color = hoverBackgroundColor;
    }

    // Áp dụng style normal
    private void ApplyNormalStyle()
    {
        buttonText.color = normalTextColor;

        if (backgroundImage != null)
            backgroundImage.color = normalBackgroundColor;
    }

    // Hover bằng chuột
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
        {
            ApplyHoverStyle();
        }
    }

    // Rời chuột
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            ApplyNormalStyle();
    }

    // Khi được chọn (bằng click hoặc phím)
    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyHoverStyle();
    }

    // Khi bị bỏ chọn
    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ApplyNormalStyle();
    }
}
