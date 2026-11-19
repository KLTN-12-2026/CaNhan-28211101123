using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

namespace JUTPS.WeaponSystem
{
    public class JUScopeSystemUI : MonoBehaviour
    {
        public Image ScopeImage;
        public JUCharacterController PlayerCharacter;
        public bool IsShowingScope { get; private set; }

        private void Update()
        {
            if (PlayerCharacter == null ||
                PlayerCharacter.GetComponent<PhotonView>() == null ||
                !PlayerCharacter.GetComponent<PhotonView>().IsMine)
            {
                PlayerCharacter = null;

                foreach (var controller in FindObjectsByType<JUCharacterController>(FindObjectsSortMode.None))
                {
                    PhotonView pv = controller.GetComponent<PhotonView>();
                    if (pv != null && pv.IsMine)
                    {
                        PlayerCharacter = controller;
                        break;
                    }
                }
            }

            IsShowingScope = false;

            if (PlayerCharacter != null && PlayerCharacter.HoldableItemInUseRightHand != null)
            {
                if (PlayerCharacter.HoldableItemInUseRightHand is Weapon weapon)
                {
                    if (PlayerCharacter.IsAiming && weapon.AimMode == Weapon.WeaponAimMode.Scope)
                    {
                        IsShowingScope = true;
                        if (weapon.ScopeTexture)
                            ScopeImage.sprite = weapon.ScopeTexture;
                    }
                }
            }

            if (ScopeImage.gameObject.activeSelf != IsShowingScope)
            {
                ScopeImage.gameObject.SetActive(IsShowingScope);
            }
        }
    }
}