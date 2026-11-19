using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using JUTPS; 
using JUTPS.WeaponSystem;
using JUTPS.InventorySystem;

public class ShopManager : MonoBehaviour
{
    [Header("Team Shop UI")]
    public GameObject teamAShopPanel;
    public GameObject teamBShopPanel;
    public TMP_Text teamAMoneyText;
    public TMP_Text teamBMoneyText;

    [Header("References")]
    public GameManager gameManager;

    public bool isShopOpen { get; private set; } = false;
    private int currentMoney = 0;
    private int lastMoney = -1;

    private Dictionary<string, int> weaponPriceDictionary = new Dictionary<string, int>()
    {
        { "Lunar", 0 },
        { "Lunar2", 0 },
        { "Mustang", 800 },
        { "Punisher", 1200 },
        { "Inferno", 2000 },
        { "Ceasar", 2000 },
        { "Solar", 2900 },
        { "Demon", 3500 },
        { "Hands", 0 }
    };

    private Dictionary<string, string> weaponCategoryDictionary = new Dictionary<string, string>()
    {
        { "Lunar", "Pistol" },
        { "Lunar2", "Pistol" },
        { "Mustang", "Pistol" },
        { "Punisher", "Primary" },
        { "Inferno", "Primary" },
        { "Ceasar", "Primary" },
        { "Solar", "Primary" },
        { "Demon", "Primary" },
        { "Hands", "Melee" }
    };

    private List<string> weaponsBoughtThisRound = new List<string>();
    private bool wasPrepPhase = false;
    private int currentArmorPricePaid = 0; 

    private void Update()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Cash"))
        {
            currentMoney = (int)PhotonNetwork.LocalPlayer.CustomProperties["Cash"];
            
            if (currentMoney != lastMoney)
            {
                lastMoney = currentMoney;
                string moneyString = "$" + currentMoney.ToString();
                if (teamAMoneyText != null) teamAMoneyText.text = moneyString;
                if (teamBMoneyText != null) teamBMoneyText.text = moneyString;
            }
        }

        bool isPrepPhase = gameManager != null && gameManager.CanOpenShop();

        if (isPrepPhase && !wasPrepPhase)
        {
            weaponsBoughtThisRound.Clear();
            currentArmorPricePaid = 0;
        }
        wasPrepPhase = isPrepPhase;

        if (isPrepPhase)
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                ToggleShop();
            }
        }
    }

    private GameObject GetActiveTeamShopPanel()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            string myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
            if (myTeam == "TeamA") return teamAShopPanel;
            if (myTeam == "TeamB") return teamBShopPanel;
        }
        return null;
    }

    private GameObject GetLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return p;
        }
        return null;
    }

    public void BuyWeapon(string weaponData)
    {
        if (gameManager == null || !gameManager.CanOpenShop()) return;
        
        string[] data = weaponData.Split(',');
        if (data.Length < 2) return;

        string weaponName = data[0];
        int price = int.Parse(data[1]);

        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;
        JUInventory inventory = localPlayer.GetComponent<JUInventory>();
        if (inventory == null) return;

        string targetCategory = weaponCategoryDictionary.ContainsKey(weaponName) ? weaponCategoryDictionary[weaponName] : "Primary";
        int refundAmount = 0;
        string oldWeaponToDisable = "";

        foreach (var item in inventory.HoldableItensRightHand)
        {
            string wName = item.ItemName;
            if (weaponCategoryDictionary.ContainsKey(wName) && weaponCategoryDictionary[wName] == targetCategory)
            {
                if (item.Unlocked)
                {
                    if (wName == weaponName) return;

                    if (weaponsBoughtThisRound.Contains(wName) && weaponPriceDictionary.ContainsKey(wName))
                    {
                        refundAmount = weaponPriceDictionary[wName];
                        weaponsBoughtThisRound.Remove(wName);
                    }
                    oldWeaponToDisable = wName;
                }
            }
        }

        int actualCost = price - refundAmount;

        if (currentMoney >= actualCost)
        {
            SubtractMoney(actualCost);
            weaponPriceDictionary[weaponName] = price;
            weaponsBoughtThisRound.Add(weaponName);

            EquipWeaponInJU(inventory, oldWeaponToDisable, weaponName);
            CloseShop();
        }
        else
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void BuyArmor(string armorData)
    {
        string[] data = armorData.Split(',');
        if (data.Length < 2) return;

        int targetArmorAmount = int.Parse(data[0]);
        int targetPrice = int.Parse(data[1]);

        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;

        JUHealth health = localPlayer.GetComponent<JUHealth>();
        if (health == null || health.Armor >= targetArmorAmount) return;

        int refundAmount = currentArmorPricePaid;
        int actualCost = targetPrice - refundAmount;

        if (currentMoney >= actualCost)
        {
            SubtractMoney(actualCost);
            health.UpdateArmorThroughNetwork(targetArmorAmount);
            currentArmorPricePaid = targetPrice;
        }
    }

    private void SubtractMoney(int amount)
    {
        int newBalance = currentMoney - amount;
        if (newBalance > 9000) newBalance = 9000; 

        currentMoney = newBalance;
        
        lastMoney = currentMoney;
        string moneyString = "$" + currentMoney.ToString();
        if (teamAMoneyText != null) teamAMoneyText.text = moneyString;
        if (teamBMoneyText != null) teamBMoneyText.text = moneyString;

        Hashtable hash = new Hashtable();
        hash.Add("Cash", newBalance);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        
        GameObject activeShopPanel = GetActiveTeamShopPanel();
        if (activeShopPanel != null)
        {
            activeShopPanel.SetActive(isShopOpen);
        }

        JUTPS.CameraSystems.JUCameraController.LockMouse(!isShopOpen, !isShopOpen);

        if (isShopOpen) MatchUIManager.Instance?.HideJUTPSUI();
        else MatchUIManager.Instance?.ShowJUTPSUI();
    }

    public void CloseShop()
    {
        if (!isShopOpen) return;

        isShopOpen = false;
        
        if (teamAShopPanel != null) teamAShopPanel.SetActive(false);
        if (teamBShopPanel != null) teamBShopPanel.SetActive(false);
        
        JUTPS.CameraSystems.JUCameraController.LockMouse(true, true);
        MatchUIManager.Instance?.ShowJUTPSUI();
    }

    public void SellPistol()
    { 
        string myTeam = "";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];

        string defaultPistol = (myTeam == "TeamA") ? "Lunar2" : "Lunar";
        
        SellWeaponByCategory("Pistol", defaultPistol);
    }
    public void SellWeapon(string category) { SellWeaponByCategory(category, ""); }

    private void SellWeaponByCategory(string category, string fallbackWeapon)
    {
        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;
        JUInventory inventory = localPlayer.GetComponent<JUInventory>();
        if (inventory == null) return;

        string weaponToSell = "";

        foreach (var item in inventory.HoldableItensRightHand)
        {
            string wName = item.ItemName;
            if (weaponCategoryDictionary.ContainsKey(wName) && weaponCategoryDictionary[wName] == category)
            {
                if (item.Unlocked && wName != fallbackWeapon)
                {
                    weaponToSell = wName;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(weaponToSell) && weaponsBoughtThisRound.Contains(weaponToSell))
        {
            int price = weaponPriceDictionary.ContainsKey(weaponToSell) ? weaponPriceDictionary[weaponToSell] : 0;
            if (price > 0)
            {
                SubtractMoney(-price);
                weaponsBoughtThisRound.Remove(weaponToSell);
                EquipWeaponInJU(inventory, weaponToSell, fallbackWeapon);
            }
        }
    }

    public void SellArmor(string armorData)
    {
        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer == null) return;
        JUHealth health = localPlayer.GetComponent<JUHealth>();
        
        if (health != null && currentArmorPricePaid > 0)
        {
            SubtractMoney(-currentArmorPricePaid); 
            health.UpdateArmorThroughNetwork(0f);
            currentArmorPricePaid = 0; 
        }
    }

    private void EquipWeaponInJU(JUInventory inventory, string weaponToDisable, string weaponToEnable)
    {
        if (!string.IsNullOrEmpty(weaponToDisable))
        {
            foreach (var item in inventory.HoldableItensRightHand)
            {
                if (item.ItemName == weaponToDisable) { item.Unlocked = false; break; }
            }
            
            foreach (var item in inventory.HoldableItensLeftHand)
            {
                if (item.ItemName == weaponToDisable) { item.Unlocked = false; break; }
            }
        }

        if (!string.IsNullOrEmpty(weaponToEnable))
        {
            foreach (var item in inventory.HoldableItensLeftHand)
            {
                if (item.ItemName == weaponToEnable)
                {
                    item.Unlocked = true;
                    break;
                }
            }

            foreach (var item in inventory.HoldableItensRightHand)
            {
                if (item.ItemName == weaponToEnable)
                {
                    item.Unlocked = true;
                    
                    var brain = inventory.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
                    if (brain != null)
                    {
                        brain.SwitchToItem(item.ItemSwitchID, true);
                    }
                    else
                    {
                        inventory.SwitchToItem(item.ItemSwitchID, true);
                    }
                    
                    break;
                }
            }
        }
    }
}