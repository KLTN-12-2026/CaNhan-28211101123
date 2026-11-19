using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AvatarOpenSelectorButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public Image panelImage;
    public AvatarSelectorUI selectorUI;

    public Animator animator;

    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.9f, 0.6f);

    private void Reset()
    {
        panelImage = GetComponent<Image>();
    }

    private void Start()
    {
        panelImage.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        panelImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        panelImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        selectorUI.OpenPanel();
        animator.SetBool("isOpen",true);
    }
}
