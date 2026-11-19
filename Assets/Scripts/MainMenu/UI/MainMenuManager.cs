using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MainMenuPanel;
    public GameObject OptionsPanel;
    public GameObject VideoPanel;
    public GameObject AudioPanel;
    public GameObject ControlsPanel;
    public GameObject KeyBindingPanel;
    public GameObject Join_Create_Room_Panel;
    public GameObject UserPanel;
    public GameObject RoomCreatePanel;
    public GameObject Room_List_LobbyPanel;
    public GameObject JoinRoomPanel;
    public GameObject PasswordPanel;

    [Header("Label")]
    public TextMeshProUGUI labelText;
    public GameObject LabelPanelTop;
    public GameObject LabelPanelBottom;
    private GameObject currentPanel;

    public Animator animator;
    public Animator buttonani;
    public Animator roomani;
    public Animator roomListAni;
    public Animator lobbyAni;

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            HideAllPanels();
        }
        else
        {
            ShowMainMenu();
        }
    }

    void HideAllPanels()
    {
        MainMenuPanel.SetActive(false);
        OptionsPanel.SetActive(false);
        VideoPanel.SetActive(false);
        AudioPanel.SetActive(false);
        ControlsPanel.SetActive(false);
        KeyBindingPanel.SetActive(false);
        Join_Create_Room_Panel.SetActive(false);
        UserPanel.SetActive(false);
        RoomCreatePanel.SetActive(false);
        Room_List_LobbyPanel.SetActive(false);
        JoinRoomPanel.SetActive(false);
    }

    void ShowPanel(GameObject panel, string title = "")
    {
        HideAllPanels();

        panel.SetActive(true);
        currentPanel = panel;

        if (string.IsNullOrEmpty(title))
        {
            LabelPanelTop.SetActive(false);
            LabelPanelBottom.SetActive(false);
        }
        else
        {
            LabelPanelTop.SetActive(true);
            LabelPanelBottom.SetActive(true);
            labelText.SetText(title);
        }
    }

    public void ShowMainMenu()
    {
        ShowPanel(MainMenuPanel);
        UserPanel.SetActive(true);
    }

    public void OpenOptionsPanel()
    {
        ShowPanel(OptionsPanel, "OPTIONS");
        if (animator != null) animator.SetBool("Open",true);
    }

    public void OpenAudioPanel()
    {
        StartCoroutine(CloseOptionsThen(() =>
        {
            ShowPanel(AudioPanel, "Audio");
        }));
    }

    public void OpenVideoPanel()
    {
        StartCoroutine(CloseOptionsThen(() =>
        {
            ShowPanel(VideoPanel, "Video");
        }));
    }

    public void OpenControlPanel()
    {
        StartCoroutine(CloseOptionsThen(() =>
        {
            ShowPanel(ControlsPanel, "Controls");
        }));
    }

    public void OpenKeyBindingPanel()
    {
        StartCoroutine(CloseOptionsThen(() =>
        {
            ShowPanel(KeyBindingPanel, "Key Binding");
        }));
    }

    public void OpenJoinCreateRoomPanel()
    {
        ShowPanel(Join_Create_Room_Panel, "");
        LabelPanelBottom.SetActive(true);
        UserPanel.SetActive(true);
        if (buttonani != null) buttonani.SetBool("isOpen",true);
    }

    public void OpenCreateRoomPanel()
    {
        ShowPanel(RoomCreatePanel, "");
        LabelPanelBottom.SetActive(true);
        UserPanel.SetActive(false);
        if (roomani != null) roomani.SetBool("Open", true);
    }

    public void OpenRoomListPanel()
    {
        ShowPanel(Room_List_LobbyPanel, "");
        LabelPanelBottom.SetActive(false);
        UserPanel.SetActive(false);
        
        if (roomListAni != null) roomListAni.SetBool("Open", true);
    }

    public void OpenJoinRoomPanel()
    {
        ShowPanel(JoinRoomPanel, "");
        LabelPanelBottom.SetActive(false);
        UserPanel.SetActive(false);
    }

    public void OpenPasswordPanel()
    {
        PasswordPanel.SetActive(true);
    }

    public void ClosePasswordPanel()
    {
        PasswordPanel.SetActive(false);
    }

    public void TransitionToRoomList()
    {
        StartCoroutine(CloseJoinThen(() =>
        {
            PhotonManager pm = Object.FindFirstObjectByType<PhotonManager>();
            if (pm != null)
            {
                pm.RoomListBtnClicked();
            }
        }));
    }

    public void Back()
    {
        if (currentPanel == null) return; 

        switch (currentPanel)
        {
            case var panel when panel == OptionsPanel:
                StartCoroutine(CloseOptionsThen(ShowMainMenu));
                break;
            case var panel when panel == Join_Create_Room_Panel:
                StartCoroutine(CloseJoinThen(ShowMainMenu));
                break;
            case var panel when panel == RoomCreatePanel:
                StartCoroutine(CloseRoomCreatePanelThen(OpenJoinCreateRoomPanel));
                break;
            case var panel when panel == Room_List_LobbyPanel:
                StartCoroutine(CloseRoomListThen(OpenJoinCreateRoomPanel));
                break;
            case var panel when panel == MainMenuPanel:
                break;
            default:
                OpenOptionsPanel();
                break;
        }
    }

    IEnumerator CloseRoomListThen(System.Action onClosed)
    {
        if (roomListAni != null) roomListAni.SetBool("Open", false);
        yield return new WaitForSeconds(0.5f);
        onClosed?.Invoke();
    }

    IEnumerator CloseRoomCreatePanelThen(System.Action onClosed)
    {
        if (roomani != null) roomani.SetBool("Open", false);
        yield return new WaitForSeconds(0.5f);
        onClosed?.Invoke();
    }

    IEnumerator CloseJoinThen(System.Action onClosed)
    {
        if (buttonani != null) buttonani.SetBool("isOpen", false);
        yield return new WaitForSeconds(0.5f);
        onClosed?.Invoke();
    }

    IEnumerator CloseOptionsThen(System.Action onClosed)
    {
        if (animator != null) animator.SetBool("Open", false);
        yield return new WaitForSeconds(0.09f); 
        onClosed?.Invoke();
    }

    void Update()
    {
        AutoSyncWithPhotonManager();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Back();
        }
    }

    void AutoSyncWithPhotonManager()
    {
        if (Join_Create_Room_Panel.activeInHierarchy && currentPanel != Join_Create_Room_Panel)
        {
            currentPanel = Join_Create_Room_Panel;
            LabelPanelBottom.SetActive(true);
            UserPanel.SetActive(true);
            if (buttonani != null) buttonani.SetBool("isOpen", true);
        }
        else if (RoomCreatePanel.activeInHierarchy && currentPanel != RoomCreatePanel)
        {
            currentPanel = RoomCreatePanel;
            LabelPanelBottom.SetActive(true);
            UserPanel.SetActive(false);
            if (roomani != null) roomani.SetBool("Open", true);
        }
        else if (Room_List_LobbyPanel.activeInHierarchy && currentPanel != Room_List_LobbyPanel)
        {
            currentPanel = Room_List_LobbyPanel;
            LabelPanelBottom.SetActive(false);
            UserPanel.SetActive(false);
            
            if (roomListAni != null) roomListAni.SetBool("Open", true);
        }
        else if (MainMenuPanel.activeInHierarchy && currentPanel != MainMenuPanel)
        {
            currentPanel = MainMenuPanel;
            UserPanel.SetActive(true);
        }

        if (currentPanel != null && !currentPanel.activeInHierarchy)
        {
            LabelPanelBottom.SetActive(false);
            currentPanel = null;
        }
    }
}