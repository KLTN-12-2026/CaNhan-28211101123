using UnityEngine;
using JUTPS.CharacterBrain;
using JUTPS.InventorySystem;
using JUTPS.CameraSystems;

public class MapInputBlocker : MonoBehaviour
{
    private JUCharacterBrain playerBrain;
    private JUInventory playerInventory;

    void Start()
    {
        playerBrain = GetComponent<JUCharacterBrain>();
        playerInventory = GetComponent<JUInventory>();
    }

    void Update()
    {
        if (bl_MiniMap.ActiveMiniMap != null)
        {
            bool isMapOpen = bl_MiniMap.ActiveMiniMap.IsFullScreen;

            if (playerBrain != null) playerBrain.enabled = !isMapOpen;
            
            if (playerInventory != null) playerInventory.enabled = !isMapOpen;

            if (isMapOpen)
            {
                JUCameraController.LockMouse(false, false);
            }
            else
            {
                var shop = FindObjectOfType<ShopManager>();
                if (shop == null || !shop.isShopOpen)
                {
                    JUCameraController.LockMouse(true, true);
                }
            }
        }
    }
}