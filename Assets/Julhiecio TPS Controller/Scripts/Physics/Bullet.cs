using UnityEngine;
using JUTPS.FX;
using JUTPS.ArmorSystem;
using Photon.Pun;

namespace JUTPS.WeaponSystem
{
    [AddComponentMenu("JU TPS/Weapon System/Bullet")]
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour
    {
        public enum BulletMovementType { Physics, Calculated, Teleport }
        private GameObject Owner;
        private Vector3 OldPosition;
        private Rigidbody rb;
        
        [Header("Bullet Settings")]
        public float BulletVelocity;
        public float BulletDamage = 20;
        public float DestroyTime = 0;
        public bool HightPrecisionCollisionDetection = true;
        public bool ImpactAddForce = true;
        
        [Header("Ricochet")]
        public bool Ricochet = false;
        public float RicochetAngle = 45;
        public int MaxRicochets = 1;
        [HideInInspector] public int RicochetsCount;

        public BulletMovementType MovementType;
        [HideInInspector] public Vector3 FinalPoint;
        [HideInInspector] public Vector3 FinalPointNormal;

        private string CollidedGameObjectTag;
        public SurfaceFX[] ImpactFX;
        
        private bool hasHit = false; 

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (IsDestinationAHead() == false && MovementType != BulletMovementType.Physics) MovementType = BulletMovementType.Physics;

            if (MovementType == BulletMovementType.Physics) rb.linearVelocity = transform.forward * BulletVelocity;
            else rb.useGravity = false;

            OldPosition = transform.position;
            if (MovementType == BulletMovementType.Teleport && FinalPoint != Vector3.zero) transform.position = FinalPoint;
        }

        void FixedUpdate()
        {
            if (MovementType == BulletMovementType.Teleport) return;
            
            if (HightPrecisionCollisionDetection) CheckCollisionDetection();

            if (MovementType == BulletMovementType.Calculated && FinalPoint != Vector3.zero) MoveToFinalPoint();
            else if (MovementType == BulletMovementType.Calculated) MovePhysically();
            if (MovementType == BulletMovementType.Physics) MovePhysically();

            if (transform.position == FinalPoint && MovementType != BulletMovementType.Physics) MovementType = BulletMovementType.Physics;
        }

        private void MovePhysically() { rb.linearVelocity = transform.forward * BulletVelocity; }
        private void MoveToFinalPoint() { transform.position = Vector3.MoveTowards(transform.position, FinalPoint, BulletVelocity * Time.deltaTime); }

        private void CheckCollisionDetection()
        {
            int layerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Ignore Raycast"));

            if (Physics.Linecast(OldPosition, transform.position, out RaycastHit hit, layerMask))
            {
                FinalPoint = hit.point;
                FinalPointNormal = hit.normal;
                transform.position = hit.point;
                MovementType = BulletMovementType.Calculated;

                HandleHit(hit.collider.gameObject, hit.point, hit.normal);
            }
            OldPosition = transform.position;
        }

        void OnCollisionEnter(Collision col)
        {
            HandleHit(col.gameObject, col.contacts[0].point, col.contacts[0].normal);
        }

        private void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hasHit) return;
            if (hitObject.CompareTag("Bullet")) return;
            if (Owner != null && hitObject.transform.root == Owner.transform.root) return;

            hasHit = true;
            
            PhotonView ownerPv = (Owner != null) ? Owner.GetComponent<PhotonView>() : null;
            string killerName = (ownerPv != null && ownerPv.Owner != null) ? ownerPv.Owner.NickName : "Unknown";
            
            string weaponName = this.gameObject.name.Replace("(Clone)", "").Trim();
            
            if (Owner != null)
            {
                if (Owner.TryGetComponent(out JUTPS.InventorySystem.JUInventory inventory))
                {
                    var rightHandItem = inventory.HoldableItemInUseInRightHand;
                    if (rightHandItem != null && !string.IsNullOrEmpty(rightHandItem.ItemName))
                    {
                        weaponName = rightHandItem.ItemName;
                    }
                }
            }

            float finalDamage = BulletDamage;
            bool canDealDamage = true; 

            if (ownerPv != null && PhotonNetwork.IsConnected && !ownerPv.IsMine)
            {
                canDealDamage = false;
            }

            if (canDealDamage)
            {
                if (hitObject.TryGetComponent(out DamageableBodyPart bodyPart))
                {
                    finalDamage = BulletDamage * bodyPart.DamageMultiplier;
                    JUHealth health = hitObject.GetComponentInParent<JUHealth>();
                    if (health != null) health.DoDamage(finalDamage, killerName, weaponName, hitPoint);
                }
                else if (hitObject.GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>())
                {
                    hitObject.GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>().TakeDamage(BulletDamage);
                }
                else if (hitObject.TryGetComponent(out JUHealth health))
                {
                    health.DoDamage(BulletDamage, killerName, weaponName, hitPoint);
                }
            }

            if (ImpactAddForce && hitObject.TryGetComponent(out Rigidbody other))
            {
                other.AddForce(transform.forward * BulletVelocity * rb.mass, ForceMode.Impulse);
            }

            CollidedGameObjectTag = hitObject.tag;
            SurfaceFX.InstantiateParticleFX(ImpactFX, hitObject.tag, hitPoint, Quaternion.FromToRotation(transform.forward, hitNormal) * transform.rotation, hitObject.transform);

            if (Owner != null && Owner.CompareTag("Player") && canDealDamage)
            {
                if ((hitObject.layer == 15 || hitObject.layer == 9) && hitObject.GetComponentInParent<JUHealth>() != null)
                {
                    if (!hitObject.GetComponentInParent<JUHealth>().IsDead)
                        HitMarkerEffect.HitCheck(CollidedGameObjectTag, hitPoint, finalDamage);
                }
                else HitMarkerEffect.HitCheck(CollidedGameObjectTag);
            }

            if (Ricochet && Vector3.Dot(transform.forward, hitNormal) >= -(RicochetAngle / 360))
            {
                RicochetBullet(hitNormal);
            }
            else
            {
                DestroyBullet(DestroyTime);
            }
        }

        public void RicochetBullet(Vector3 WallNormal)
        {
            if (Ricochet == false || RicochetsCount > MaxRicochets) { DestroyBullet(DestroyTime); return; }
            RicochetsCount++;
            MovementType = BulletMovementType.Physics;
            transform.rotation = Quaternion.FromToRotation(transform.forward, Vector3.Reflect(transform.forward, WallNormal)) * transform.rotation;
            
            hasHit = false;
        }

        private bool IsDestinationAHead()
        {
            if (FinalPoint != Vector3.zero && MovementType != BulletMovementType.Physics)
                return Vector3.Dot(transform.forward, (FinalPoint - transform.position).normalized) > 0.3f;
            return true;
        }

        public void Ignore(Collider[] collider) { foreach (Collider col in collider) Physics.IgnoreCollision(GetComponent<Collider>(), col); }
        public void SetOwner(GameObject owner) { Owner = owner; }
        public GameObject GetOwner() { return Owner != null ? Owner : gameObject; }
        public void DestroyBullet(float destroyTime = 0) { Destroy(gameObject, destroyTime); }
    }
}