using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Photon.Pun; 

namespace JUTPS.AI
{
    [AddComponentMenu("JU TPS/AI/Patrol AI")]
    public class PatrolAI : JUCharacterArtificialInteligenceBrain
    {
        private CharacterBrain.JUCharacterBrain targetJuCharacter;

        private Transform currentTarget;

        private float distanceFromDestination;
        private Vector3 fieldViewPosition;

        private Vector3 smoothedTargetPosition;
        private Vector3 closestWalkablePosition;
        private Vector3 lastVisiblePosition;

        [Header("Follow Settings")]
        public string[] TargetTags = new string[] { "Player" };
        public FieldView FieldOfView = new FieldView(10, 60);
        public LayerMask SensorLayerMask;
        public float StartRunningAtDistance = 5;
        public float StopDistance = 3;

        [Header("Attack Settings")]
        public float AttackAtDistance = 15;
        public float AimUpOffset = 1;
        public float LookTargetSpeed = 5;
        public float AttackDuration = 1.5f;
        public float MinTimeToAttack = 1, MaxTimeToAttack = 2;

        private float currentTimeToAttack;
        private float currentMaxTimeToAttack;
        private float currentAttackDuration;
        private bool isAttacking;

        private bool isCurrentTargetAttackable;
        private bool currentTargetIsVisible;
        private bool isRunning;
        private float currentTimeToDisableFireMode;


        [Space(10)]
        public UnityEvent _OnFollowWaypoint;
        private bool FollowingWaypoint = false;
        public UnityEvent _OnFollowAIPath;
        private bool FollowingAIPath = false;
        public UnityEvent _OnSeeTarget;
        private bool SawATarget = false;
        public UnityEvent _OnStopSeeingTarget;
        private bool StoppedSeeingTarget;


        private void Start()
        {
            if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected) return;

            InvokeRepeating(nameof(CheckTargets), 0.5f, 0.5f);
        }

        protected virtual void Update()
        {
            if (character.IsDead) { this.enabled = false; return; }
            
            if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected) return;

            CheckEndEvents();

            isRunning = (distanceFromDestination > StartRunningAtDistance);

            if (currentTarget != null)
            {
                Debug.DrawLine(fieldViewPosition, smoothedTargetPosition, Color.green);
                HuntTheTargetState();

                if (distanceFromDestination < AttackAtDistance && currentTargetIsVisible && isCurrentTargetAttackable)
                {
                    EnterAttackModeState();
                }
                else
                {
                    ExitAttackModeState();
                }
            }

            else
            {
                Debug.DrawLine(fieldViewPosition, smoothedTargetPosition, Color.red);

                if (WaypointPath != null)
                {
                    if (character.FiringMode)
                    {
                        if (distanceFromDestination > StopDistance)
                        {
                            FollowAIPathState(closestWalkablePosition, isRunning);
                        }
                        else
                        {
                            if (distanceFromDestination < StopDistance / 2)
                            {
                                FollowAIPathState(transform.position - transform.forward * 5, true);
                            }
                            else
                            {
                                IdleState();
                            }
                        }

                        currentTimeToDisableFireMode += Time.deltaTime;
                        if (currentTimeToDisableFireMode > 2 && character.FiringMode)
                        {
                            ExitAttackModeState();
                        }

                        character.LookAtPosition = smoothedTargetPosition + Vector3.up * AimUpOffset;
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, WaypointPath.GetWaypointCenter()) < 15)
                        {
                            FollowWaypointPathState(isRunning);
                        }
                        else
                        {
                            FollowAIPathState(WaypointPath.WaypointPathPositions[0], isRunning);
                        }
                    }
                }
                else
                {
                    if (Destination == Vector3.zero)
                    {
                        IdleState();
                    }
                    else
                    {
                        if (distanceFromDestination < StopDistance / 2 || PathToDestination.Length == 0)
                        {
                            IdleState();
                        }
                        else
                        {
                            FollowAIPathState(closestWalkablePosition, isRunning);
                        }
                    }
                }
            }

            UpdateCurrentTarget();
        }

        public void CheckTargets()
        {
            if (photonView != null && !photonView.IsMine && PhotonNetwork.IsConnected) return;

            Vector3 fieldViewPosition = transform.position + transform.up * (AimUpOffset + 0.2f);
            Collider[] targets = FieldOfView.CheckViewCollider(fieldViewPosition, transform.forward, SensorLayerMask, viewerToIgnore: this.gameObject);
            
            if (targets.Length > 0)
            {
                currentTarget = SelectTargetOnList(targets, TargetTags);

                if (currentTarget == null)
                {
                    targetJuCharacter = null;
                }
                else
                {
                    if (targetJuCharacter != null)
                    {
                        isCurrentTargetAttackable = !targetJuCharacter.IsDead;
                        if (currentTarget != targetJuCharacter.transform)
                        {
                            if (currentTarget.TryGetComponent(out JUTPS.CharacterBrain.JUCharacterBrain character)) targetJuCharacter = character;
                        }
                    }
                    else
                    {
                        isCurrentTargetAttackable = false;
                        if (currentTarget.TryGetComponent(out JUTPS.CharacterBrain.JUCharacterBrain character)) targetJuCharacter = character;
                    }
                }
            }

            currentTargetIsVisible = (targetJuCharacter == null) ? FieldOfView.IsVisibleToThisFieldOfView(currentTarget, fieldViewPosition, transform.forward, SensorLayerMask, TagsToConsiderVisible: TargetTags) : FieldOfView.IsVisibleToThisFieldOfView(targetJuCharacter.HumanoidSpine, fieldViewPosition, transform.forward, SensorLayerMask, TagsToConsiderVisible: TargetTags);
        }

        public void UpdateCurrentTarget()
        {
            if (currentTarget != null && SawATarget == false)
            {
                _OnSeeTarget.Invoke();
                SawATarget = true;
                StoppedSeeingTarget = false;
            }
            if (currentTarget == null && StoppedSeeingTarget == false && SawATarget == true)
            {
                _OnStopSeeingTarget.Invoke();
                StoppedSeeingTarget = true;
                SawATarget = false;
            }

            if (CurrentWayPointToFollow > PathToDestination.Length - 1)
            {
                distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, Destination);
            }
            else
            {
                distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, PathToDestination[CurrentWayPointToFollow]);
            }

            smoothedTargetPosition = (currentTarget != null && targetJuCharacter == null) ? Vector3.Lerp(smoothedTargetPosition, currentTarget.position, LookTargetSpeed * Time.deltaTime) : smoothedTargetPosition;

            if (currentTarget == null) currentTargetIsVisible = false;

            if (targetJuCharacter != null) smoothedTargetPosition = Vector3.Lerp(smoothedTargetPosition, targetJuCharacter.HumanoidSpine.position - targetJuCharacter.transform.up * AimUpOffset, LookTargetSpeed * Time.deltaTime);

            closestWalkablePosition = JUPathFinder.GetClosestWalkablePoint(currentTarget != null ? currentTarget.position : closestWalkablePosition);

            if (currentTargetIsVisible) lastVisiblePosition = closestWalkablePosition;

            if (currentTarget != null)
            {
                if (distanceFromDestination > 2 * FieldOfView.Radious)
                {
                    currentTarget = null;
                }
            }
        }


        public void HuntTheTargetState()
        {
            if(StoppedSeeingTarget && currentTargetIsVisible == false)
            {
                FollowAIPathState(lastVisiblePosition, isRunning);
                return;
            }

            if (currentTargetIsVisible && SawATarget)
            {
                FollowAIPathState(closestWalkablePosition, isRunning);
            }
            else
            {
                FollowWaypointPathState(isRunning);
            }
        }

        public void FollowAIPathState(Vector3 Position, bool Run)
        {
            GoToPosition(Position, DistanceToFinishOnePoint, Run);
            JUPathFinder.VisualizePath(PathToDestination);
            OnEndPath = WaypointPath.OnEndPathAction.Stop;

            if (FollowingAIPath == false)
            {
                _OnFollowAIPath.Invoke();
                FollowingAIPath = true;
            }
            FollowingWaypoint = false;
        }

        public void FollowWaypointPathState(bool Run)
        {
            if (WaypointPath == null) { return; }

            FollowCurrentWaypoint(Run);
            OnEndPath = WaypointPath.OnEndPathAction.ReversePath;

            if (FollowingWaypoint == false)
            {
                _OnFollowWaypoint.Invoke();
                FollowingWaypoint = true;
            }
            FollowingAIPath = false;
        }


        public void EnterAttackModeState()
        {
            currentTimeToDisableFireMode = 0;
            if (isCurrentTargetAttackable == false)
            {
                isAttacking = false;
                currentMaxTimeToAttack = 0;
                currentAttackDuration = 0;
            }

            character.LookAtPosition = smoothedTargetPosition + Vector3.up * AimUpOffset;
            character.FiringMode = true;
            character.FiringModeIK = true;

            if (isAttacking == false)
            {
                if (currentMaxTimeToAttack == 0) { currentMaxTimeToAttack = Random.Range(MinTimeToAttack, MaxTimeToAttack); }

                currentTimeToAttack += Time.deltaTime;

                if (currentTimeToAttack >= currentMaxTimeToAttack) { currentTimeToAttack = 0; isAttacking = true; }

                character.DefaultUseOfAllItems(false, false);
            }

            if (isAttacking && currentAttackDuration < AttackDuration)
            {
                if (Vector3.Dot(smoothedTargetPosition - transform.position, currentTarget.position - transform.position) > 0.8f)
                {
                    character.DefaultUseOfAllItems(true, true, false, false, false, true);
                }
                currentAttackDuration += Time.deltaTime;

                if (currentAttackDuration >= AttackDuration)
                {
                    isAttacking = false;
                    currentMaxTimeToAttack = 0;
                    currentAttackDuration = 0;
                }
            }

        }

        public void ExitAttackModeState()
        {
            character.FiringMode = false;
            character.FiringModeIK = false;

            isAttacking = false;
            currentMaxTimeToAttack = 0;
            currentAttackDuration = 0;
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            FieldView.DrawFieldOfView(transform.position + transform.up * AimUpOffset, transform.forward, FieldOfView);
        }
#endif
    }
}