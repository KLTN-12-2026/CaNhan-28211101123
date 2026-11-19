using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections;
using Photon.Realtime;
using Unity.Cinemachine;
using JUTPS;

public class C4Controller : MonoBehaviourPun
{
    public string attackingTeam = "TeamA";
    public string defendingTeam = "TeamB";
    public float activateTime = 4f;
    public float defuseTime = 7f;
    public float interactRadius = 3f;
    public float idleSettleDelay = 0.3f;

    [Header("Audio Settings")]
    public AudioSource beepSource;
    public AudioClip explosionSound;
    public AudioClip plantSound;
    public AudioClip defuseSound;
    public float maxHearDistance = 35f;

    [Header("Explosion AoE Settings")]
    public float explosionRadius = 100f;
    public int maxExplosionDamage = 200;
    public GameObject explosionEffectPrefab;

    private bool isActivated = false;
    private bool isDefused = false;
    private bool hasExploded = false; 
    
    private float currentProgress = 0f;
    private float countdownTimer = 0f;
    private double plantTime = 0;
    
    private GameObject localPlayer;
    
    private JUCharacterController localShooter;
    private bool isLocalPlayerInteracting = false;
    private float idleHoldTimer = 0f;
    
    private bool wasMatchStarted = false;
    private Coroutine statusAnimationCoroutine;
    private Coroutine beepCoroutine;

    public static bool isAnyBombActivated = false;
    private bool isDefuseHalfway = false;
    private int interactingPlayerId = -1;
    private int planterActorNumber = -1;

    private void Start()
    {
        if (beepSource != null)
        {
            beepSource.loop = false;
            beepSource.playOnAwake = false;
            beepSource.spatialBlend = 1f;
            beepSource.rolloffMode = AudioRolloffMode.Linear;
            beepSource.minDistance = 3f;
            beepSource.maxDistance = maxHearDistance;
        }
        ResetBomb();
    }

    private void Update()
    {
        if (GameManager.isMatchStarted && !wasMatchStarted)
        {
            ResetBomb();
            wasMatchStarted = true;
        }
        else if (!GameManager.isMatchStarted && wasMatchStarted)
        {
            ResetBomb(); 
            wasMatchStarted = false;
        }

        if (!GameManager.isMatchStarted) return;

        if (interactingPlayerId != -1)
        {
            Player p = PhotonNetwork.CurrentRoom.GetPlayer(interactingPlayerId);
            if (p == null && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("RPC_SetInteractingPlayer", RpcTarget.All, -1);
            }
        }

        if (isActivated && !isDefused && !hasExploded && GameManager.Instance != null)
        {
            countdownTimer = Mathf.Max(0f, GameManager.Instance.bombExplodeTime - (float)(PhotonNetwork.Time - plantTime));

            if (countdownTimer <= 0f && PhotonNetwork.IsMasterClient)
            {
                hasExploded = true; 
                photonView.RPC("RPC_Explode", RpcTarget.All);
            }
        }

        CheckLocalInteraction();
    }

    private void EnsureLocalPlayerFound()
    {
        if (localPlayer == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                if (p.GetComponent<PhotonView>() != null && p.GetComponent<PhotonView>().IsMine)
                {
                    localPlayer = p;
                    localShooter = p.GetComponent<JUCharacterController>();
                    break;
                }
            }
        }
    }

    private void CheckLocalInteraction()
    {
        EnsureLocalPlayerFound();
        if (localPlayer == null) return;

        bool isDead = false;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsDead"))
        {
            isDead = (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsDead"];
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team") || isDead)
        {
            if (isLocalPlayerInteracting)
            {
                isLocalPlayerInteracting = false;
                if (localShooter != null) localShooter.DisableAllMove = false;
            }
            idleHoldTimer = 0f;
            CancelInteraction();
            return;
        }

        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        string myTeam = PhotonNetwork.LocalPlayer.CustomProperties["Team"] as string;

        bool isTryingToInteract = false;

        if (distance <= interactRadius)
        {
            bool canActivate = !isActivated && myTeam == attackingTeam && !isAnyBombActivated && !hasExploded;
            bool canDefuse = isActivated && !isDefused && myTeam == defendingTeam && !hasExploded;

            if (canActivate || canDefuse)
            {
                if (Input.GetKey(KeyCode.F))
                {
                    if (isLocalPlayerInteracting)
                    {
                        isTryingToInteract = true;
                    }
                    else
                    {
                        bool isPlayerMoving = localShooter != null && localShooter.IsMoving;

                        if (isPlayerMoving)
                        {
                            idleHoldTimer = 0f;
                        }
                        else
                        {
                            idleHoldTimer += Time.deltaTime;
                        }

                        isTryingToInteract = idleHoldTimer >= idleSettleDelay;
                    }

                    if (isTryingToInteract)
                    {
                        if (interactingPlayerId != -1 && interactingPlayerId != PhotonNetwork.LocalPlayer.ActorNumber)
                        {
                            isTryingToInteract = false;
                        }
                        else
                        {
                            if (!isLocalPlayerInteracting)
                            {
                                isLocalPlayerInteracting = true;
                                if (localShooter != null) localShooter.DisableAllMove = true;
                            }

                            if (interactingPlayerId == -1)
                            {
                                interactingPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
                                photonView.RPC("RPC_SetInteractingPlayer", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                                currentProgress = (canDefuse && isDefuseHalfway) ? (defuseTime / 2f) : 0f;

                                photonView.RPC("RPC_PlayInteractSound", RpcTarget.All, canActivate);
                            }

                            MatchUIManager.Instance?.ShowBombProgress();
                            MatchUIManager.Instance?.SetCheckpointMarker(canDefuse, isDefuseHalfway);

                            string baseText = canActivate ? "PLANTING" : "DEFUSING";
                            if (statusAnimationCoroutine == null)
                            {
                                StartStatusAnimation(baseText);
                            }

                            float requiredTime = canActivate ? activateTime : defuseTime;
                            currentProgress += Time.deltaTime;
                            MatchUIManager.Instance?.UpdateBombProgress(currentProgress / requiredTime);

                            if (canDefuse && !isDefuseHalfway && currentProgress >= (defuseTime / 2f))
                            {
                                MatchUIManager.Instance?.SetCheckpointMarker(true, true);
                                photonView.RPC("RPC_SetDefuseHalfway", RpcTarget.All, true);
                            }

                            if (currentProgress >= requiredTime)
                            {
                                isTryingToInteract = false;

                                int myActorNum = PhotonNetwork.LocalPlayer.ActorNumber;

                                if (isLocalPlayerInteracting)
                                {
                                    isLocalPlayerInteracting = false;
                                    if (localShooter != null) localShooter.DisableAllMove = false;
                                }

                                CancelInteraction();

                                if (canActivate) photonView.RPC("RPC_SetBombState", RpcTarget.All, true, false, myActorNum);
                                else if (canDefuse) photonView.RPC("RPC_SetBombState", RpcTarget.All, true, true, myActorNum);
                            }
                        }
                    }
                }
                else
                {
                    idleHoldTimer = 0f;
                }
            }
            else
            {
                idleHoldTimer = 0f;
            }
        }
        else
        {
            idleHoldTimer = 0f;
        }

        if (!isTryingToInteract)
        {
            if (isLocalPlayerInteracting)
            {
                isLocalPlayerInteracting = false;
                if (localShooter != null) localShooter.DisableAllMove = false;
            }
            CancelInteraction();
        }
    }

    private void CancelInteraction()
    {
        if (interactingPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            photonView.RPC("RPC_SetInteractingPlayer", RpcTarget.All, -1);
        }
        ResetProgress();
    }

    private void ResetProgress()
    {
        if (currentProgress > 0f || statusAnimationCoroutine != null)
        {
            currentProgress = 0f;
            MatchUIManager.Instance?.HideBombProgress();
            MatchUIManager.Instance?.UpdateBombStatus("");
            
            if (statusAnimationCoroutine != null)
            {
                StopCoroutine(statusAnimationCoroutine);
                statusAnimationCoroutine = null;
            }
        }
    }

    #region Status Text Anim
    private void StartStatusAnimation(string baseText)
    {
        if (statusAnimationCoroutine != null) StopCoroutine(statusAnimationCoroutine);
        statusAnimationCoroutine = StartCoroutine(AnimateStatusText(baseText));
    }

    private IEnumerator AnimateStatusText(string baseText)
    {
        int dots = 0;
        while (true)
        {
            string dotString = new string('.', dots);
            MatchUIManager.Instance?.UpdateBombStatus(baseText + dotString);
            dots = (dots + 1) % 4;
            yield return new WaitForSeconds(0.5f);
        }
    }
    #endregion

    private void ResetBomb()
    {
        isActivated = false;
        isDefused = false;
        isAnyBombActivated = false;
        hasExploded = false; 
        
        isDefuseHalfway = false;
        interactingPlayerId = -1;
        planterActorNumber = -1;

        countdownTimer = GameManager.Instance != null ? GameManager.Instance.bombExplodeTime : 45f;
        plantTime = 0;
        currentProgress = 0f;
        idleHoldTimer = 0f;

        if (beepCoroutine != null)
        {
            StopCoroutine(beepCoroutine);
            beepCoroutine = null;
        }

        MatchUIManager.Instance?.HideBombProgress();
        if (beepSource != null) beepSource.Stop();
        
        gameObject.SetActive(true); 
    }

    [PunRPC]
    private void RPC_SetInteractingPlayer(int actorNumber) { interactingPlayerId = actorNumber; }

    [PunRPC]
    private void RPC_SetDefuseHalfway(bool state) { isDefuseHalfway = state; }

    [PunRPC]
    private void RPC_PlayInteractSound(bool isPlanting)
    {
        if (beepSource != null)
        {
            AudioClip clipToPlay = isPlanting ? plantSound : defuseSound;
            
            if (clipToPlay != null)
            {
                beepSource.PlayOneShot(clipToPlay);
            }
        }
    }

    [PunRPC]
    private void RPC_SetBombState(bool activated, bool defused, int actorId)
    {
        isActivated = activated;
        isDefused = defused;

        if (activated && !defused)
        {
            planterActorNumber = actorId;
            isAnyBombActivated = true;
            plantTime = PhotonNetwork.Time;
            if (GameManager.Instance != null) GameManager.Instance.Local_BombPlanted();
        }

        if (isActivated && !isDefused)
        {
            if (beepCoroutine != null) StopCoroutine(beepCoroutine);
            beepCoroutine = StartCoroutine(BombBeepRoutine());
        }
        else if (isDefused)
        {
            if (beepCoroutine != null) StopCoroutine(beepCoroutine);
            if (beepSource != null) beepSource.Stop();
            if (GameManager.Instance != null) GameManager.Instance.Local_BombDefused();
        }
    }

    private IEnumerator BombBeepRoutine()
    {
        while (isActivated && !isDefused && !hasExploded)
        {
            if (beepSource != null)
            {
                if (beepSource.clip != null)
                {
                    beepSource.PlayOneShot(beepSource.clip);
                }
                else
                {
                    beepSource.Play(); 
                }
            }

            float delay = 1.0f;
            
            if (countdownTimer <= 5f)
            {
                delay = 0.15f;
            }
            else if (countdownTimer <= 15f)
            {
                delay = 0.35f;
            }

            yield return new WaitForSeconds(delay);
        }
    }

    [PunRPC]
    private void RPC_Explode()
    {
        hasExploded = true;

        if (beepCoroutine != null) StopCoroutine(beepCoroutine);
        
        if (beepSource != null) 
        {
            beepSource.Stop();
            if (explosionSound != null)
            {
                beepSource.PlayOneShot(explosionSound);
            }
        }

        if (explosionEffectPrefab != null)
        {
            GameObject fxInstance = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fxInstance, 5f);
        }

        CinemachineImpulseSource impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        EnsureLocalPlayerFound();
        bool isDead = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsDead") && (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsDead"];

        if (localPlayer != null && !isDead)
        {
            float distanceToBomb = Vector3.Distance(transform.position, localPlayer.transform.position);

            if (distanceToBomb <= explosionRadius)
            {
                float damageMultiplier = 1f - (distanceToBomb / explosionRadius);
                int calculatedDamage = Mathf.RoundToInt(maxExplosionDamage * damageMultiplier);

                JUHealth health = localPlayer.GetComponent<JUHealth>();
                if (health != null)
                {
                    string killerName = "C4";
                    if (planterActorNumber != -1)
                    {
                        Player planter = PhotonNetwork.CurrentRoom.GetPlayer(planterActorNumber);
                        if (planter != null) killerName = planter.NickName;
                    }

                    health.DoDamage(calculatedDamage, killerName, "Phao", transform.position);
                }
            }
        }
    }
}