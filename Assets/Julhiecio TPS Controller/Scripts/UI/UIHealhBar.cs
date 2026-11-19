using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS;

namespace JUTPS.UI
{
    [AddComponentMenu("JU TPS/UI/UI Health Bar")]
    public class UIHealhBar : MonoBehaviour
    {
        [Header("UI Bar Settings")]
        [SerializeField] private JUHealth HealthComponent;
        [SerializeField] private bool IsPlayerHealthBar = true;
        [SerializeField] private bool IsArmorBar = false; 

        [SerializeField] private Image BarImage; 
        [SerializeField] private float Speed = 6;
        [SerializeField] private Text PointsText;

        [Header("Color Settings")]
        [SerializeField] private Color EmptyColor = Color.red;
        [SerializeField] private Color FullColor = Color.green;
        [SerializeField] private Color HealingColor = Color.cyan;
        [SerializeField] private Color LossColor = Color.yellow;
        [SerializeField] private bool ChangeTextColorToo = true;

        private float oldFillAmount;
        
        void Start()
        {
            if (BarImage != null) oldFillAmount = BarImage.fillAmount;
        }

        void Update()
        {
            if (IsPlayerHealthBar && (HealthComponent == null || (HealthComponent.photonView != null && !HealthComponent.photonView.IsMine)))
            {
                JUHealth[] allHealths = FindObjectsOfType<JUHealth>();
                foreach (JUHealth h in allHealths)
                {
                    if (h.photonView != null && h.photonView.IsMine && !h.IsDead)
                    {
                        HealthComponent = h;
                        break;
                    }
                }
            }

            if (HealthComponent == null || BarImage == null) return;

            float currentValue = IsArmorBar ? HealthComponent.Armor : HealthComponent.Health;
            float maxValue = IsArmorBar ? HealthComponent.MaxArmor : HealthComponent.MaxHealth;
            float normalizedValue = maxValue > 0 ? currentValue / maxValue : 0;
            
            BarImage.fillAmount = Mathf.MoveTowards(BarImage.fillAmount, normalizedValue, Speed * Time.deltaTime);
            BarImage.color = Color.Lerp(EmptyColor, FullColor, BarImage.fillAmount);

            if (PointsText != null)
            {
                PointsText.text = Mathf.RoundToInt(currentValue).ToString("0") + "/" + Mathf.RoundToInt(maxValue).ToString("000");
                if (ChangeTextColorToo) PointsText.color = Color.Lerp(BarImage.color, Color.white, 0.6f);
            }
            
            if (oldFillAmount != BarImage.fillAmount)
            {
                if (oldFillAmount < BarImage.fillAmount)
                {
                    BarImage.color = HealingColor;
                }
               
                if (oldFillAmount > BarImage.fillAmount)
                {
                    BarImage.color = LossColor;
                }

                oldFillAmount = BarImage.fillAmount;
            }
        }
    }
}