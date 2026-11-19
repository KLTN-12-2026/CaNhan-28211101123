using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.1f;
    public float speed = 10f;

    private Vector3 normalScale;
    private Vector3 targetScale;

    void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }
}
