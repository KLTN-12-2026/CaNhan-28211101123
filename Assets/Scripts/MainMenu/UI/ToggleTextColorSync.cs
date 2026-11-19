using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class ToggleTextColorSync : MonoBehaviour
{
    public TextMeshProUGUI labelText;

    public Color selectedColor = Color.black;
    public Color unselectedColor = Color.white;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void Start()
    {
        Refresh();
    }

    void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    void OnToggleValueChanged(bool isOn)
    {
        Refresh();
    }

    void Refresh()
    {
        if (labelText == null || toggle == null)
            return;

        labelText.color = toggle.isOn ? selectedColor : unselectedColor;
    }
}
