using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionSelector : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;
    public Button leftButton;
    public Button rightButton;

    [Header("Values")]
    public List<string> values = new List<string>();
    public int currentIndex = 0;

    [Header("Config")]
    public bool loop = true;

    void Start()
    {
        leftButton.onClick.AddListener(Previous);
        rightButton.onClick.AddListener(Next);

        RefreshUI();
        ApplySetting();
    }

    public void Next()
    {
        if (values.Count == 0) return;

        currentIndex++;

        if (currentIndex >= values.Count)
            currentIndex = loop ? 0 : values.Count - 1;

        RefreshUI();
        ApplySetting();
    }

    public void Previous()
    {
        if (values.Count == 0) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = loop ? values.Count - 1 : 0;

        RefreshUI();
        ApplySetting();
    }

    void RefreshUI()
    {
        if (valueText != null && values.Count > 0)
            valueText.text = values[currentIndex];
    }

    // Override hoặc sửa hàm này cho từng loại Option
    void ApplySetting()
    {
        Debug.Log($"{labelText.text} = {values[currentIndex]}");
    }
}
