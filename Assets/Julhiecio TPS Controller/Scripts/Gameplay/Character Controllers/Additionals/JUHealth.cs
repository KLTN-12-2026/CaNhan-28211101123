using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPS.FX;
using JUTPSEditor.JUHeader;
using Photon.Pun;

namespace JUTPS
{
    [AddComponentMenu("JU TPS/Third Person System/Additionals/JU Health")]
    public class JUHealth : MonoBehaviourPun
    {
        [JUHeader("Settings")]
        public float Health = 200;
        public float MaxHealth = 200;

        [JUHeader("Armor Settings")]
        public float Armor = 0;
        public float MaxArmor = 100;
        [Range(0f, 1f)]
        public float ArmorAbsorption = 1f;

        [JUHeader("Effects")]
        public bool BloodScreenEffect = false;
        public GameObject BloodHitParticle;

        [JUHeader("On Death Event")]
        public UnityEvent OnDeath;

        [JUHeader("Stats")]
        public bool IsDead;

        [HideInInspector] public string lastKillerName = "";
        [HideInInspector] public string lastWeaponName = "";

        [JUHeader("Assist System")]
        public float assistDamageThreshold = 25f;
        public Dictionary<string, float> damageContributors = new Dictionary<string, float>();

        void Start()
        {
            LimitHealth();
            InvokeRepeating(nameof(CheckHealthState), 0, 0.5f);

            if (photonView != null && !photonView.IsMine)
            {
                var bloodScreen = GetComponentInChildren<BloodScreen>();
                if (bloodScreen != null) 
                {
                    bloodScreen.gameObject.SetActive(false);
                }
            }
        }

        private void LimitHealth()
        {
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            Armor = Mathf.Clamp(Armor, 0, MaxArmor);
        }

        public static void DoDamage(JUHealth health, float damage, Vector3 hitPosition = default(Vector3))
        {
            health.DoDamage(damage, hitPosition);
        }

        public void DoDamage(float damage, Vector3 hitPosition = default(Vector3))
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                photonView.RPC(nameof(RPC_DoDamageWithInfo), RpcTarget.All, damage, "Environment", "Environment", hitPosition);
            }
            else
            {
                RPC_DoDamageWithInfo(damage, "Environment", "Environment", hitPosition);
            }
        }

        public void DoDamage(float damage, string killerName, string weaponName, Vector3 hitPosition = default(Vector3))
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                photonView.RPC(nameof(RPC_DoDamageWithInfo), RpcTarget.All, damage, killerName, weaponName, hitPosition);
            }
            else
            {
                RPC_DoDamageWithInfo(damage, killerName, weaponName, hitPosition);
            }
        }

        [PunRPC]
        public void RPC_DoDamageWithInfo(float damage, string killerName, string weaponName, Vector3 hitPosition)
        {
            if (string.IsNullOrEmpty(killerName))
            {
                lastKillerName = "Environment";
            }
            else
            {
                lastKillerName = killerName;

                if (photonView != null && killerName != photonView.Owner.NickName && killerName != "Environment" && killerName != "Unknown")
                {
                    if (damageContributors.ContainsKey(killerName))
                    {
                        damageContributors[killerName] += damage;
                    }
                    else
                    {
                        damageContributors.Add(killerName, damage);
                    }
                }
            }

            lastWeaponName = string.IsNullOrEmpty(weaponName) ? "Environment" : weaponName;
            
            RPC_DoDamage(damage, hitPosition);
        }

        [PunRPC]
        public void RPC_DoDamage(float damage, Vector3 hitPosition)
        {
            if (Armor > 0)
            {
                float damageToArmor = damage * ArmorAbsorption;
                float damageToHealth = damage - damageToArmor;

                if (Armor >= damageToArmor)
                {
                    Armor -= damageToArmor;
                }
                else
                {
                    float remainingDamage = damageToArmor - Armor;
                    Armor = 0;
                    damageToHealth += remainingDamage; 
                }
                
                Health -= damageToHealth;
            }
            else
            {
                Health -= damage;
            }

            LimitHealth();
            CheckHealthState();

            if (hitPosition != Vector3.zero && BloodHitParticle != null)
            {
                GameObject fxParticle = Instantiate(BloodHitParticle, hitPosition, Quaternion.identity, this.transform);
                fxParticle.hideFlags = HideFlags.HideInHierarchy;
                Destroy(fxParticle, 3);
            }

            if (BloodScreenEffect)
            {
                if (photonView != null && photonView.IsMine)
                {
                    var bloodScreen = GetComponentInChildren<BloodScreen>(); 
                    if (bloodScreen != null) bloodScreen.PlayerHasHited();
                }
            }
        }

        public void CheckHealthState()
        {
            LimitHealth();

            if (Health <= 0 && IsDead == false)
            {
                Health = 0;
                IsDead = true;

                foreach (Damager dmg in GetComponentsInChildren<Damager>()) dmg.gameObject.SetActive(false);

                JUTPS.PhysicsScripts.AdvancedRagdollController ragdoll = GetComponent<JUTPS.PhysicsScripts.AdvancedRagdollController>();
                if (ragdoll != null) ragdoll.enabled = false; 

                JUFootPlacement footPlacer = GetComponent<JUFootPlacement>();
                if (footPlacer != null) footPlacer.enabled = false; 

                JUCharacterController controller = GetComponent<JUCharacterController>();
                if (controller != null) controller.enabled = false;

                if (photonView != null && !photonView.IsMine)
                {
                    PhotonTransformView ptv = GetComponent<PhotonTransformView>();
                    if (ptv != null) ptv.enabled = false;

                    PhotonRigidbodyView prv = GetComponent<PhotonRigidbodyView>();
                    if (prv != null) prv.enabled = false;
                }

                Rigidbody rootRb = GetComponent<Rigidbody>();
                if (rootRb != null)
                {
                    if (!rootRb.isKinematic)
                    {
                        rootRb.linearVelocity = Vector3.zero;
                        rootRb.angularVelocity = Vector3.zero;
                    }
                    rootRb.isKinematic = true;
                    rootRb.useGravity = false;
                }

                CapsuleCollider col = GetComponent<CapsuleCollider>();
                if (col != null) col.enabled = false;

                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.enabled = true; 
                    anim.applyRootMotion = false; 
                    
                    anim.SetLayerWeight(1, 0);
                    anim.SetLayerWeight(2, 0);
                    anim.SetLayerWeight(3, 0);
                    anim.SetLayerWeight(4, 0);
                    anim.SetLayerWeight(5, 0);
                    
                    if (controller != null && controller.AnimatorParameters != null && !string.IsNullOrEmpty(controller.AnimatorParameters.Dying))
                        anim.SetBool(controller.AnimatorParameters.Dying, true);
                    else
                        anim.SetBool("Die", true);
                }

                if (photonView == null || photonView.IsMine)
                {
                    if (controller != null)
                    {
                        controller.RagdollWhenDie = false;
                        controller.KillCharacter();
                    }

                    if (!string.IsNullOrEmpty(lastKillerName) && KillFeedManager.Instance != null && lastKillerName != "Environment")
                    {
                        string victimName = photonView != null ? photonView.Owner.NickName : "Unknown";
                        KillFeedManager.Instance.BroadcastKill(lastKillerName, victimName, lastWeaponName);
                    }

                    if (SpectatorManager.Instance != null) SpectatorManager.Instance.StartSpectating();

                    if (PhotonNetwork.IsConnected)
                    {
                        int currentDeaths = 0;
                        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Deaths"))
                        {
                            currentDeaths = (int)PhotonNetwork.LocalPlayer.CustomProperties["Deaths"];
                        }

                        ExitGames.Client.Photon.Hashtable deathHash = new ExitGames.Client.Photon.Hashtable();
                        deathHash.Add("IsDead", true);
                        deathHash.Add("Deaths", currentDeaths + 1);
                        PhotonNetwork.LocalPlayer.SetCustomProperties(deathHash);

                        if (!string.IsNullOrEmpty(lastKillerName) && lastKillerName != PhotonNetwork.LocalPlayer.NickName && lastKillerName != "Environment" && lastKillerName != "Unknown")
                        {
                            foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
                            {
                                if (p.NickName == lastKillerName)
                                {
                                    int currentKills = 0;
                                    if (p.CustomProperties.ContainsKey("Kills"))
                                        currentKills = (int)p.CustomProperties["Kills"];

                                    ExitGames.Client.Photon.Hashtable killHash = new ExitGames.Client.Photon.Hashtable();
                                    killHash.Add("Kills", currentKills + 1);
                                    p.SetCustomProperties(killHash);
                                    break;
                                }
                            }
                        }

                        foreach (var contributor in damageContributors)
                        {
                            if (contributor.Key != lastKillerName && contributor.Value >= assistDamageThreshold)
                            {
                                foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
                                {
                                    if (p.NickName == contributor.Key)
                                    {
                                        int currentAssists = 0;
                                        if (p.CustomProperties.ContainsKey("Assists"))
                                            currentAssists = (int)p.CustomProperties["Assists"];

                                        ExitGames.Client.Photon.Hashtable assistHash = new ExitGames.Client.Photon.Hashtable();
                                        assistHash.Add("Assists", currentAssists + 1);
                                        p.SetCustomProperties(assistHash);
                                        break;
                                    }
                                }
                            }
                        }

                        damageContributors.Clear();
                    }
                }

                try { OnDeath?.Invoke(); } catch { }
            }

            if (Health > 0) IsDead = false;
        }

        public void UpdateArmorThroughNetwork(float newArmorValue)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                photonView.RPC(nameof(RPC_SetArmor), RpcTarget.All, newArmorValue);
            }
            else
            {
                RPC_SetArmor(newArmorValue);
            }
        }

        [PunRPC]
        private void RPC_SetArmor(float newArmorValue)
        {
            Armor = newArmorValue;
            LimitHealth();
        }

        [PunRPC]
        public void RPC_TakeDamage(float damage)
        {
            RPC_DoDamageWithInfo(damage, "Environment", "Environment", Vector3.zero);
        }
    }
}