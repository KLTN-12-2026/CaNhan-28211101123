using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

namespace JUTPS.ActionScripts
{
    [AddComponentMenu("JU TPS/Third Person System/Actions/Fall Damage")]
    public class FallDamage : JUTPSActions.JUTPSAction
    {
        [Header("Fall Damage")]
        [Range(0, 10)]
        public float Damage = 10;
        public float HeightToGetDamage = 4;
        [Header("Landing Roll")]
        public bool RollWhenLand;
        public float HeightToMakeCharacterRoll = 2;
        public bool CameraShake = true;
        [Range(0, 5)]
        public float CameraShakeIntensity = 3;

        [Header("Events")]
        public UnityEvent OnLanding;
        public UnityEvent OnFalling;
        private float FallDamageIntensity;

        private PhotonView pv;

        public override void Awake()
        {
            base.Awake();
            pv = GetComponent<PhotonView>();
        }

        void Update()
        {
            if (pv != null && !pv.IsMine) return; 

            if (TPSCharacter.IsGrounded == false && TPSCharacter.CanJump == false)
            {
                Falling();

                float LocalUpSpeed = transform.InverseTransformDirection(rb.linearVelocity).y;

                if (LocalUpSpeed > 0)
                {
                    FallDamageIntensity = 0;
                }
                else
                {
                    FallDamageIntensity = -LocalUpSpeed / 5;
                }

                anim.SetFloat(TPSCharacter.AnimatorParameters.LandingIntensity, FallDamageIntensity);
            }
            else
            {
                if (FallDamageIntensity > 0)
                {
                    if (FX.Shaker.GetCurrentCameraInstance() != null)
                    {
                        FX.Shaker.GetCurrentCameraInstance().Shake(FallDamageIntensity + 3, 0.2f, 30, 3, 6, CameraShakeIntensity * FallDamageIntensity / 30);
                    }

                    if (FallDamageIntensity > HeightToMakeCharacterRoll && RollWhenLand)
                    {
                        TPSCharacter._Roll();
                    }
                    
                    if (FallDamageIntensity > HeightToGetDamage)
                    {
                        float finalDamage = FallDamageIntensity * Damage;
                        
                        if (pv != null)
                        {
                            pv.RPC("TakeFallDamageRPC", RpcTarget.All, finalDamage);
                        }
                        else
                        {
                            TPSCharacter.TakeDamage(finalDamage);
                        }
                    }
                    FallDamageIntensity = 0;
                    Landed();
                }
            }

            if (TPSCharacter.IsDriving || TPSCharacter.IsRagdolled)
            {
                FallDamageIntensity = 0;
            }
        }
        
        protected virtual void Landed()
        {
            OnLanding.Invoke();
        }
        protected virtual void Falling()
        {
            OnFalling.Invoke();
        }

        [PunRPC]
        public void TakeFallDamageRPC(float damageValue)
        {
            if (TPSCharacter != null)
            {
                TPSCharacter.TakeDamage(damageValue);
            }
        }
    }
}