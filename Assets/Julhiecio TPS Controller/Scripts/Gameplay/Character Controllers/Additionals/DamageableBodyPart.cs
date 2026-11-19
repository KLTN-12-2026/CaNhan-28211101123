using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.ArmorSystem
{
    [AddComponentMenu("JU TPS/Armor System/Damageable Body Part")]
    public class DamageableBodyPart : MonoBehaviour
    {
        public JUHealth Health;
        public float DamageMultiplier = 1;
        [HideInInspector] public Armor ArmorProtecting;
        private void Start()
        {
            if (Health == null)
            {
                Health = GetComponentInParent<JUHealth>();
            }
        }
        public float DoDamage(float damage, string killerName, string weaponName, Vector3 hitPoint)
        {
            if (Health == null)
                return 0;

            float finalDamage = DamageMultiplier * damage;

            Health.DoDamage(finalDamage, killerName, weaponName, hitPoint);

            if (ArmorProtecting != null)
                ArmorProtecting.DoDamageOnArmor(damage);

            return finalDamage;
        }
    }
}