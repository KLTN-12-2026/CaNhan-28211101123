using System.Collections;
using System.Collections.Generic;
using JUTPS.ActionScripts;
using UnityEngine;
using UnityEngine.UI;

namespace JUTPS.UI
{
    public class UIInteractMessages : MonoBehaviour
    {
        [Header("Item Pickup Message")]
        [SerializeField] private GameObject PickUpMessageObject;
        [SerializeField] private bool SetMessagePositionToItemPosition = true;
        [SerializeField] private Vector3 Offset;
        [SerializeField] private bool ShowItemNameOnText;
        [SerializeField] private Text WarningText;
        [SerializeField] private string PickUpLabelText = "[HOLD] TO PICK UP ";
        [Header("Vehicle Enter Message")]
        [SerializeField] private string VehicleEnterLabelText = "TO DRIVE";
        [SerializeField] private Vector3 VehicleOffset;

        private void Start()
        {
            JUGameManager.PlayerController = JUGameManager.Instance.GetPlayer();
        }
        void Update()
        {
            if (JUGameManager.PlayerController == null) { PickUpMessageObject.SetActive(false); return; }

            if (JUGameManager.PlayerController.Inventory == null)
            {
                PickUpMessageObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            // >> Vehicle Message
            if (JUGameManager.PlayerController.TryGetComponent<DriveVehicles>(out var characterDrivesVehicle))
            {
                var canEnterVehicle = characterDrivesVehicle.NearestVehicle && characterDrivesVehicle.CanEnterVehicle;

                if (characterDrivesVehicle.NearestVehicleCharacterIK)
                {
                    if (!characterDrivesVehicle.NearestVehicleCharacterIK.CharactersCanGetVehicle)
                        canEnterVehicle = false;
                }

                if (canEnterVehicle)
                {
                    PickUpMessageObject.SetActive(true);
                    UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, characterDrivesVehicle.NearestVehicle.transform.position, VehicleOffset);
                    if (WarningText)
                    {
                        WarningText.text = VehicleEnterLabelText;
                    }

                    return;
                }
            }

            // >> Item Message
            PickUpMessageObject.SetActive(JUGameManager.PlayerController.Inventory.ItemToPickUp != null);

            if (PickUpMessageObject.activeInHierarchy && SetMessagePositionToItemPosition)
            {
                UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, JUGameManager.PlayerController.Inventory.ItemToPickUp.transform.position, Offset);
            }

            if (ShowItemNameOnText && WarningText && JUGameManager.PlayerController.Inventory.ItemToPickUp != null)
            {
                WarningText.text = PickUpLabelText + JUGameManager.PlayerController.Inventory.ItemToPickUp.ItemName;
            }
        }
    }
}
