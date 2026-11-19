using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Blood Screen")]
    [RequireComponent(typeof(Image))]
    public class BloodScreen : MonoBehaviour
    {
        JUTPS.CharacterBrain.JUCharacterBrain pl;
        Image img;
        float healthvalue;
        Color currentColor;

        void Start()
        {
            img = GetComponent<Image>();
            pl = GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>();
        }

        void Update()
        {
            if (pl == null) return;

            if (pl.CharacterHealth != null)
            {
                float healthPercent = pl.CharacterHealth.Health / pl.CharacterHealth.MaxHealth;
                healthvalue = Mathf.Lerp(healthvalue, healthPercent, 15 * Time.deltaTime);
                Color targetColor = new Color(1, 0, 0, 0.5f);
                currentColor = Color.Lerp(targetColor, Color.clear, healthvalue);
                img.color = Color.Lerp(img.color, currentColor, 5 * Time.deltaTime);
            }
        }

        public void PlayerHasHited()
        {
            if (img != null) img.color = Color.white;
        }
    }
}