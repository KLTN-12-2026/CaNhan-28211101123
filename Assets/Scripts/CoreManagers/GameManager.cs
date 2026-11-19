using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using JUTPS;
using JUTPS.WeaponSystem;
using JUTPS.InventorySystem;
using JUTPS.FX;
using JUTPS.InventorySystem.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Player Prefab & Spawns")]
    [SerializeField] GameObject teamAPlayerPrefab; 
    [SerializeField] GameObject teamBPlayerPrefab; 
    [SerializeField] Transform[] teamASpawnPoints; 
    [SerializeField] Transform[] teamBSpawnPoints; 

    [Header("Match Settings")]
    public int roundsToWin = 2;
    public float roundDuration = 120f;
    public float delayBetweenRounds = 5f;
    public float prepDuration = 30f;

    [Header("C4 Bomb Settings")]
    public float bombExplodeTime = 45f;
    private bool isBombPlanted = false;
    private double bombPlantTime = 0;
    private bool isBombDefused = false;
    private float frozenBombTime = 0f;

    [Header("Shop Integration")]
    public ShopManager shopManager;

    private int scoreTeamA = 0;
    private int scoreTeamB = 0;
    private bool isPlaying = false;
    private string myTeam = "";
    private GameObject myPlayerInstance;
    private int lastTickSecond = -1;
    
    public int currentRound = 1;
    public static bool isMatchStarted = false;
    public int currentLocalArmor = 0; 
    public int roundStartArmor = 0; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        isMatchStarted = false;
        if (MatchUIManager.Instance != null) MatchUIManager.Instance.StartLoadingTransition();
    }

    public void UpdateArmorUI(int currentArmor) { currentLocalArmor = currentArmor; }
    public bool CanOpenShop() { return !string.IsNullOrEmpty(myTeam) && !isMatchStarted && isPlaying; }

    public void Local_BombPlanted()
    {
        isBombPlanted = true;
        bombPlantTime = PhotonNetwork.Time;
        MatchUIManager.Instance?.ShowRoundAnnouncement("BOMB PLANTED");
        StartCoroutine(HideBombPlantedText());
    }

    private IEnumerator HideBombPlantedText()
    {
        yield return new WaitForSeconds(2.5f);
        if (isPlaying && !isBombDefused) MatchUIManager.Instance?.HideRoundAnnouncement();
    }

    public void Local_BombDefused()
    {
        if (isBombDefused) return; 
        isBombDefused = true;
        frozenBombTime = Mathf.Max(0f, bombExplodeTime - (float)(PhotonNetwork.Time - bombPlantTime));
        MatchUIManager.Instance?.ShowRoundAnnouncement("BOMB DEFUSED");
        StartCoroutine(DelayRoundEndAfterDefuse());
    }

    private IEnumerator DelayRoundEndAfterDefuse()
    {
        yield return new WaitForSeconds(2.0f);
        if (PhotonNetwork.IsMasterClient && isPlaying) photonView.RPC("RPC_EndRound", RpcTarget.All, "TeamB");
    }

    private void Update()
    {
        if (isPlaying && myPlayerInstance != null)
        {
            JUTPS.JUCharacterController myController = myPlayerInstance.GetComponent<JUTPS.JUCharacterController>();
            if (myController != null && myController.DisableAllMove) myController.DisableAllMove = false;
        }

        if (!string.IsNullOrEmpty(myTeam) && !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
        {
            MatchUIManager.Instance?.ShowPrepPhaseText("Waiting for players");
            MatchUIManager.Instance?.UpdateTimer("00:00", Color.white);
            return;
        }

        if (isPlaying && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
        {
            double startTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["StartTime"];
            float timer = (float)(PhotonNetwork.Time - startTime);

            if (timer < prepDuration)
            {
                isMatchStarted = false;
                float prepTimeRemaining = prepDuration - timer;

                int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(prepTimeRemaining));
                int currentSecond = totalSeconds;

                MatchUIManager.Instance?.HideRoundAnnouncement();
                MatchUIManager.Instance?.ShowPrepPhaseText("PREPARE ROUND");

                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                MatchUIManager.Instance?.UpdateTimer(string.Format("{0:00}:{1:00}", minutes, seconds), Color.red);

                if (currentSecond <= 5 && currentSecond != lastTickSecond)
                {
                    lastTickSecond = currentSecond;
                    if (currentSecond > 0 && MatchAudioManager.Instance != null) MatchAudioManager.Instance.PlayTickSound();
                }
            }
            else
            {
                if (!isMatchStarted)
                {
                    isMatchStarted = true;
                    MatchUIManager.Instance?.HidePrepPhaseText();
                    if (MatchAudioManager.Instance != null) MatchAudioManager.Instance.PlayStartSound();
                    if (shopManager != null && shopManager.isShopOpen) shopManager.CloseShop();
                }

                if (isBombPlanted)
                {
                    if (isBombDefused)
                    {
                        int seconds = Mathf.FloorToInt(frozenBombTime);
                        int ms = Mathf.FloorToInt((frozenBombTime - seconds) * 100f);
                        MatchUIManager.Instance?.UpdateTimer(string.Format("{0:00}.{1:00}", seconds, ms), Color.cyan);
                    }
                    else
                    {
                        float timePassed = (float)(PhotonNetwork.Time - bombPlantTime);
                        float timeLeft = Mathf.Max(0f, bombExplodeTime - timePassed);
                        
                        int seconds = Mathf.FloorToInt(timeLeft);
                        int ms = Mathf.FloorToInt((timeLeft - seconds) * 100f);
                        MatchUIManager.Instance?.UpdateTimer(string.Format("{0:00}.{1:00}", seconds, ms), Color.red);

                        if (timeLeft <= 0 && PhotonNetwork.IsMasterClient && isPlaying) photonView.RPC("RPC_EndRound", RpcTarget.All, "TeamA");
                    }
                }
                else
                {
                    float timePassedInRound = timer - prepDuration;
                    float timeRemaining = roundDuration - timePassedInRound;

                    int totalSecs = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
                    int minutes = totalSecs / 60;
                    int seconds = totalSecs % 60;
                    Color timerColor = (totalSecs <= 30) ? Color.red : Color.green;
                    MatchUIManager.Instance?.UpdateTimer(string.Format("{0:00}:{1:00}", minutes, seconds), timerColor);

                    if (totalSecs <= 0 && PhotonNetwork.IsMasterClient)
                    {
                        if (isPlaying) photonView.RPC("RPC_EndRound", RpcTarget.All, "TeamB");
                    }
                }
            }
        }
    }

    public void OnJoinTeamA()
    {
        if (GetTeamPlayerCount("TeamA") >= 2) { MatchUIManager.Instance?.ShowSystemMessage("Team Tấn Công đã đầy!"); return; }
        JoinTeam("TeamA");
    }

    public void OnJoinTeamB()
    {
        if (GetTeamPlayerCount("TeamB") >= 2) { MatchUIManager.Instance?.ShowSystemMessage("Team Phòng Thủ đã đầy!"); return; }
        JoinTeam("TeamB");
    }

    private int GetTeamPlayerCount(string teamName)
    {
        int count = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.ContainsKey("Team") && (string)p.CustomProperties["Team"] == teamName) count++;
        return count;
    }

    private void JoinTeam(string teamName)
    {
        myTeam = teamName;
        if (MatchUIManager.Instance != null) MatchUIManager.Instance.ShowLoadingScreen();
        
        currentLocalArmor = 0;
        Hashtable hash = new Hashtable();
        hash.Add("SavedArmor", 0);
        hash.Add("SavedPrimary", "");
        hash.Add("SavedSecondary", "");
        hash.Add("Team", myTeam);
        hash.Add("IsDead", false);
        hash.Add("RoundReady", currentRound);

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Cash")) hash.Add("Cash", 800);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        StartCoroutine(SpawnMyPlayerDelayed(0.5f));
    }

    private IEnumerator SpawnMyPlayerDelayed(float delayTime)
    {
        yield return new WaitForSeconds(delayTime); 
        SpawnMyPlayer(); 
        yield return null; 
        if (MatchUIManager.Instance != null) MatchUIManager.Instance.HideLoadingScreen();
    }

    private void SpawnMyPlayer()
    {
        Transform[] selectedSpawns = (myTeam == "TeamA") ? teamASpawnPoints : teamBSpawnPoints;
        GameObject prefabToSpawn = (myTeam == "TeamA") ? teamAPlayerPrefab : teamBPlayerPrefab;

        if (selectedSpawns == null || selectedSpawns.Length == 0)
        {
            Debug.LogError($"GameManager: no spawn points assigned for {myTeam} in the Inspector - cannot spawn player.");
            return;
        }
        if (prefabToSpawn == null)
        {
            Debug.LogError($"GameManager: no player prefab assigned for {myTeam} in the Inspector - cannot spawn player.");
            return;
        }

        if (myPlayerInstance != null) PhotonNetwork.Destroy(myPlayerInstance);

        List<Player> teamMembers = new List<Player>();
        foreach (Player p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.ContainsKey("Team") && (string)p.CustomProperties["Team"] == myTeam) teamMembers.Add(p);
        
        if (!teamMembers.Contains(PhotonNetwork.LocalPlayer)) teamMembers.Add(PhotonNetwork.LocalPlayer);
        teamMembers.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
        int myIndex = teamMembers.IndexOf(PhotonNetwork.LocalPlayer);
        if (myIndex < 0) myIndex = 0;

        Transform spawnTransform = selectedSpawns[myIndex % selectedSpawns.Length];
        if (spawnTransform == null)
        {
            Debug.LogError($"GameManager: spawn point at index {myIndex % selectedSpawns.Length} for {myTeam} is empty/missing.");
            return;
        }

        Vector3 finalSpawnPos = spawnTransform.position;
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 1.2f;
        finalSpawnPos += new Vector3(randomOffset.x, 0, randomOffset.y);

        string prefabToSpawnName = prefabToSpawn.name;
        myPlayerInstance = PhotonNetwork.Instantiate(prefabToSpawnName, finalSpawnPos, spawnTransform.rotation);
        
        StartCoroutine(FixPlayerOrientation(myPlayerInstance, spawnTransform));

        JUTPS.JUCharacterController freshController = myPlayerInstance.GetComponent<JUTPS.JUCharacterController>();
        if (freshController != null)
        {
            freshController.DisableAllMove = true;

            if (isPlaying) freshController.DisableAllMove = false;
        }

        roundStartArmor = 0;
        string savedPrimary = "";
        string savedSecondary = "";

        if (myPlayerInstance != null)
        {
            JUInventory inventory = myPlayerInstance.GetComponent<JUInventory>();
            if (inventory != null)
            {
                string defaultPistol = (myTeam == "TeamA") ? "Lunar2" : "Lunar";

                foreach (var item in inventory.HoldableItensRightHand)
                {
                    if (item.ItemName != "Hands") 
                        item.Unlocked = false;
                    else 
                        item.Unlocked = true;
                }

                int switchTargetID = -1;

                if (currentRound == 1)
                {
                    roundStartArmor = 0;
                    
                    foreach (var item in inventory.HoldableItensRightHand)
                    {
                        if (item.ItemName == defaultPistol)
                        {
                            item.Unlocked = true;
                            switchTargetID = item.ItemSwitchID;
                            break;
                        }
                    }
                    
                    Hashtable hash = new Hashtable();
                    hash.Add("Cash", 800);
                    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                }
                else if (currentRound > 1)
                {
                    if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SavedArmor"))
                        roundStartArmor = (int)PhotonNetwork.LocalPlayer.CustomProperties["SavedArmor"];
                    if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SavedPrimary"))
                        savedPrimary = (string)PhotonNetwork.LocalPlayer.CustomProperties["SavedPrimary"];
                    if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SavedSecondary"))
                        savedSecondary = (string)PhotonNetwork.LocalPlayer.CustomProperties["SavedSecondary"];

                    if (!string.IsNullOrEmpty(savedSecondary))
                    {
                        foreach (var item in inventory.HoldableItensRightHand)
                        {
                            if (item.ItemName == savedSecondary)
                            {
                                item.Unlocked = true;
                                switchTargetID = item.ItemSwitchID;
                                break;
                            }
                        }
                        
                        foreach (var item in inventory.HoldableItensLeftHand)
                        {
                            if (item.ItemName == savedSecondary)
                            {
                                item.Unlocked = true;
                                break;
                            }
                        }
                    }
                    else 
                    {
                        foreach (var item in inventory.HoldableItensRightHand)
                        {
                            if (item.ItemName == defaultPistol)
                            {
                                item.Unlocked = true;
                                switchTargetID = item.ItemSwitchID;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(savedPrimary))
                    {
                        foreach (var item in inventory.HoldableItensRightHand)
                        {
                            if (item.ItemName == savedPrimary)
                            {
                                item.Unlocked = true;
                                switchTargetID = item.ItemSwitchID;
                                break;
                            }
                        }
                    }
                }

                StartCoroutine(EquipWeaponDelayed(myPlayerInstance, switchTargetID));

                var uiInfo = FindObjectOfType<UIItemInformation>();
                if (uiInfo != null) uiInfo.ResetPlayerReference();
            }
        }
        
        if (myPlayerInstance != null)
        {
            JUHealth health = myPlayerInstance.GetComponent<JUHealth>();
            if (health != null) health.UpdateArmorThroughNetwork(roundStartArmor);
        }

        UpdateArmorUI(roundStartArmor);
        ActivateHitMarkerEffect();
        MatchUIManager.Instance?.ShowJUTPSUI();

        StartCoroutine(ForceUpdateWeaponUI(myPlayerInstance));
    }

    private IEnumerator EquipWeaponDelayed(GameObject playerObj, int targetID)
    {
        yield return new WaitForSeconds(0.4f);
        
        if (playerObj != null && targetID != -1)
        {
            var inventory = playerObj.GetComponent<JUInventory>();
            var brain = playerObj.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();

            if (inventory != null)
            {
                foreach (var item in inventory.HoldableItensRightHand)
                {
                    if (item.ItemSwitchID == targetID) 
                    {
                        item.Unlocked = true;
                        break;
                    }
                }

                if (brain != null)
                {
                    brain.SwitchToItem(targetID, true);
                }
                else
                {
                    inventory.SwitchToItem(targetID, true);
                }
            }
        }
    }

    private IEnumerator ForceUpdateWeaponUI(GameObject playerObj)
    {
        yield return new WaitForSeconds(0.6f); 
        
        if (playerObj != null)
        {
            var inventory = playerObj.GetComponent<JUInventory>();
            if (inventory != null)
            {
                inventory.RefreshInBodyItemVisibility();
            }
        }

        if (MatchUIManager.Instance != null)
        {
            MatchUIManager.Instance.HideJUTPSUI();
            MatchUIManager.Instance.ShowJUTPSUI();
        }
    }

    private IEnumerator FixPlayerOrientation(GameObject playerObj, Transform spawnPoint)
    {
        yield return new WaitForSeconds(0.1f);

        if (playerObj != null)
        {
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.rotation = spawnPoint.rotation;
            }

            playerObj.transform.rotation = spawnPoint.rotation;

            JUTPS.JUCharacterController tpsController = playerObj.GetComponent<JUTPS.JUCharacterController>();
            if (tpsController != null && tpsController.MyPivotCamera != null)
            {
                Transform camPivot = tpsController.MyPivotCamera.transform;

                Vector3 targetEuler = spawnPoint.eulerAngles;
                camPivot.rotation = Quaternion.Euler(0f, targetEuler.y, 0f);

                Behaviour camBehaviour = tpsController.MyPivotCamera as Behaviour;
                if (camBehaviour != null)
                {
                    camBehaviour.enabled = false;
                    camBehaviour.enabled = true;
                }
            }
        }
    }

    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient || isPlaying) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

        int readyPlayers = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.ContainsKey("RoundReady") && (int)p.CustomProperties["RoundReady"] == currentRound) readyPlayers++;

        if (readyPlayers == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            SetRoundStartTime();
            photonView.RPC("RPC_StartRound", RpcTarget.All);
        }
    }

    private void SetRoundStartTime()
    {
        Hashtable roomProps = new Hashtable();
        roomProps.Add("StartTime", PhotonNetwork.Time);
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("RoundReady")) CheckAllPlayersReady();
        if (!isPlaying || !PhotonNetwork.IsMasterClient) return;

        if (changedProps.ContainsKey("IsDead"))
        {
            bool isDead = (bool)changedProps["IsDead"];
            if (isDead == true) CheckRoundWinCondition();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CheckAllPlayersReady();
        if (!PhotonNetwork.IsMasterClient) return;

        if (CheckForTeamForfeit()) return;
        if (isPlaying) CheckRoundWinCondition();
    }

    private bool CheckForTeamForfeit()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 0) return false;

        int teamA_Total = 0, teamB_Total = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey("Team")) continue;
            string team = (string)p.CustomProperties["Team"];
            if (team == "TeamA") teamA_Total++;
            else if (team == "TeamB") teamB_Total++;
        }

        string winnerTeam = null;
        if (teamA_Total == 0 && teamB_Total > 0) winnerTeam = "TeamB";
        else if (teamB_Total == 0 && teamA_Total > 0) winnerTeam = "TeamA";

        if (winnerTeam == null) return false;

        isPlaying = false;
        photonView.RPC(nameof(RPC_ForfeitWin), RpcTarget.All, winnerTeam);
        return true;
    }

    [PunRPC]
    private void RPC_ForfeitWin(string winnerTeam)
    {
        isPlaying = false;
        if (winnerTeam == "TeamA") scoreTeamA = roundsToWin;
        else scoreTeamB = roundsToWin;

        MatchUIManager.Instance?.UpdateScores(scoreTeamA, scoreTeamB);
        MatchUIManager.Instance?.ShowRoundAnnouncement("OPPONENT FORFEITED");

        if (PhotonNetwork.IsMasterClient) ShowGameOver(winnerTeam);
    }

    private void CheckRoundWinCondition()
    {
        if (!isPlaying) return; 

        int teamA_Alive = 0, teamA_Total = 0;
        int teamB_Alive = 0, teamB_Total = 0;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Team"))
            {
                string team = (string)p.CustomProperties["Team"];
                bool isDead = p.CustomProperties.ContainsKey("IsDead") && (bool)p.CustomProperties["IsDead"];

                if (team == "TeamA") { teamA_Total++; if (!isDead) teamA_Alive++; }
                else if (team == "TeamB") { teamB_Total++; if (!isDead) teamB_Alive++; }
            }
        }

        if (teamA_Total > 0 && teamB_Total > 0)
        {
            if (teamA_Alive == 0) { if (!isBombPlanted) photonView.RPC("RPC_EndRound", RpcTarget.All, "TeamB"); }
            else if (teamB_Alive == 0) photonView.RPC("RPC_EndRound", RpcTarget.All, "TeamA");
        }
    }

    [PunRPC]
    private void RPC_StartRound()
    {
        isPlaying = true;
        MatchUIManager.Instance?.HideRoundAnnouncement();

        if (myPlayerInstance != null)
        {
            JUTPS.JUCharacterController controller = myPlayerInstance.GetComponent<JUTPS.JUCharacterController>();
            if (controller != null) controller.DisableAllMove = false;
        }
    }

    [PunRPC]
    private void RPC_EndRound(string winnerTeam)
    {
        if (!isPlaying) return; 
        
        isPlaying = false;
        currentRound++;
        isBombPlanted = false;
        isBombDefused = false;

        if (myPlayerInstance != null)
        {
            JUTPS.JUCharacterController endRoundController = myPlayerInstance.GetComponent<JUTPS.JUCharacterController>();
            if (endRoundController != null) endRoundController.DisableAllMove = true;
        }

        if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
        if (shopManager != null) shopManager.CloseShop();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable roomProps = new Hashtable();
            roomProps.Add("StartTime", null);
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        if (!string.IsNullOrEmpty(myTeam) && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Cash"))
        {
            int currentCash = (int)PhotonNetwork.LocalPlayer.CustomProperties["Cash"];
            int bonus = (myTeam == winnerTeam) ? 3000 : 1900; 

            currentCash += bonus;
            if (currentCash > 9000) currentCash = 9000;

            bool isDead = false;
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("IsDead")) isDead = (bool)PhotonNetwork.LocalPlayer.CustomProperties["IsDead"];

            string myPrimary = "";
            string mySecondary = "";

            if (!isDead && myPlayerInstance != null)
            {
                JUHealth myHealth = myPlayerInstance.GetComponent<JUHealth>();
                if (myHealth != null) currentLocalArmor = Mathf.RoundToInt(myHealth.Armor);

                JUInventory inventory = myPlayerInstance.GetComponent<JUInventory>();
                if (inventory != null)
                {
                    foreach (var item in inventory.HoldableItensRightHand)
                    {
                        if (item.Unlocked)
                        {
                            if (item.ItemName == "Hands") continue;
                            
                            if (item.ItemName == "Lunar" || item.ItemName == "Lunar2" || item.ItemName == "Mustang") 
                                mySecondary = item.ItemName;
                            else 
                                myPrimary = item.ItemName;
                        }
                    }
                }
            }
            else currentLocalArmor = 0;

            Hashtable hash = new Hashtable();
            hash.Add("Cash", currentCash);
            hash.Add("SavedArmor", currentLocalArmor); 
            hash.Add("SavedPrimary", myPrimary);
            hash.Add("SavedSecondary", mySecondary);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        if (winnerTeam == "TeamA") scoreTeamA++;
        else if (winnerTeam == "TeamB") scoreTeamB++;

        MatchUIManager.Instance?.UpdateScores(scoreTeamA, scoreTeamB);
        MatchUIManager.Instance?.ShowRoundAnnouncement(winnerTeam + " VICTORY");

        if (scoreTeamA >= roundsToWin) { if (PhotonNetwork.IsMasterClient) ShowGameOver("TeamA"); }
        else if (scoreTeamB >= roundsToWin) { if (PhotonNetwork.IsMasterClient) ShowGameOver("TeamB"); }
        else StartCoroutine(NextRoundRoutine());
    }

    [PunRPC]
    private void RPC_SetMatchResult(string winningTeam, int scoreA, int scoreB,
        string mvpName, int mvpKills, int mvpDeaths, int mvpAssists, int mvpAvatarID)
    {
        MatchResultData.HasResult = true;
        MatchResultData.ScoreTeamA = scoreA;
        MatchResultData.ScoreTeamB = scoreB;
        MatchResultData.MvpName = mvpName;
        MatchResultData.MvpKills = mvpKills;
        MatchResultData.MvpDeaths = mvpDeaths;
        MatchResultData.MvpAssists = mvpAssists;
        MatchResultData.MvpAvatarID = mvpAvatarID;
    }

    private IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(delayBetweenRounds);
        if (!string.IsNullOrEmpty(myTeam)) 
        {
            Hashtable hash = new Hashtable();
            hash.Add("IsDead", false);
            hash.Add("RoundReady", currentRound);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

            MatchUIManager.Instance?.HideRoundAnnouncement();
            SpawnMyPlayer();
            
            yield return new WaitForEndOfFrame();
            ActivateHitMarkerEffect();
        }
    }

    private Player GetMVPPlayer()
    {
        Player mvp = null;
        float maxScore = -1f;
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int kills = p.CustomProperties.ContainsKey("Kills") ? (int)p.CustomProperties["Kills"] : 0;
            int assists = p.CustomProperties.ContainsKey("Assists") ? (int)p.CustomProperties["Assists"] : 0;
            
            float playerScore = (kills * 2f) + (assists * 1f); 
            
            float damage = 0f;
            if (p.CustomProperties.ContainsKey("Damage")) { try { damage = System.Convert.ToSingle(p.CustomProperties["Damage"]); } catch { } }

            playerScore += (damage * 0.001f); 

            if (playerScore > maxScore) 
            { 
                maxScore = playerScore; 
                mvp = p; 
            }
        }
        return mvp;
    }

    private void ShowGameOver(string winningTeam) { StartCoroutine(GameOverRoutine(winningTeam)); }
    private IEnumerator GameOverRoutine(string winningTeam)
    {
        yield return new WaitForSeconds(4.0f);
        Player mvp = GetMVPPlayer();

        string mvpName = mvp != null ? mvp.NickName : "Unknown";
        int mvpKills = mvp != null && mvp.CustomProperties.ContainsKey("Kills") ? (int)mvp.CustomProperties["Kills"] : 0;
        int mvpDeaths = mvp != null && mvp.CustomProperties.ContainsKey("Deaths") ? (int)mvp.CustomProperties["Deaths"] : 0;
        int mvpAssists = mvp != null && mvp.CustomProperties.ContainsKey("Assists") ? (int)mvp.CustomProperties["Assists"] : 0;
        int mvpAvatarID = mvp != null && mvp.CustomProperties.ContainsKey("AvatarID") ? (int)mvp.CustomProperties["AvatarID"] : -1;

        photonView.RPC("RPC_SetMatchResult", RpcTarget.All, scoreTeamA, scoreTeamB,
            mvpName, mvpKills, mvpDeaths, mvpAssists, mvpAvatarID);

        PhotonNetwork.SendAllOutgoingCommands();
        yield return new WaitForSeconds(0.3f);

        string targetScene = (winningTeam == "TeamA") ? "Team Red Win" : "Team Red Lose";
        PhotonNetwork.LoadLevel(targetScene);
    }

    public void ReturnToInsideRoom()
    {
        MatchUIManager.Instance?.HideJUTPSUI();
        if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
        HitMarkerEffect hitEffect = FindObjectOfType<HitMarkerEffect>();
        if (hitEffect != null) hitEffect.enabled = false;

        if (PlayerPrefs.GetInt("RoomDisbanded", 0) == 1) { LeaveRoomAndQuit(); return; }

        PlayerSessionUtils.ResetPlayerProperties(true);

        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("MainMenu");
    }

    public void LeaveRoomAndQuit()
    {
        MatchUIManager.Instance?.HideJUTPSUI();
        if (SpectatorManager.Instance != null) SpectatorManager.Instance.StopSpectating();
        HitMarkerEffect hitEffect = FindObjectOfType<HitMarkerEffect>();
        if (hitEffect != null) hitEffect.enabled = false;

        PlayerSessionUtils.ResetPlayerProperties(false);
        
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom() { UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); }
    public override void OnMasterClientSwitched(Player newMasterClient) { }

    private void ActivateHitMarkerEffect() { StartCoroutine(ActivateHitMarkerEffectCoroutine()); }
    private IEnumerator ActivateHitMarkerEffectCoroutine()
    {
        yield return null;
        HitMarkerEffect hitEffect = FindObjectOfType<HitMarkerEffect>();
        if (hitEffect != null) { if (!hitEffect.enabled) hitEffect.enabled = true; }
        else
        {
            yield return new WaitForSeconds(0.5f);
            hitEffect = FindObjectOfType<HitMarkerEffect>();
            if (hitEffect != null && !hitEffect.enabled) hitEffect.enabled = true;
        }
    }
}