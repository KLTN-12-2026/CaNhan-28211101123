using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using System.Security.Cryptography;
using System.Text;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("GLOBAL MESSAGES UI")]
    public GameObject globalMessagesPanel; 
    public Text globalMessageText;

    [Header("Room UI")]
    public TMP_InputField roomNameText;
    public TMP_Dropdown gameModeDropdown;
    public TMP_InputField passwordInput;

    [Header("Join Password UI")]
    public GameObject joinPasswordPanel;
    public TMP_InputField joinPasswordInput;
    private string selectedRoomName;

    [Header("Room Description UI")]
    public GameObject descriptionPanel;
    public TMP_Text descRoomNameText;
    public TMP_Text descStatusText;
    public TMP_Text descGameModeText;
    public TMP_Text descMapText; 
    public GameObject joinRoomButtonObj; 

    [Header("Panels")]
    public GameObject LobbyPanel;
    public GameObject joinCreateRoomPanel; 
    public GameObject CreateRoomPanel;
    public GameObject LoadingPanel;
    public GameObject RoomListPanel;
    public GameObject noRoomsMessage; 

    private Dictionary <string, RoomInfo> roomListData;

    public GameObject roomListPrefabs;
    public GameObject roomListParent;

    private Dictionary <string, GameObject> roomListGameobject;

    [Header("Inside Room Panel")]
    public GameObject InsideRoomPanel;
    public GameObject playerListItemPrefab;
    public GameObject playerListItemParent;
    public GameObject PlayButton;

    [Header("Inside Room Info UI")]
    public TMP_Text inRoomNameText;
    public TMP_Text inRoomGameModeText;
    public TMP_Text inRoomMapText;
    
    [Header("Kick Player UI")]
    public GameObject confirmKickPanel;
    public TMP_Text confirmKickText;
    private Player playerToKick;

    [Header("Player Avatars")]
    public AvatarDatabaseSO avatarDatabase;

    [Header("Validation Settings")]
    [SerializeField] private int minRoomNameLength = 5;
    [SerializeField] private int maxNameLength = 13;
    [SerializeField] private int maxRepeatingChars = 3;

    [Header("Lobby UI")]
    public TMP_Text playerNameDisplay;
    public Image myAvatarDisplay;

    private Coroutine hideErrorCoroutine;
    private bool justLeftRoom = false;
    private float roomListOpenTime = 0f;
    private const float noRoomsMessageDelay = 1.25f;

    private string pendingPasswordAttempt = null;
    private bool isVerifyingPassword = false;
    private bool isStartingMatch = false;

    #region UnityMethods

    private void Awake()
    {
        Application.runInBackground = true;
        PhotonNetwork.KeepAliveInBackground = 600f;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        roomListData = new Dictionary<string, RoomInfo>();
        roomListGameobject = new Dictionary<string, GameObject>();

        if (roomNameText != null) roomNameText.characterLimit = maxNameLength;

        HideError();
        
        if (confirmKickPanel != null) confirmKickPanel.SetActive(false);
        if (descriptionPanel != null) descriptionPanel.SetActive(false); 
        if (joinRoomButtonObj != null) joinRoomButtonObj.SetActive(false);
        if (noRoomsMessage != null) noRoomsMessage.SetActive(false); 

        string savedName = PlayerPrefs.GetString("PlayerName", "");
        int savedAvatarID = PlayerPrefs.GetInt("PlayerAvatarIndex", 0);

        if (PhotonNetwork.InRoom)
        {
            if (string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
            {
                PhotonNetwork.LocalPlayer.NickName = savedName;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
            }

            ActivateMyPanel(InsideRoomPanel.name);
            UpdatePlayerListUI();
            UpdateInsideRoomInfo();
            UpdateLobbyUI(savedName, savedAvatarID);
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            if (!PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
            ActivateMyPanel(LobbyPanel.name);
            UpdateLobbyUI(PhotonNetwork.LocalPlayer.NickName, savedAvatarID);
        }
        else if (!string.IsNullOrEmpty(savedName))
        {
            PhotonNetwork.LocalPlayer.NickName = savedName;

            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add("AvatarID", savedAvatarID);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

            string playFabId = PlayerPrefs.GetString("PlayFabId", "");
            if (!string.IsNullOrEmpty(playFabId))
            {
                PhotonNetwork.AuthValues = new AuthenticationValues(playFabId);
            }

            PhotonNetwork.ConnectUsingSettings();
            ActivateMyPanel(LoadingPanel.name);
        }
        else
        {
            SceneManager.LoadScene("Login Scene"); 
        }
    }

    #endregion

    private void UpdateLobbyUI(string playerName, int avatarID)
    {
        if (playerNameDisplay != null) 
            playerNameDisplay.text = playerName;

        if (myAvatarDisplay != null && avatarDatabase != null)
        {
            myAvatarDisplay.sprite = avatarDatabase.GetAvatar(avatarID);
        }
    }

    #region UiMethods
    public void QuitGame()
    {
        SessionGuardian.StopGuarding();

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }

            PhotonNetwork.Disconnect();
        }

        PlayFabClientAPI.ForgetAllCredentials();

        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save();

        StartCoroutine(LoadLoginSceneSequence("Login Scene")); 
    }

    IEnumerator LoadLoginSceneSequence(string sceneName)
    {
        if (LoadingPanel != null) LoadingPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f); 

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float minLoadTime = 2f;
        float timer = 0f;
        
        while (!operation.isDone)
        {
            timer += Time.unscaledDeltaTime;
            
            if (operation.progress >= 0.9f && timer >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public void OnClickMainMenuPlay()
    {
        ActivateMyPanel(joinCreateRoomPanel.name);
    }

    public void OnClickRoomCreate()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            ShowGlobalError("Reconnecting to the lobby, please wait...");
            return;
        }

        string roomName = roomNameText.text;
        string password = passwordInput.text;

        if (ValidateInput(roomName, "Tên phòng", minRoomNameLength))
        {
            RoomOptions roomOptions = new RoomOptions();
            ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable();

            if (gameModeDropdown.value == 0)
            {
                roomOptions.MaxPlayers = 4;
                customRoomProperties.Add("gm", "2vs2");
            }
            else 
            {
                ShowGlobalError("Only the 2v2 mode is available!");
                gameModeDropdown.value = 0; 
                return; 
            }

            bool hasPassword = !string.IsNullOrEmpty(password);
            if (hasPassword)
            {
                customRoomProperties.Add("pw", HashPassword(password));
            }
            customRoomProperties.Add("hasPw", hasPassword);

            roomOptions.CustomRoomProperties = customRoomProperties;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "gm", "hasPw" };
            roomOptions.EmptyRoomTtl = 0;
            roomOptions.PlayerTtl = 10000;

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }
    }

    public void OnRoomItemClick(string roomName)
    {
        if (roomListData.ContainsKey(roomName))
        {
            selectedRoomName = roomName;
            RoomInfo info = roomListData[roomName];
            if (descriptionPanel != null) descriptionPanel.SetActive(true);
            if (joinRoomButtonObj != null) joinRoomButtonObj.SetActive(true);

            if (descRoomNameText != null) descRoomNameText.text = "Room name: " + info.Name;
            
            string modeDisplay = info.CustomProperties.ContainsKey("gm") ? (string)info.CustomProperties["gm"] : "Unknown";
            if (descGameModeText != null) descGameModeText.text = "Gamemode: " + modeDisplay;

            if (descStatusText != null)
            {
                if (!info.IsOpen) descStatusText.text = "Status: In Battle";
                else if (info.PlayerCount >= info.MaxPlayers && info.MaxPlayers != 0) descStatusText.text = "Status: Room is Full";
                else descStatusText.text = "Status: On Waiting";
            }

            if (descMapText != null) descMapText.text = "Map: DTU University";
        }
    }

    public void OnClickJoinSelectedRoom()
    {
        if (string.IsNullOrEmpty(selectedRoomName) || !roomListData.ContainsKey(selectedRoomName))
        {
            ShowGlobalError("Please select a room from the list on the left!");
            return;
        }

        RoomInfo info = roomListData[selectedRoomName];

        if (!info.IsOpen)
        {
            ShowGlobalError("This room has started!");
            return; 
        }

        if (info.PlayerCount >= info.MaxPlayers && info.MaxPlayers != 0)
        {
            ShowGlobalError("The room is full!");
            return;
        }

        bool needsPassword = info.CustomProperties.ContainsKey("hasPw") && (bool)info.CustomProperties["hasPw"];
        if (needsPassword)
        {
            joinPasswordInput.text = "";
            joinPasswordPanel.SetActive(true);
            joinPasswordPanel.transform.SetAsLastSibling();
        }
        else
        {
            RoomJoinFromList(selectedRoomName);
        }
    }

    public void OnConfirmPasswordJoin()
    {
        if (string.IsNullOrEmpty(joinPasswordInput.text))
        {
            ShowGlobalError("Please enter the password!");
            return;
        }

        pendingPasswordAttempt = joinPasswordInput.text;
        isVerifyingPassword = true;
        joinPasswordPanel.SetActive(false);
        RoomJoinFromList(selectedRoomName);
    }

    public void OnCancelPasswordJoin()
    {
        if (joinPasswordPanel != null) joinPasswordPanel.SetActive(false);
    }

    public void OnJoinRandomRoomClick()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            ShowGlobalError("Server not connected!");
            return;
        }

        List<string> validRooms = new List<string>();

        foreach (RoomInfo room in roomListData.Values)
        {
            if (room.IsOpen && room.IsVisible && room.PlayerCount < room.MaxPlayers)
            {
                bool hasPassword = room.CustomProperties.ContainsKey("hasPw") && (bool)room.CustomProperties["hasPw"];

                if (!hasPassword)
                {
                    validRooms.Add(room.Name);
                }
            }
        }

        if (validRooms.Count > 0)
        {
            int randomIndex = Random.Range(0, validRooms.Count);
            PhotonNetwork.JoinRoom(validRooms[randomIndex]);
        }
        else
        {
            ShowGlobalError("No suitable rooms found!");
        }
    }

    private static string HashPassword(string password)
    {
        if (password == null) password = "";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    private bool ValidateInput(string text, string fieldName, int minLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            ShowGlobalError($"Please enter {fieldName}!");
            return false;
        }

        if (text.Length < minLength)
        {
            ShowGlobalError($"{fieldName} too short (Minimum {minLength} characters)!");
            return false;
        }

        if (!Regex.IsMatch(text, "^[a-zA-Z0-9_]*$"))
        {
            ShowGlobalError($"{fieldName} No special characters allowed!");
            return false;
        }

        string spamPattern = @"(\w)\1{" + maxRepeatingChars + ",}";
        if (Regex.IsMatch(text, spamPattern))
        {
            ShowGlobalError($"{fieldName} Do not repeat too many characters!");
            return false;
        }

        return true;
    }

    private void ShowGlobalError(string message)
    {
        if (globalMessagesPanel != null)
        {
            if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);

            globalMessagesPanel.SetActive(true);
            globalMessagesPanel.transform.SetAsLastSibling(); 

            if (globalMessageText != null) globalMessageText.text = message;

            hideErrorCoroutine = StartCoroutine(AutoHideError(3f));
        }
    }

    IEnumerator AutoHideError(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideError();
    }

    public void OnCloseMessagesPanel()
    {
        HideError();
    }

    private void HideError()
    {
        if (globalMessagesPanel != null) globalMessagesPanel.SetActive(false);
        if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);
        hideErrorCoroutine = null;
    }

    public void OnCancelClick()
    {
        ActivateMyPanel(joinCreateRoomPanel.name);
    }

    public void RoomListBtnClicked()
    {
        ActivateMyPanel(RoomListPanel.name);
    }

    public void BackFromRoomList()
    {
        ActivateMyPanel(joinCreateRoomPanel.name);
    }

    public void BackToMainMenu()
    {
        ActivateMyPanel(LobbyPanel.name);
    }

    public void OnClickLeaveRoomButton()
    {
        if(PhotonNetwork.InRoom)
        {
            justLeftRoom = true; 
            ActivateMyPanel(LoadingPanel.name); 
            LeaveRoomSafely(); 
        }
        else
        {
            ActivateMyPanel(joinCreateRoomPanel.name);
        }
    }

    private void LeaveRoomSafely()
    {
        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        PhotonNetwork.LeaveRoom();
    }

    public void OpenConfirmKickPanel(Player targetPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            playerToKick = targetPlayer;
            if (confirmKickPanel != null)
            {
                confirmKickPanel.SetActive(true);
                confirmKickPanel.transform.SetAsLastSibling(); 
                if (confirmKickText != null) confirmKickText.text = "Want to kick this player?";
            }
        }
    }

    public void ConfirmKickPlayer()
    {
        if (PhotonNetwork.IsMasterClient && playerToKick != null)
        {
            photonView.RPC("RPC_BeKicked", playerToKick);
        }
        CloseConfirmKickPanel();
    }

    public void CloseConfirmKickPanel()
    {
        playerToKick = null;
        if (confirmKickPanel != null) confirmKickPanel.SetActive(false);
    }

    [PunRPC]
    private void RPC_BeKicked(PhotonMessageInfo info)
    {
        if (info.Sender == null || !info.Sender.IsMasterClient)
        {
            Debug.LogWarning($"Ignored RPC_BeKicked from non-master sender: {info.Sender?.NickName}");
            return;
        }

        PlayerPrefs.SetInt("WasKicked", 1);
        PlayerPrefs.Save();
        LeaveRoomSafely();
    }
    #endregion

    #region PHOTON_CALLBACKS
    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        if (!PhotonNetwork.InRoom)
        {
            if (PlayerPrefs.GetInt("RoomDisbanded", 0) == 1)
            {
                ActivateMyPanel(RoomListPanel.name);
                ShowGlobalError("The room has been disbanded.");
                PlayerPrefs.SetInt("RoomDisbanded", 0); 
                PlayerPrefs.Save();
            }
            else if (PlayerPrefs.GetInt("WasKicked", 0) == 1)
            {
                ActivateMyPanel(RoomListPanel.name);
                ShowGlobalError("You've been kicked!");
                PlayerPrefs.SetInt("WasKicked", 0);
                PlayerPrefs.Save();
            }
            else
            {
                if (justLeftRoom)
                {
                    ActivateMyPanel(RoomListPanel.name);
                    justLeftRoom = false; 
                }
                else 
                {
                    ActivateMyPanel(LobbyPanel.name);
                }
            }

            int savedAvatarID = PlayerPrefs.GetInt("PlayerAvatarIndex", 0);
            UpdateLobbyUI(PhotonNetwork.LocalPlayer.NickName, savedAvatarID);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ShowGlobalError("The room number already exists/ The network connection is unstable!");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isVerifyingPassword = false;
        pendingPasswordAttempt = null;

        if (returnCode == ErrorCode.GameFull) ShowGlobalError("The room is full!");
        else if (returnCode == ErrorCode.GameClosed) ShowGlobalError("This room is either closed or in play!");
        else ShowGlobalError("Unable to join the room: " + message);
        
        ActivateMyPanel(RoomListPanel.name);
    }

    public override void OnJoinedRoom()
    {
        if (isVerifyingPassword)
        {
            isVerifyingPassword = false;
            string correctPasswordHash = "";
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("pw"))
            {
                correctPasswordHash = PhotonNetwork.CurrentRoom.CustomProperties["pw"] as string;
            }

            if (HashPassword(pendingPasswordAttempt) != correctPasswordHash)
            {
                pendingPasswordAttempt = null;
                ShowGlobalError("Incorrect password!");
                justLeftRoom = true;
                LeaveRoomSafely();
                return;
            }

            pendingPasswordAttempt = null;
        }

        ActivateMyPanel(LoadingPanel.name);
        StartCoroutine(TransitionToInsideRoom());
    }

    private IEnumerator TransitionToInsideRoom()
    {
        yield return new WaitForSecondsRealtime(1f);
        ActivateMyPanel(InsideRoomPanel.name);
        UpdatePlayerListUI();
        UpdateInsideRoomInfo();
    }

    private void UpdateInsideRoomInfo()
    {
        if (PhotonNetwork.InRoom)
        {
            if (inRoomNameText != null) inRoomNameText.text = "Room name: " + PhotonNetwork.CurrentRoom.Name;
            string modeDisplay = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("gm") ? 
                                 (string)PhotonNetwork.CurrentRoom.CustomProperties["gm"] : "Unknown";
            if (inRoomGameModeText != null) inRoomGameModeText.text = "Gamemode: " + modeDisplay;
            if (inRoomMapText != null) inRoomMapText.text = "Map: DTU University";
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ShowGlobalError($"{newPlayer.NickName} entered room!");
        UpdatePlayerListUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowGlobalError($"{otherPlayer.NickName} left room!");
        UpdatePlayerListUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient) return;

        if (PhotonNetwork.InRoom)
        {
            PlayerPrefs.SetInt("RoomDisbanded", 1);
            PlayerPrefs.Save();
            LeaveRoomSafely(); 
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("AvatarID") && PhotonNetwork.InRoom)
        {
            UpdatePlayerListUI();
        }
    }

    private void UpdatePlayerListUI()
    {
        if (playerListItemParent == null) return; 

        for (int i = playerListItemParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = playerListItemParent.transform.GetChild(i);
            if (child != null && child.gameObject != null)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject playerListItem = Instantiate(playerListItemPrefab, playerListItemParent.transform);
            playerListItem.transform.localScale = Vector3.one;
            playerListItem.name = p.ActorNumber.ToString();
            
            if (playerListItem.transform.childCount > 1)
            {
                TMP_Text nameText = playerListItem.transform.GetChild(1).GetComponent<TMP_Text>();
                if (nameText != null) nameText.text = p.NickName;
                else Debug.LogWarning("playerListItemPrefab child 1 has no TMP_Text component.");
            }

            if (p.CustomProperties.ContainsKey("AvatarID") && avatarDatabase != null)
            {
                int avatarID = (int)p.CustomProperties["AvatarID"];
                Sprite avatarSprite = avatarDatabase.GetAvatar(avatarID);

                if (avatarSprite != null)
                {
                    if (playerListItem.transform.childCount > 0)
                    {
                        Transform maskTransform = playerListItem.transform.GetChild(0);
                        if (maskTransform.childCount > 0)
                        {
                            Image avatarImage = maskTransform.GetChild(0).GetComponent<Image>();
                            if (avatarImage != null)
                            {
                                avatarImage.sprite = avatarSprite;
                            }
                        }
                    }
                }
            }

            if (playerListItem.transform.childCount > 2)
            {
                Button kickBtn = playerListItem.transform.GetChild(2).GetComponent<Button>();
                if (kickBtn != null)
                {
                    kickBtn.gameObject.SetActive(PhotonNetwork.IsMasterClient && !p.IsLocal);
                    kickBtn.onClick.RemoveAllListeners();
                    kickBtn.onClick.AddListener(() => OpenConfirmKickPanel(p));
                }
            }

            if (playerListItem.transform.childCount > 3)
            {
                GameObject crownIcon = playerListItem.transform.GetChild(3).gameObject;
                if (crownIcon != null)
                {
                    crownIcon.SetActive(p.IsMasterClient);
                }
            }
        }

        if (PlayButton != null) PlayButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void OnClickPlayButton()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isStartingMatch) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            ShowGlobalError("At least two players are needed to start a match!");
            return;
        }

        isStartingMatch = true;
        if (PlayButton != null)
        {
            Button playBtn = PlayButton.GetComponent<Button>();
            if (playBtn != null) playBtn.interactable = false;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false; 
        PhotonNetwork.CurrentRoom.IsVisible = false; 
        PhotonNetwork.LoadLevel("Game"); 
    }

    public override void OnLeftRoom()
    {
        CloseConfirmKickPanel();

        if (roomNameText != null) roomNameText.text = "";
        if (passwordInput != null) passwordInput.text = "";
        if (gameModeDropdown != null) gameModeDropdown.value = 0;
        if (joinPasswordInput != null) joinPasswordInput.text = "";
        
        if (playerListItemParent != null)
        {
            for (int i = playerListItemParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = playerListItemParent.transform.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomList();

        foreach (RoomInfo rooms in roomList)
        {
            if(!rooms.IsVisible || rooms.RemovedFromList)
            {
                if(roomListData.ContainsKey(rooms.Name)) roomListData.Remove(rooms.Name);
            }
            else
            {
                if(roomListData.ContainsKey(rooms.Name)) roomListData[rooms.Name] = rooms;
                else roomListData.Add(rooms.Name, rooms);
            }
        }

        if (!string.IsNullOrEmpty(selectedRoomName) && !roomListData.ContainsKey(selectedRoomName))
        {
            selectedRoomName = "";
            if (descriptionPanel != null) descriptionPanel.SetActive(false);
            if (joinRoomButtonObj != null) joinRoomButtonObj.SetActive(false);
        }

        RefreshNoRoomsMessage();

        foreach(RoomInfo roomItem in roomListData.Values)
        {
            if (roomListParent == null) break; 

            GameObject roomListItemObject = Instantiate(roomListPrefabs);
            roomListItemObject.transform.SetParent(roomListParent.transform, false);
            roomListItemObject.transform.localScale = Vector3.one;
            
            TMP_Text roomNameLabel = roomListItemObject.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
            if (roomNameLabel != null) roomNameLabel.text = roomItem.Name;
            else Debug.LogWarning("roomListPrefabs child 0 has no TMP_Text component.");
            
            Button itemBtn = roomListItemObject.GetComponent<Button>();
            if (itemBtn != null)
            {
                itemBtn.onClick.RemoveAllListeners();
                itemBtn.onClick.AddListener(() => OnRoomItemClick(roomItem.Name)); 
            }

            roomListGameobject.Add(roomItem.Name, roomListItemObject);  
        }
    }

    public override void OnLeftLobby()
    {
        ClearRoomList();
        roomListData.Clear();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("Lost connection: " + cause.ToString());

        if (cause == DisconnectCause.DisconnectByClientLogic) return;

        if (globalMessagesPanel != null)
        {
            ShowGlobalError("Disconnected! Trying to reconnect...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    #endregion

    #region Public_Methods
    public void RoomJoinFromList(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void ClearRoomList()
    {
        foreach(var v in roomListGameobject.Values)
        {
            if (v != null) Destroy(v); 
        }
        roomListGameobject.Clear();
    }

    public void ActivateMyPanel(string panelName)
    {
        if (LobbyPanel != null) 
        {
            if (panelName.Equals(InsideRoomPanel.name))
            {
                LobbyPanel.SetActive(false);
            }
            else
            {
                LobbyPanel.SetActive(panelName.Equals(LobbyPanel.name));
            }
        }

        if (joinCreateRoomPanel != null) joinCreateRoomPanel.SetActive(panelName.Equals(joinCreateRoomPanel.name));
        if (CreateRoomPanel != null) CreateRoomPanel.SetActive(panelName.Equals(CreateRoomPanel.name));
        if (LoadingPanel != null) LoadingPanel.SetActive(panelName.Equals(LoadingPanel.name));
        if (RoomListPanel != null) RoomListPanel.SetActive(panelName.Equals(RoomListPanel.name));
        if (InsideRoomPanel != null) InsideRoomPanel.SetActive(panelName.Equals(InsideRoomPanel.name));
        if (joinPasswordPanel != null) joinPasswordPanel.SetActive(false);

        if (panelName.Equals(RoomListPanel.name))
        {
            selectedRoomName = "";
            if (descriptionPanel != null) descriptionPanel.SetActive(false);
            if (joinRoomButtonObj != null) joinRoomButtonObj.SetActive(false);
            
            roomListOpenTime = Time.time;
            if (noRoomsMessage != null)
            {
                noRoomsMessage.SetActive(false);
                StartCoroutine(ShowNoRoomsMessageDelayed());
            }
        }
        else
        {
            if (noRoomsMessage != null) noRoomsMessage.SetActive(false);
        }

        HideError();
    }

    IEnumerator ShowNoRoomsMessageDelayed()
    {
        yield return new WaitForSeconds(noRoomsMessageDelay);
        RefreshNoRoomsMessage();
    }

    private void RefreshNoRoomsMessage()
    {
        if (noRoomsMessage == null || RoomListPanel == null || !RoomListPanel.activeInHierarchy) return;
        if (Time.time - roomListOpenTime < noRoomsMessageDelay) return;

        noRoomsMessage.SetActive(roomListData.Count == 0);
    }
    #endregion
}