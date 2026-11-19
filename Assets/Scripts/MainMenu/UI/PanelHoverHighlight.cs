using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Cinemachine;

public class PanelHoverHighlight : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("UI")]
    public Image panelImage;
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.9f, 0.6f);
    public Color selectedColor = new Color(1f, 0.8f, 0.3f);

    [Header("Cinemachine")]
    public CinemachineCamera cam;
    public int activePriority = 20;
    public int inactivePriority = 10;

    private bool isSelected = false;

    private void Reset()
    {
        panelImage = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected) return;
        panelImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;
        panelImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isSelected) return;
        Select();
    }

    private void Select()
    {
        isSelected = true;

        // Đổi màu sang trạng thái đã chọn
        panelImage.color = selectedColor;

        // Tắt hover vĩnh viễn
        panelImage.raycastTarget = false;

        // Kích hoạt camera này
        if (cam != null)
        {
            cam.Priority = activePriority;
        }
    }

    // Gọi từ ngoài nếu bạn muốn reset lại
    public void Deselect()
    {
        isSelected = false;
        panelImage.color = normalColor;
        panelImage.raycastTarget = true;

        if (cam != null)
        {
            cam.Priority = inactivePriority;
        }
    }
}
