using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;
using JUTPS.ArmorSystem;
using JUTPSEditor.JUHeader;
using Photon.Pun;

namespace JUTPS.InventorySystem
{
    [AddComponentMenu("JU TPS/Third Person System/Additionals/Inventory")]
    public class JUInventory : MonoBehaviourPun
    {
        private JUTPS.CharacterBrain.JUCharacterBrain JUCharacter;
        public enum SequentialSlotsEnum { first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth }

        [JUHeader("Settings")]
        public bool UpdateOnBodyItemsVisibility = false;
        public bool DisableAllItemsOnStart = true;
        public bool IsALoot;

        [JUHeader("Items")]
        public JUHoldableItem[] HoldableItensRightHand;
        public JUHoldableItem[] HoldableItensLeftHand;
        [HideInInspector] public JUHoldableItem[] AllHoldableItems;
        public JUItem[] AllItems;

        [JUHeader("Sequential Items")]
        public SequentialSlot[] SequenceSlot = new SequentialSlot[]
        { new SequentialSlot(SequentialSlotsEnum.first, null),
      new SequentialSlot(SequentialSlotsEnum.second, null),
      new SequentialSlot(SequentialSlotsEnum.third, null),
      new SequentialSlot(SequentialSlotsEnum.fourth, null),
      new SequentialSlot(SequentialSlotsEnum.fifth, null),
      new SequentialSlot(SequentialSlotsEnum.sixth, null),
      new SequentialSlot(SequentialSlotsEnum.seventh, null),
      new SequentialSlot(SequentialSlotsEnum.eighth, null),
      new SequentialSlot(SequentialSlotsEnum.ninth, null),
      new SequentialSlot(SequentialSlotsEnum.tenth, null)};

        [JUHeader("PickUp System")]
        public bool EnablePickup = true;
        public LayerMask ItemLayer;
        public Vector3 CheckerOffset;
        public float CheckerRadious = 1;
        public bool UseDefaultInputToPickUp = true;
        public bool AutoEquipPickedUpItems = true;
        [Range(0, 1)]
        public float HoldTimeToPickUp = 0.1f;
        private float CurrentHoldTimeToPickUp;
        private float CurrentTimeToDisablePickingUpState;
        [HideInInspector] public JUItem ItemToPickUp;
        [HideInInspector] public Collider[] ItemsAround;

        [HideInInspector] public Weapon[] WeaponsRightHand;
        [HideInInspector] public Weapon[] WeaponsLeftHand;

        [HideInInspector] public JUHoldableItem HoldableItemInUseInRightHand, HoldableItemInUseInLeftHand;
        [HideInInspector] public Weapon WeaponInUseInRightHand, WeaponInUseInLeftHand;
        [HideInInspector] public MeleeWeapon MeleeWeaponInUseInRightHand, MeleeWeaponInUseInLeftHand;

        [HideInInspector] public int CurrentRightHandItemID = -1, CurrentLeftHandItemID = -1; // [-1] = Hand

        public bool IsPickingItem { get; private set; }

        public bool IsItemSelected
        {
            get => HoldableItemInUseInLeftHand || HoldableItemInUseInRightHand;
        }

        public bool IsDualWielding
        {
            get => HoldableItemInUseInLeftHand && HoldableItemInUseInRightHand;
        }

        private void Awake()
        {
            if (ItemLayer.value == 0) ItemLayer = LayerMask.GetMask("Item");
            JUCharacter = GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();

            SetupItems();
            CorrectSwitchIDs(HoldableItensLeftHand);
            CorrectSwitchIDs(HoldableItensRightHand);
        }

        private void Start()
        {
            foreach (JUItem item in AllItems)
            {
                DisableItemPhysics(item.gameObject);
            }
            
            if (DisableAllItemsOnStart)
            {
                foreach (JUItem item in AllItems)
                {
                    if (item is JUHoldableItem) (item as JUHoldableItem).RefreshItemDependencies();
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            CheckItemsAround();

            if (photonView == null || photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                if (HoldableItemInUseInLeftHand != null)
                {
                    if (HoldableItemInUseInLeftHand.ItemQuantity == 0 || HoldableItemInUseInLeftHand.Unlocked == false)
                    {
                        UnequipItem(GetGlobalItemSwitchID(HoldableItemInUseInLeftHand, this));
                    }
                }
                if (HoldableItemInUseInRightHand != null)
                {
                    if (HoldableItemInUseInRightHand.ItemQuantity == 0 || HoldableItemInUseInRightHand.Unlocked == false)
                    {
                        UnequipItem(GetGlobalItemSwitchID(HoldableItemInUseInRightHand, this));
                    }
                }
            }

            if (JUCharacter != null)
            {
                if (JUCharacter.IsDead && IsALoot == false)
                {
                    IsALoot = true;
                }
            }
        }

        [ContextMenu(" >>> Setup Items", false, 100)]
        public void SetupItems()
        {
            if (IsALoot)
            {
                AllHoldableItems = GetComponentsInChildren<JUHoldableItem>();
                HoldableItensLeftHand = GetAllItemsOnCharacterHand(gameObject, false);
                HoldableItensRightHand = GetAllItemsOnCharacterHand(gameObject, true);
                AllItems = GetComponentsInChildren<JUItem>();
                return;
            }

            if (JUCharacter == null)
            {
                JUCharacter = GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }
            if (JUCharacter != null)
            {
                if (JUCharacter.anim == null) { JUCharacter.anim = GetComponent<Animator>(); }
            }
            else { Debug.LogError("No JU Character Controller/Brain"); return; }

            if (JUCharacter.anim != null)
            {
                if (JUCharacter.anim.GetBoneTransform(HumanBodyBones.RightHand) == null || JUCharacter.anim.GetBoneTransform(HumanBodyBones.LeftHand) == null)
                {
                    if (!IsInvoking(nameof(SetupItems)))
                    {
                        Invoke(nameof(SetupItems), 0.1f);
                    }
                    return;
                }

                AllHoldableItems = GetComponentsInChildren<JUHoldableItem>();
                HoldableItensLeftHand = GetAllItemsOnCharacterHand(gameObject, false);
                HoldableItensRightHand = GetAllItemsOnCharacterHand(gameObject, true);
                AllItems = GetComponentsInChildren<JUItem>();
            }
        }

        public static void CorrectSwitchIDs(JUHoldableItem[] ItemsArray)
        {
            if (ItemsArray == null) return;

            for (int i = 0; i < ItemsArray.Length; i++)
            {
                if (ItemsArray[i].ItemSwitchID != i)
                {
                    ItemsArray[i].ItemSwitchID = i;
                }
            }
        }

        private void CheckItemsAround()
        {
            if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected) return;

            if (EnablePickup == false || CheckerRadious < 0.0001) return;

            ItemsAround = Physics.OverlapSphere(transform.TransformPoint(CheckerOffset), 1, ItemLayer);

            if (ItemsAround.Length > 0 && ItemToPickUp == null)
            {
                ItemToPickUp = ItemsAround[0].GetComponent<JUItem>() == null ? null : ItemsAround[0].GetComponent<JUItem>();
            }
            if (ItemToPickUp != null && ItemsAround.Length == 0)
            {
                ItemToPickUp = null;
            }

            if (JUInputSystem.JUInput.GetButton(JUInputSystem.JUInput.Buttons.PickupButton) && ItemToPickUp != null)
            {
                CurrentHoldTimeToPickUp += Time.deltaTime;
                if (CurrentHoldTimeToPickUp >= HoldTimeToPickUp)
                {
                    PickUp();
                }
            }

            if (IsPickingItem)
            {
                CurrentTimeToDisablePickingUpState += Time.deltaTime;
                if (CurrentTimeToDisablePickingUpState >= 0.4f)
                {
                    IsPickingItem = false;
                }
            }
            else { CurrentTimeToDisablePickingUpState = 0; }
        }

        public static JUHoldableItem[] GetAllItemsOnCharacterHand(GameObject character, bool RightHand = true)
        {
            JUHoldableItem[] items;
            Animator anim = character.GetComponent<Animator>();

            if (anim == null) { Debug.LogError("Unable to find items in hands because there is no animator"); return null; }
            if (anim.isHuman == false) { Debug.LogError("Unable to find items in hands because the animator is not a humanoid"); return null; }
            if (anim.GetBoneTransform(HumanBodyBones.RightHand) == null) { Debug.LogWarning("Unable to find items in hands because the animator is not a humanoid"); return null; }

            Transform hand = RightHand ? anim.GetBoneTransform(HumanBodyBones.RightHand) : anim.GetBoneTransform(HumanBodyBones.LeftHand);
            items = hand.GetComponentsInChildren<JUHoldableItem>();

            return items;
        }

        public static void DisableItemPhysics(GameObject item)
        {
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            Rigidbody[] rigidbodies = item.GetComponentsInChildren<Rigidbody>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = true;
            }
        }
        
        public static void EnableItemPhysic(GameObject item)
        {
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            Rigidbody[] rigidbodies = item.GetComponentsInChildren<Rigidbody>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = false;
            }
        }

        public static void PickUpNearbyItem(JUInventory InventoryToAddItem)
        {
            if (InventoryToAddItem.ItemToPickUp != null)
            {
                if (InventoryToAddItem.ItemToPickUp is JUHoldableItem)
                {
                    var PickedItemToUnlock = InventoryToAddItem.ItemToPickUp.GetComponent<JUHoldableItem>();
                    foreach (JUHoldableItem ItemInInventoryToUnlock in InventoryToAddItem.AllHoldableItems)
                    {
                        if (ItemInInventoryToUnlock.ItemName == PickedItemToUnlock.ItemName && ItemInInventoryToUnlock.IsLeftHandItem == PickedItemToUnlock.IsLeftHandItem)
                        {
                            ItemInInventoryToUnlock.Unlocked = true;
                            InventoryToAddItem.AddPickedItemData(ItemInInventoryToUnlock, PickedItemToUnlock);
                            InventoryToAddItem.RefreshInBodyItemVisibility();

                            Destroy(PickedItemToUnlock.gameObject);

                            InventoryToAddItem.IsPickingItem = true;

                            if (InventoryToAddItem.AutoEquipPickedUpItems)
                            {
                                InventoryToAddItem.JUCharacter.SwitchToItem(ItemInInventoryToUnlock.ItemSwitchID, !ItemInInventoryToUnlock.IsLeftHandItem);
                            }
                            return;
                        }
                    }
                }
                else
                {
                    foreach (JUItem ItemToAdd in InventoryToAddItem.AllItems)
                    {
                        var PickedToAdd = InventoryToAddItem.ItemToPickUp;

                        if (ItemToAdd.ItemSwitchID == PickedToAdd.ItemSwitchID && ItemToAdd.ItemName == PickedToAdd.ItemName)
                        {
                            InventoryToAddItem.AddPickedItemData(ItemToAdd, PickedToAdd);
                            InventoryToAddItem.RefreshInBodyItemVisibility();

                            Destroy(PickedToAdd.gameObject);

                            InventoryToAddItem.IsPickingItem = true;
                            return;
                        }
                    }
                }
            }
        }

        public static JUHoldableItem GetCurrentHoldableItemInUsing(JUInventory inventory, bool RightHand = true)
        {
            JUHoldableItem itemInUse = null;
            if (RightHand)
            {
                foreach (JUHoldableItem item in inventory.HoldableItensRightHand)
                {
                    if (item.ItemSwitchID == inventory.CurrentRightHandItemID) itemInUse = item;
                }
            }
            else
            {
                foreach (JUHoldableItem item in inventory.HoldableItensLeftHand)
                {
                    if (item.ItemSwitchID == inventory.CurrentLeftHandItemID) itemInUse = item;
                }
            }
            return itemInUse;
        }

        protected virtual void RefreshItemsVisibility()
        {
            for (int i = 0; i < HoldableItensRightHand.Length; i++)
            {
                if (CurrentRightHandItemID > -1)
                {
                    if (i != CurrentRightHandItemID)
                    {
                        HoldableItensRightHand[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        if (HoldableItensRightHand[i].Unlocked == true)
                        {
                            HoldableItemInUseInRightHand = HoldableItensRightHand[i];
                            HoldableItensRightHand[i].gameObject.SetActive(true);

                            if (HoldableItemInUseInRightHand is Weapon)
                            {
                                WeaponInUseInRightHand = HoldableItemInUseInRightHand.GetComponent<Weapon>();
                            }
                        }
                        else
                        {
                            HoldableItemInUseInRightHand = null;
                            WeaponInUseInRightHand = null;
                        }
                    }
                }
                else
                {
                    HoldableItensRightHand[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < HoldableItensLeftHand.Length; i++)
            {
                if (CurrentLeftHandItemID > -1)
                {
                    if (i != CurrentLeftHandItemID)
                    {
                        HoldableItensLeftHand[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        if (HoldableItensLeftHand[i].Unlocked == true)
                        {
                            HoldableItemInUseInLeftHand = HoldableItensLeftHand[i];
                            HoldableItensLeftHand[i].gameObject.SetActive(true);

                            if (HoldableItemInUseInLeftHand is Weapon)
                            {
                                WeaponInUseInLeftHand = HoldableItemInUseInLeftHand.GetComponent<Weapon>();
                            }
                        }
                        else
                        {
                            HoldableItemInUseInLeftHand = null;
                            WeaponInUseInLeftHand = null;
                        }
                    }
                }
                else
                {
                    HoldableItensLeftHand[i].gameObject.SetActive(false);
                }
            }
        }

        public void SwitchToItem(int id = -1, bool RightHand = true)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_SwitchToItem), RpcTarget.Others, id, RightHand);
                    RPC_SwitchToItem(id, RightHand);
                }
            }
            else
            {
                RPC_SwitchToItem(id, RightHand);
            }
        }

        [PunRPC]
        public void RPC_SwitchToItem(int id, bool RightHand)
        {
            if (RightHand)
            {
                if (id < -1)
                {
                    CurrentRightHandItemID = HoldableItensRightHand.Length - 1;
                    HoldableItemInUseInRightHand = HoldableItensRightHand[HoldableItensRightHand.Length - 1];
                }
                if (id == HoldableItensRightHand.Length)
                {
                    CurrentRightHandItemID = -1;
                    WeaponInUseInRightHand = null;
                    HoldableItemInUseInRightHand = null;
                }
            }
            else
            {

                if (id < -1)
                {
                    CurrentLeftHandItemID = HoldableItensLeftHand.Length - 1;
                    HoldableItemInUseInLeftHand = HoldableItensLeftHand[HoldableItensLeftHand.Length - 1];
                }
                if (id == HoldableItensLeftHand.Length)
                {
                    CurrentLeftHandItemID = -1;
                    WeaponInUseInLeftHand = null;
                    HoldableItemInUseInLeftHand = null;
                }
            }
            if (RightHand == true)
            {
                foreach (JUHoldableItem item in HoldableItensRightHand)
                {
                    if (id < 0)
                    {
                        CurrentRightHandItemID = -1;
                        WeaponInUseInRightHand = null;
                        HoldableItemInUseInRightHand = null;
                    }
                    else
                    {
                        if (item.ItemSwitchID == id)
                        {
                            if (HoldableItemInUseInRightHand == item) return;

                            HoldableItemInUseInRightHand = item;

                            if (item is Weapon) { WeaponInUseInRightHand = item.GetComponent<Weapon>(); } else { WeaponInUseInRightHand = null; }
                            if (item is MeleeWeapon) { MeleeWeaponInUseInRightHand = item.GetComponent<MeleeWeapon>(); } else { MeleeWeaponInUseInRightHand = null; }

                            CurrentRightHandItemID = id;
                        }
                    }
                }
            }
            if (RightHand == false)
            {
                foreach (JUHoldableItem item in HoldableItensLeftHand)
                {
                    if (id < 0)
                    {
                        CurrentLeftHandItemID = id;
                        WeaponInUseInLeftHand = null;
                        HoldableItemInUseInLeftHand = null;
                    }
                    else
                    {
                        if (item.ItemSwitchID == id)
                        {
                            if (HoldableItemInUseInLeftHand == item) return;

                            HoldableItemInUseInLeftHand = item;

                            if (item is Weapon) { WeaponInUseInLeftHand = item.GetComponent<Weapon>(); } else { WeaponInUseInLeftHand = null; }
                            if (item is MeleeWeapon) { MeleeWeaponInUseInLeftHand = item.GetComponent<MeleeWeapon>(); } else { MeleeWeaponInUseInLeftHand = null; }

                            CurrentLeftHandItemID = id;
                        }
                    }
                }
            }
            UpdateItemInUse();
            RefreshItemsVisibility();
            RefreshInBodyItemVisibility();
        }

        public static int GetGlobalItemSwitchID(JUItem item, JUInventory inventory)
        {
            int global_id = -3;
            if (item == null) return global_id;

            for (int i = 0; i < inventory.AllItems.Length; i++)
            {
                if (inventory.AllItems[i].ItemName == item.ItemName) global_id = i;
            }
            return global_id;
        }

        public int GetNextUnlockedItemID(int CurrentID, bool LocalID = true, bool RightHand = true)
        {
            int item_id;
            if (LocalID)
            {
                if (RightHand)
                {
                    item_id = NextUnlockedItemLocalIndexRightHand(CurrentID);
                }
                else
                {
                    item_id = NextUnlockedItemLocalIndexLeftHand(CurrentID);
                }
            }
            else
            {
                if (RightHand)
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensRightHand[NextUnlockedItemLocalIndexRightHand(CurrentID)], inventory: this);
                }
                else
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensLeftHand[NextUnlockedItemLocalIndexLeftHand(CurrentID)], inventory: this);
                }
            }
            return item_id;
        }
        
        public int GetPreviousUnlockedItemID(int CurrentID, bool LocalID = true, bool RightHand = true)
        {
            int item_id;
            if (LocalID)
            {
                if (RightHand)
                {
                    item_id = PreviousUnlockedItemLocalIndexRightHand(CurrentID);
                }
                else
                {
                    item_id = PreviousUnlockedItemLocalIndexLeftHand(CurrentID);
                }
            }
            else
            {
                if (RightHand)
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensRightHand[PreviousUnlockedItemLocalIndexRightHand(CurrentID)], inventory: this);
                }
                else
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensLeftHand[PreviousUnlockedItemLocalIndexLeftHand(CurrentID)], inventory: this);
                }
            }
            return item_id;
        }


        public void SetSequentialSlotItem(SequentialSlotsEnum slot, JUItem item)
        {
            foreach (SequentialSlot s in SequenceSlot)
            {
                if (s.SelectedSlot == slot)
                {
                    s.ItemInThisSlot = item;
                }
            }
        }
        
        public JUItem GetSequentialSlotItem(SequentialSlotsEnum slot)
        {
            JUItem itemSlotToReturn = null;
            foreach (SequentialSlot s in SequenceSlot)
            {
                if (s.SelectedSlot == slot) itemSlotToReturn = s.ItemInThisSlot;
            }
            return itemSlotToReturn;
        }


        protected int NextUnlockedItemLocalIndexRightHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i < HoldableItensRightHand.Length; i++)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensRightHand[i].ItemSwitchID;
                        return HoldableItensRightHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        
        protected int NextUnlockedItemLocalIndexLeftHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i < HoldableItensLeftHand.Length; i++)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensLeftHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensLeftHand[i].ItemSwitchID;
                        return HoldableItensLeftHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        
        protected int PreviousUnlockedItemLocalIndexRightHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i > -1; i--)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensRightHand[i].ItemSwitchID;
                        return HoldableItensRightHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        
        protected int PreviousUnlockedItemLocalIndexLeftHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i > -1; i--)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensLeftHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensLeftHand[i].ItemSwitchID;
                        return HoldableItensLeftHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }

        public void RefreshInBodyItemVisibility()
        {
            if (UpdateOnBodyItemsVisibility == false) return;
            for (int i = HoldableItensRightHand.Length - 1; i > -1; i--)
            {
                if (i != CurrentRightHandItemID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true && HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(true);
                    }
                    else if (HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (HoldableItensRightHand[i].Unlocked == true && HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        public void PickUp()
        {
            if (ItemToPickUp == null) return;
            PickUpNearbyItem(this);
        }

        public void AddPickedItemData(JUItem OnInventoryItem, JUItem ItemToPickup)
        {
            GetLootItem(OnInventoryItem, ItemToPickUp);
        }

        public JUItem GetItem(int globalID) { return AllItems[globalID]; }

        public JUItem GetItem(string itemName)
        {
            JUItem itemToReturn = null;
            foreach (JUItem currentItem in AllItems)
            {
                if (currentItem.ItemName == itemName || currentItem.name == itemName)
                {
                    itemToReturn = currentItem;
                }
            }
            return itemToReturn;
        }

        public static JUItem GetItem(JUInventory inventory, string itemName)
        {
            JUItem itemToReturn = null;
            foreach (JUItem currentItem in inventory.AllItems)
            {
                if (currentItem.ItemName == itemName || currentItem.name == itemName)
                {
                    itemToReturn = currentItem;
                }
            }
            return itemToReturn;
        }

        public void GetLootItem(JUItem itemOnThisInventory, JUItem itemOnLoot)
        {
            if (itemOnThisInventory is Weapon)
            {
                Weapon localWeapon = itemOnThisInventory as Weapon;
                Weapon lootWeapon = itemOnLoot as Weapon;

                if (itemOnThisInventory.ItemQuantity == 0)
                {
                    itemOnThisInventory.ItemQuantity += 1;
                    itemOnThisInventory.Unlocked = true;

                    localWeapon.TotalBullets = lootWeapon.TotalBullets;
                    localWeapon.BulletsAmounts = lootWeapon.BulletsAmounts;

                    lootWeapon.Unlocked = false;
                }
                else
                {
                    localWeapon.TotalBullets += lootWeapon.TotalBullets;
                    itemOnThisInventory.ItemQuantity += 1;
                }
                return;
            }
            
            itemOnThisInventory.ItemQuantity = itemOnThisInventory.Unlocked ? itemOnLoot.ItemQuantity : (itemOnThisInventory.ItemQuantity + itemOnLoot.ItemQuantity);
            itemOnThisInventory.ItemQuantity = Mathf.Clamp(itemOnThisInventory.ItemQuantity, 0, itemOnThisInventory.MaxItemQuantity);

            if (itemOnThisInventory is MeleeWeapon)
            {
                MeleeWeapon localMeleeWeapon = itemOnThisInventory as MeleeWeapon;
                MeleeWeapon _lootMeleeWeapon = itemOnLoot as MeleeWeapon;

                if (localMeleeWeapon.Unlocked == false)
                {
                    localMeleeWeapon.MeleeWeaponHealth = _lootMeleeWeapon.MeleeWeaponHealth;
                    localMeleeWeapon.Unlocked = true;
                    _lootMeleeWeapon.Unlocked = false;
                }
                else
                {
                    float oldLocalDamage = localMeleeWeapon.MeleeWeaponHealth;
                    localMeleeWeapon.MeleeWeaponHealth = _lootMeleeWeapon.MeleeWeaponHealth;
                    _lootMeleeWeapon.MeleeWeaponHealth = oldLocalDamage;
                }
            }

            if (itemOnThisInventory is Armor)
            {
                Armor localAmor = itemOnThisInventory as Armor;
                Armor _lootArmor = itemOnLoot as Armor;

                if (localAmor.Unlocked == false)
                {
                    localAmor.Health = _lootArmor.Health;
                    localAmor.Unlocked = true;
                    _lootArmor.Unlocked = false;
                }
                else
                {
                    float oldLocalArmorHealth = localAmor.Health;
                    localAmor.Health = _lootArmor.Health;
                    _lootArmor.Health = oldLocalArmorHealth;
                }
            }
        }
        
        public void DropItem(int ID, bool IsRightHandItem = true)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_DropItemWithHand), RpcTarget.Others, ID, IsRightHandItem);
                    RPC_DropItemWithHand(ID, IsRightHandItem);
                }
            }
            else
            {
                RPC_DropItemWithHand(ID, IsRightHandItem);
            }
        }

        [PunRPC]
        public void RPC_DropItemWithHand(int ID, bool IsRightHandItem)
        {
            Vector3 DropPosition = transform.position + transform.forward * 0.2f + transform.up * 0.5f;
            if (IsRightHandItem)
            {
                if (HoldableItensRightHand[ID].Unlocked == false) return;

                Vector3 worldScale = HoldableItensRightHand[ID].transform.lossyScale;

                var dropedItem = (GameObject)Instantiate(HoldableItensRightHand[ID].gameObject, DropPosition, Quaternion.identity);
                dropedItem.transform.localScale = worldScale;
                EnableItemPhysic(dropedItem);
                dropedItem.SetActive(true);
                dropedItem.layer = 14;                
                
                HoldableItensRightHand[ID].RemoveItem();

                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: true);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: true);
                }
            }
            else
            {
                if (HoldableItensRightHand[ID].Unlocked == false) return;

                var dropedItem = (GameObject)Instantiate(HoldableItensLeftHand[ID].gameObject, DropPosition, Quaternion.identity);
                EnableItemPhysic(dropedItem);
                dropedItem.layer = 14;

                HoldableItensLeftHand[ID].RemoveItem();

                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: false);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: false);
                }
            }
            RefreshInBodyItemVisibility();
        }

        public void DropItem(int ID)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_DropItem), RpcTarget.Others, ID);
                    RPC_DropItem(ID);
                }
            }
            else
            {
                RPC_DropItem(ID);
            }
        }

        [PunRPC]
        public void RPC_DropItem(int ID)
        {
            Vector3 DropPosition = transform.position + transform.forward * 0.2f + transform.up * 0.5f;
            if (ID < 0) return;
            if (ID >= AllItems.Length) return;
            if (AllItems[ID].Unlocked == false) return;

            Vector3 worldScale = AllItems[ID].transform.lossyScale;

            if (AllItems[ID] is Armor)
            {
                foreach (GameObject armorPart in (AllItems[ID] as Armor).Parts)
                {
                    armorPart.transform.parent = AllItems[ID].transform;
                }
            }

            var dropedItem = (GameObject)Instantiate(AllItems[ID].gameObject, DropPosition, Quaternion.identity);
            dropedItem.transform.localScale = worldScale;
            EnableItemPhysic(dropedItem);
            dropedItem.SetActive(true);
            dropedItem.layer = 14;

            AllItems[ID].RemoveItem();
            if (AllItems[ID] is Armor && AllItems[ID].gameObject.activeInHierarchy) { AllItems[ID].gameObject.SetActive(false); }

            if (AllItems[ID] is JUHoldableItem)
            {
                if (AllItems[ID].gameObject.activeInHierarchy)
                {
                    if (JUCharacter == null)
                    {
                        SwitchToItem(-1, RightHand: true);
                    }
                    else
                    {
                        JUCharacter.SwitchToItem(-1, RightHand: true);
                    }
                }
            }
            RefreshInBodyItemVisibility();
        }

        public void EquipItem(int ID)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_EquipItem), RpcTarget.Others, ID);
                    RPC_EquipItem(ID);
                }
            }
            else
            {
                RPC_EquipItem(ID);
            }
        }

        [PunRPC]
        public void RPC_EquipItem(int ID)
        {
            if (ID < 0 || ID >= AllItems.Length) return;

            if (AllItems[ID].Unlocked == false) return;

            if (AllItems[ID] is Armor)
            {
                AllItems[ID].gameObject.SetActive(true);
                return;
            }
            if ((AllItems[ID].GetType()).IsSubclassOf(typeof(JUHoldableItem)) == false)
            {
                AllItems[ID].gameObject.SetActive(true);
                return;
            }

            JUHoldableItem holdableItem = AllItems[ID] as JUHoldableItem;

            if (JUCharacter == null)
            {
                SwitchToItem(AllItems[ID].ItemSwitchID, !holdableItem.IsLeftHandItem);
                return;
            }
            else
            {
                JUCharacter.SwitchToItem(AllItems[ID].ItemSwitchID, !holdableItem.IsLeftHandItem);
            }
        }

        public void UnequipItem(int ID)
        {
            if (photonView != null && PhotonNetwork.IsConnected)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(RPC_UnequipItem), RpcTarget.Others, ID);
                    RPC_UnequipItem(ID);
                }
            }
            else
            {
                RPC_UnequipItem(ID);
            }
        }

        [PunRPC]
        public void RPC_UnequipItem(int ID)
        {
            if (ID < 0 || ID >= AllItems.Length || AllItems[ID] == null) return;

            if (AllItems[ID] is Armor)
            {
                AllItems[ID].gameObject.SetActive(false);
                return;
            }

            if ((AllItems[ID].GetType()).IsSubclassOf(typeof(JUHoldableItem)) == false)
            {
                AllItems[ID].gameObject.SetActive(false);
                if ((AllItems[ID] as JUHoldableItem).IsLeftHandItem == false)
                {
                    HoldableItemInUseInRightHand = null;
                }
                else
                {
                    HoldableItemInUseInLeftHand = null;
                }
                return;
            }

            JUHoldableItem holdableItem = AllItems[ID] as JUHoldableItem;

            if (!holdableItem.IsLeftHandItem)
            {
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: true);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: true);
                }
            }
            else
            {
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: false);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: false);
                }
            }
        }

        protected void UpdateItemInUse()
        {
            HoldableItemInUseInLeftHand = GetCurrentHoldableItemInUsing(inventory: this, RightHand: false);
            HoldableItemInUseInRightHand = GetCurrentHoldableItemInUsing(inventory: this, RightHand: true);

            if (HoldableItemInUseInLeftHand is Weapon) WeaponInUseInLeftHand = (Weapon)HoldableItemInUseInLeftHand; else WeaponInUseInLeftHand = null;
            if (HoldableItemInUseInRightHand is Weapon) WeaponInUseInRightHand = (Weapon)HoldableItemInUseInRightHand; else WeaponInUseInRightHand = null;

            if (HoldableItemInUseInLeftHand is MeleeWeapon) MeleeWeaponInUseInLeftHand = (MeleeWeapon)HoldableItemInUseInLeftHand; else MeleeWeaponInUseInLeftHand = null;
            if (HoldableItemInUseInRightHand is MeleeWeapon) MeleeWeaponInUseInRightHand = (MeleeWeapon)HoldableItemInUseInRightHand; else MeleeWeaponInUseInRightHand = null;
        }

        private void OnDrawGizmos()
        {
            if (!EnablePickup) return;
            Gizmos.DrawWireSphere(transform.TransformPoint(CheckerOffset), CheckerRadious);
        }

        [System.Serializable]
        public class SequentialSlot
        {
            public SequentialSlotsEnum SelectedSlot;
            public JUItem ItemInThisSlot;

            public SequentialSlot(SequentialSlotsEnum slot, JUItem itemToSlot)
            {
                SelectedSlot = slot;
                ItemInThisSlot = itemToSlot;
            }
        }
    }
}