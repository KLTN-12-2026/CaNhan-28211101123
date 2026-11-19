using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;
using Photon.Pun;

namespace JUTPS.InventorySystem.UI
{
    public class UIItemInformation : MonoBehaviour
    {
        private JUHoldableItem CurrentItem;
        private JUCharacterController Player;

        [Header("Essentials")]
        public Sprite EmptySprite;
        public Image Icon;
        public Text ItemName;
        public GameObject BulletLabel;
        public Text BulletQuantity;

        void Start()
        {
        }

        public void ResetPlayerReference()
        {
            Player = null;
        }

        void Update()
        {
            if (Player == null)
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                foreach (GameObject p in players)
                {
                    PhotonView pv = p.GetComponent<PhotonView>();
                    if (pv != null && pv.IsMine)
                    {
                        Player = p.GetComponent<JUCharacterController>();
                        break;
                    }
                }
                
                if (Player == null) return;
            }

            if (Player.Inventory == null) return;

            CurrentItem = Player.HoldableItemInUseRightHand;

            if (CurrentItem == null)
            {
                if (Icon != null) Icon.sprite = EmptySprite;
                if (BulletLabel != null) BulletLabel.SetActive(false);
                if (ItemName != null) ItemName.text = "Hand";
            }
            else
            {
                if (CurrentItem is Weapon)
                {
                    if (Icon != null) Icon.sprite = CurrentItem.ItemIcon;
                    if (ItemName != null) ItemName.text = CurrentItem.ItemName;

                    if (BulletLabel != null) BulletLabel.SetActive(true);
                    if (BulletQuantity != null) 
                    {
                        string currentAmmo = ((Weapon)CurrentItem).BulletsAmounts.ToString();
                        string totalAmmo = ((Weapon)CurrentItem).TotalBullets.ToString();
                        BulletQuantity.text = "<color=red>" + currentAmmo + "</color>/" + totalAmmo;
                    }
                    return;
                }

                if (CurrentItem is JUHoldableItem || CurrentItem is ThrowableItem)
                {
                    if (Icon != null) Icon.sprite = CurrentItem.ItemIcon;
                    if (ItemName != null) ItemName.text = CurrentItem.ItemName;

                    if (BulletLabel != null) BulletLabel.SetActive(false);
                }

                if (CurrentItem is MeleeWeapon)
                {
                    if (Icon != null) Icon.sprite = CurrentItem.ItemIcon;
                    if (ItemName != null) ItemName.text = CurrentItem.ItemName;

                    if (BulletLabel != null) BulletLabel.SetActive(false);
                }
            }
        }
    }
}