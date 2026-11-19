using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class KillFeedItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text killerText;
    public TMP_Text victimText;
    public Image weaponIcon;

    [Header("Settings")]
    public float displayTime = 4f;
    public float fadeOutTime = 1f;

    private CanvasGroup canvasGroup;

    public void Setup(string killerName, string victimName, Sprite icon)
    {
        if (killerText != null) killerText.text = killerName;
        if (victimText != null) victimText.text = victimName;
        
        if (weaponIcon != null)
        {
            if (icon != null)
            {
                weaponIcon.sprite = icon;
                weaponIcon.gameObject.SetActive(true);
            }
            else
            {
                weaponIcon.gameObject.SetActive(false);
            }
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(displayTime);

        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}