using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEditor;
using Photon.Pun;

namespace JUTPS.PhysicsScripts
{
    public class BoneTransformInfo
    {
        public Transform Transform;
        public Vector3 StoredPosition;
        public Quaternion StoredRotation;
        public BoneTransformInfo(Transform t)
        {
            Transform = t;
        }
    }

    [AddComponentMenu("JU TPS/Physics/Advanced Ragdoll Controller")]
    public class AdvancedRagdollController : MonoBehaviourPun
    {
        private Vector3 groundNormal;
        private GameObject character;
        private Animator animator;
        private bool isStartAnimatorUpdateInPhysics;
        private Rigidbody rigidBody;

        [HideInInspector]
        public bool RagdollEnabled;

        public enum RagdollState
        {
            Animated,
            WaitStablePosition,
            Ragdolled,
            BlendToAnim,
        }
        public RagdollState State;
        private RagdollState _lastSyncedState; 
        private bool GetUpFromBelly;

        public Transform[] AllBones;
        public Rigidbody[] RagdollBones;

        public Transform Hips;
        public Transform HipsParent;
        private Transform Head;
        private Rigidbody HipsRigidbody;

        private List<BoneTransformInfo> bones = new List<BoneTransformInfo>();

        public float BlendAmount;

        [Range(1, 5f)]
        public float TimeToGetUp = 3f;

        [Range(1, 5f)]
        public float BlendSpeed = 2f;

        public float RagdollDrag = 0.5f;

        public bool RagdollWhenPressKeyG;
        public bool ViewHumanBodyBones;
        public bool ViewBodyPhysics;
        public bool ViewBodyDirection;

        public bool FilterByBoneLayer = true;

        private void Start()
        {
            Invoke(nameof(StartAdvancedRagdollController), 0.001f);
        }

        private void Update()
        {
            if (photonView == null || photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                if (RagdollWhenPressKeyG && Keyboard.current != null && Keyboard.current.gKey.isPressed)
                {
                    State = RagdollState.Ragdolled;
                }
            }

            if (photonView != null && PhotonNetwork.IsConnected && photonView.IsMine)
            {
                if (State != _lastSyncedState)
                {
                    photonView.RPC(nameof(RPC_SyncRagdollState), RpcTarget.Others, (int)State);
                    _lastSyncedState = State;
                }
            }

            RagdollStatesController();
        }

        [PunRPC]
        public void RPC_SyncRagdollState(int syncedState)
        {
            State = (RagdollState)syncedState;
        }

        private void LateUpdate()
        {
            BlendRagdollToAnimator();
        }

        private void OnDisable()
        {
            if (Hips != null) Hips.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (Hips != null) Hips.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (Hips != null) Destroy(Hips.gameObject);
        }

        public void StartAdvancedRagdollController()
        {
            character = this.gameObject;
            animator = GetComponent<Animator>();
            rigidBody = GetComponent<Rigidbody>();
            
            if (animator == null || character == null) return;
            
            isStartAnimatorUpdateInPhysics = (animator.updateMode == AnimatorUpdateMode.Fixed);

            Hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            HipsParent = Hips.parent;
            HipsRigidbody = Hips.GetComponent<Rigidbody>();
            Head = animator.GetBoneTransform(HumanBodyBones.Head);

            RagdollBones = Hips.GetComponentsInChildren<Rigidbody>();
            AllBones = Hips.GetComponentsInChildren<Transform>();

            if (FilterByBoneLayer == true)
            {
                List<Transform> AllBonesFiltered = new List<Transform>();
                foreach (Transform bone in AllBones)
                {
                    if (bone.gameObject.layer == 15 || bone.gameObject.layer == 9)
                    {
                        AllBonesFiltered.Add(bone);
                    }
                }
                AllBones = AllBonesFiltered.ToArray();

                List<Rigidbody> RagdollBonesFiltered = new List<Rigidbody>();
                foreach (Rigidbody rbBone in RagdollBones)
                {
                    if (rbBone.gameObject.layer == 15 || rbBone.gameObject.layer == 9)
                    {
                        RagdollBonesFiltered.Add(rbBone);
                    }
                }
                RagdollBones = RagdollBonesFiltered.ToArray();
            }

            foreach (var bone in AllBones)
            {
                bones.Add(new BoneTransformInfo(bone.transform));
            }

            SetActiveRagdoll(false);
        }

        public void RagdollStatesController()
        {
            if (HipsRigidbody == null)
            {
                State = RagdollState.Animated;
                return;
            }
            
            if (State == RagdollState.Animated && animator.enabled == false)
            {
                animator.enabled = true;
                RagdollEnabled = false;
                SetActiveRagdoll(false);
                Hips.parent = HipsParent;
            }

            if (State == RagdollState.Ragdolled)
            {
                if (RagdollEnabled == false)
                {
                    SetActiveRagdoll(true, true);
                    Hips.parent = null;
                }

                JUHealth health = GetComponent<JUHealth>();
                bool isDead = health != null && health.IsDead;

                if (HipsRigidbody.linearVelocity.magnitude < 0.01f && IsInvoking("SetWaitStablePositionInvoked") == false)
                {
                    if (!isDead) 
                    {
                        Invoke("SetWaitStablePositionInvoked", TimeToGetUp);
                    }
                }
                if (Hips.parent == null)
                {
                    RaycastHit hipshitground;
                    LayerMask groundmask = LayerMask.GetMask("Default", "Terrain", "Walls");
                    Physics.Raycast(Hips.position, -transform.up, out hipshitground, 0.5f, groundmask);

                    var HipsPosition = Hips.position;
                    HipsPosition.y = hipshitground.point.y;
                    transform.position = HipsPosition;

                    groundNormal = hipshitground.normal != Vector3.zero ? hipshitground.normal : Vector3.up;
                    
                    if (!isDead)
                    {
                        SetTransformRotationToBodyDirection();
                    }
                }
                transform.position = Hips.position;
            }

            if (State == RagdollState.WaitStablePosition)
            {
                Hips.parent = HipsParent;

                foreach (var Bone in bones)
                {
                    Bone.StoredPosition = Bone.Transform.localPosition;
                    Bone.StoredRotation = Bone.Transform.localRotation;
                }
                GetUp();
                State = RagdollState.BlendToAnim;
            }

            if (State == RagdollState.BlendToAnim)
            {
                animator.updateMode = AnimatorUpdateMode.Normal;
            }
            else if (isStartAnimatorUpdateInPhysics)
            {
                animator.updateMode = AnimatorUpdateMode.Fixed;
            }

            RaycastHit hit;
            LayerMask mask = LayerMask.GetMask("Default", "Terrain", "Walls");
            if (Physics.Raycast(Hips.position, Hips.forward, out hit, 0.5f, mask))
            {
                GetUpFromBelly = true;
            }
            else
            {
                GetUpFromBelly = false;
            }
        }

        public void BlendRagdollToAnimator()
        {
            if (State == RagdollState.BlendToAnim)
            {
                foreach (var Bone in bones)
                {
                    Bone.Transform.localPosition = Vector3.Slerp(Bone.Transform.localPosition, Bone.StoredPosition, BlendAmount);
                    Bone.Transform.localRotation = Quaternion.Slerp(Bone.Transform.localRotation, Bone.StoredRotation, BlendAmount);
                }

                BlendAmount = Mathf.MoveTowards(BlendAmount, 0.0f, BlendSpeed * Time.deltaTime);

                if (BlendAmount <= 0)
                {
                    State = RagdollState.Animated;
                }

                if (IsInvoking("SetWaitStablePositionInvoked"))
                {
                    CancelInvoke("SetWaitStablePositionInvoked");
                }
            }
        }

        public void SetActiveRagdoll(bool Enabled, bool Inertia = default)
        {
            foreach (Rigidbody boneRigidbody in RagdollBones)
            {
                boneRigidbody.isKinematic = !Enabled;
            }
            RagdollEnabled = Enabled;
            animator.enabled = !Enabled;

            if (Inertia == true)
            {
                foreach (Rigidbody rb in RagdollBones)
                {
                    rb.linearVelocity = GetComponent<Rigidbody>().linearVelocity;
                    rb.angularVelocity = Vector3.zero;
                    rb.angularDamping = RagdollDrag;
                }
            }
            if (Enabled == true)
            {
                rigidBody.isKinematic = true;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            else
            {
                rigidBody.isKinematic = false;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            character.GetComponent<CapsuleCollider>().enabled = !Enabled;
        }

        private void SetWaitStablePositionInvoked()
        {
            JUHealth health = GetComponent<JUHealth>();
            if (health != null && health.IsDead) return;

            if (photonView == null || photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                State = RagdollState.WaitStablePosition;
            }
        }

        public void GetUp()
        {
            SetTransformRotationToBodyDirection();
            SetActiveRagdoll(false);

            BlendAmount = 1f;

            if (GetUpFromBelly)
            {
                animator.Play("Get Up From Belly", 0, 0);
            }
            else
            {
                animator.Play("Get Up From Back", 0, 0);
            }
        }

        Vector3 UpDirection;
        public void SetTransformRotationToBodyDirection()
        {
            UpDirection = transform.up;
            UpDirection = Vector3.Lerp(UpDirection, groundNormal, 5 * Time.deltaTime);

            transform.rotation = Quaternion.FromToRotation(transform.forward, BodyDirection()) * transform.rotation;
            Hips.rotation = Quaternion.FromToRotation(BodyDirection(), transform.forward) * Hips.rotation;
            transform.rotation = Quaternion.FromToRotation(transform.up, UpDirection) * transform.rotation;
        }

        public Vector3 BodyDirection()
        {
            Vector3 ragdolldirection = Hips.position - Head.position;
            ragdolldirection.y = 0;
            if (GetUpFromBelly)
            {
                return -ragdolldirection.normalized;
            }
            else
            {
                return ragdolldirection.normalized;
            }
        }

        public void Fall()
        {
            if (photonView == null || photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                State = RagdollState.Ragdolled;
            }
        }
    }
}