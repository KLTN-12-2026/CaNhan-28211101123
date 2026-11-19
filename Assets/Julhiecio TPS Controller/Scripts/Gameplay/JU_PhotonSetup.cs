using UnityEngine;
using Photon.Pun;
using JUTPS;
using JUTPS.CameraSystems;
using JUTPS.InventorySystem;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;

[RequireComponent(typeof(PhotonView))]
public class JU_PhotonSetup : MonoBehaviourPun, IPunObservable 
{
    private JUCharacterController juController;
    private JUInventory juInventory; 
    private Animator anim;
    private JUHealth juHealth; 
    private int lastWeaponID = -2; 

    private Vector3 syncedLookAtPosition;
    private bool syncedFiringMode;
    private bool syncedIsAiming;

    private Vector3 smoothLookAtPosition; 
    private Quaternion currentPivotRotation;
    private float rightIKWeight;
    private float leftIKWeight;

    void Awake()
    {
        if (photonView.IsMine)
        {
            var myCam = GetComponentInChildren<TPSCameraController>(true);
            if (myCam != null)
            {
                myCam.TargetToFollow = this.transform;
            }
        }
    }

    void Start()
    {
        juController = GetComponent<JUCharacterController>();
        juInventory = GetComponent<JUInventory>();
        juHealth = GetComponent<JUHealth>();
        anim = GetComponent<Animator>();

        if (!photonView.IsMine)
        {
            DisableRemotePlayer();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            Vector3 lookPos = juController != null ? juController.GetLookPosition() : Vector3.zero;
            bool isFiring = juController != null && juController.FiringMode;
            bool isAiming = juController != null && juController.IsAiming;

            stream.SendNext(lookPos);
            stream.SendNext(isFiring);
            stream.SendNext(isAiming);
        }
        else
        {
            syncedLookAtPosition = (Vector3)stream.ReceiveNext();
            syncedFiringMode = (bool)stream.ReceiveNext();
            syncedIsAiming = (bool)stream.ReceiveNext();
        }
    }

    void Update()
    {
        if (!photonView.IsMine) 
        {
            if (juController != null && juController.enabled)
            {
                juController.UseDefaultControllerInput = false;
                juController.enabled = false;
            }

            if (anim != null && juController != null && (juHealth == null || !juHealth.IsDead))
            {
                juController.IsAiming = syncedIsAiming;
                juController.FiringMode = syncedFiringMode;

                if (juController.HoldableItemInUseRightHand is Weapon wR) wR.GenerateProceduralAnimation = false;
                if (juController.HoldableItemInUseLeftHand is Weapon wL) wL.GenerateProceduralAnimation = false;
                if (smoothLookAtPosition == Vector3.zero) smoothLookAtPosition = syncedLookAtPosition;
                smoothLookAtPosition = Vector3.Lerp(smoothLookAtPosition, syncedLookAtPosition, 15f * Time.deltaTime);

                if (juController.PivotItemRotation != null && syncedFiringMode && smoothLookAtPosition != Vector3.zero)
                {
                    Vector3 aimDir = smoothLookAtPosition - juController.PivotItemRotation.transform.position;
                    if (aimDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(aimDir);
                        targetRot = Quaternion.Euler(targetRot.eulerAngles.x, targetRot.eulerAngles.y, 0);
                        currentPivotRotation = Quaternion.Lerp(currentPivotRotation, targetRot, 25f * Time.deltaTime);
                    }
                }
                else
                {
                    currentPivotRotation = Quaternion.Lerp(currentPivotRotation, transform.rotation, 15f * Time.deltaTime);
                }

                bool hasLeftHandIK = juController.HoldableItemInUseRightHand != null && juController.HoldableItemInUseRightHand.OppositeHandPosition != null;
                float targetLegs = syncedFiringMode ? 1f : 0f;
                float targetRightArm = syncedFiringMode ? 1f : 0f;
                float targetBothArms = syncedFiringMode && hasLeftHandIK ? 1f : 0f;

                float blendSpeed = 15f * Time.deltaTime; 

                anim.SetLayerWeight(1, Mathf.MoveTowards(anim.GetLayerWeight(1), targetLegs, blendSpeed));
                anim.SetLayerWeight(2, Mathf.MoveTowards(anim.GetLayerWeight(2), targetRightArm, blendSpeed));
                anim.SetLayerWeight(3, Mathf.MoveTowards(anim.GetLayerWeight(3), 0f, blendSpeed)); 
                anim.SetLayerWeight(4, Mathf.MoveTowards(anim.GetLayerWeight(4), targetBothArms, blendSpeed));

                if (juController.AnimatorParameters != null)
                {
                    anim.SetBool(juController.AnimatorParameters.FireMode, syncedFiringMode);
                }
            }
            return;
        }

        if (juController != null)
        {
            if (GameManager.Instance != null && !GameManager.isMatchStarted)
                juController.UseDefaultControllerInput = false;
            else
                juController.UseDefaultControllerInput = true;
        }

        if (juInventory != null)
        {
            int currentID = juInventory.CurrentRightHandItemID;
            if (currentID != lastWeaponID)
            {
                photonView.RPC(nameof(RPC_SyncWeapon), RpcTarget.OthersBuffered, currentID);
                lastWeaponID = currentID;
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!photonView.IsMine && anim != null && juController != null && (juHealth == null || !juHealth.IsDead))
        {
            if (juController.PivotItemRotation != null && juController.HumanoidSpine != null)
            {
                juController.PivotItemRotation.transform.position = juController.HumanoidSpine.position;
                juController.PivotItemRotation.transform.rotation = currentPivotRotation;
            }

            bool hasLeftHandIK = juController.HoldableItemInUseRightHand != null && juController.HoldableItemInUseRightHand.OppositeHandPosition != null;
            float targetIK = syncedFiringMode ? 1f : 0f;
            
            rightIKWeight = Mathf.MoveTowards(rightIKWeight, targetIK, 10f * Time.deltaTime);
            leftIKWeight = Mathf.MoveTowards(leftIKWeight, hasLeftHandIK ? targetIK : 0f, 10f * Time.deltaTime);

            if (syncedFiringMode || rightIKWeight > 0f)
            {
                anim.SetLookAtWeight(rightIKWeight, 0.3f, 0.8f, 1f, 0.5f);
                anim.SetLookAtPosition(smoothLookAtPosition);

                if (juController.HoldableItemInUseRightHand != null && juController.WeaponHoldingPositions != null)
                {
                    int wieldID = juController.HoldableItemInUseRightHand.ItemWieldPositionID;
                    if (wieldID >= 0 && wieldID < juController.WeaponHoldingPositions.WeaponPositionTransform.Count)
                    {
                        Transform rightHandTarget = juController.WeaponHoldingPositions.WeaponPositionTransform[wieldID];

                        rightHandTarget.localPosition = juController.WeaponHoldingPositions._storedLocalPositions[wieldID];
                        rightHandTarget.localRotation = juController.WeaponHoldingPositions._storedLocalRotations[wieldID];

                        anim.SetIKPositionWeight(AvatarIKGoal.RightHand, rightIKWeight);
                        anim.SetIKRotationWeight(AvatarIKGoal.RightHand, rightIKWeight);
                        anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                        anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);

                        if (hasLeftHandIK)
                        {
                            Transform leftHandTarget = juController.HoldableItemInUseRightHand.OppositeHandPosition;
                            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftIKWeight);
                            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftIKWeight);
                            anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
                        }
                        else
                        {
                            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                        }
                    }
                }
            }
            else
            {
                anim.SetLookAtWeight(0f);
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            }
        }
    }

    private void DisableRemotePlayer()
    {
        if (juController != null) 
        {
            juController.UseDefaultControllerInput = false;
            juController.enabled = false; 
        }

        var footPlacement = GetComponent<JUTPS.JUFootPlacement>();
        if (footPlacement != null) footPlacement.enabled = false;

        var footStep = GetComponent<JUTPS.FX.JUFootstep>();
        if (footStep != null) footStep.enabled = false;

        var ragdoll = GetComponent<JUTPS.PhysicsScripts.AdvancedRagdollController>();
        //if (ragdoll != null) ragdoll.enabled = false;

        var thirdPersonCamera = GetComponentInChildren<JUCameraController>(true);
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.gameObject.SetActive(false); 
        }

        var audioListener = GetComponentInChildren<AudioListener>(true);
        if (audioListener != null) audioListener.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; 
        
        var prbv = GetComponent<PhotonRigidbodyView>();
        if (prbv != null) prbv.enabled = false;
    }

    [PunRPC]
    public void RPC_SyncWeapon(int weaponID)
    {
        if (juController == null) juController = GetComponent<JUCharacterController>();
        if (juInventory == null) juInventory = GetComponent<JUInventory>();

        if (juController != null && juInventory != null)
        {
            juController.SwitchToItem(weaponID, true);

            if (juController.HoldableItemInUseRightHand != null)
            {
                juController.HoldableItemInUseRightHand.gameObject.SetActive(true);
            }
        }
    }

    [PunRPC]
    public void RPC_PerformShot(Vector3 camPos, Vector3 shootDir)
    {
        if (juController != null && juController.WeaponInUseRightHand != null)
        {
            juController.WeaponInUseRightHand.CameraPosition = camPos;
            juController.WeaponInUseRightHand.ShootDirection = shootDir;
            juController.WeaponInUseRightHand.ExecuteShot();
        }
    }

    [PunRPC]
    public void RPC_Reload()
    {
        if (juController != null && juController.WeaponInUseRightHand != null)
        {
            juController.WeaponInUseRightHand.ExecuteReload();
        }
    }

    [PunRPC]
    public void RPC_EmitBulletShell_Global(int dummyParameter)
    {
        if (juController != null && juController.WeaponInUseRightHand != null)
        {
            juController.WeaponInUseRightHand.EmitBulletShell();
        }
    }
}