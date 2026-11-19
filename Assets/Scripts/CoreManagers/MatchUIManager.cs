using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MatchUIManager : MonoBehaviour
{
    public static MatchUIManager Instance;

    [Header("JUTPS Gameplay UI")]
    public GameObject jutpsGameplayUI;

    [Header("Team & Loading UI")]
    [SerializeField] private GameObject teamSelectionPanel;
    [SerializeField] private GameObject loadingPanel;
    public float loadingDuration = 2f;

    [Header("Match UI")]
    public TMP_Text timerText;
    public TMP_Text teamAScoreText;
    public TMP_Text teamBScoreText;
    public GameObject prepTextObj;
    private TMP_Text prepTextComponent;

    [Header("Round UI")]
    public GameObject roundAnnouncementPanel;
    public TMP_Text roundAnnouncementText;

    [Header("Bomb Progress UI")]
    public GameObject bombProgressContainer;
    public Image bombProgressBarFill;
    public TMP_Text statusText;

    [Header("Checkpoint UI")]
    public Image checkpointMarkerImage;
    public Color checkpointDefaultColor = Color.white;
    public Color checkpointReachedColor = Color.green;

    [Header("Messages UI")]
    public GameObject messagesPanel;
    public Text messageText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (roundAnnouncementPanel != null) roundAnnouncementPanel.SetActive(false);
        if (bombProgressContainer != null) bombProgressContainer.SetActive(false);
        if (messagesPanel != null) messagesPanel.SetActive(false);

        HideJUTPSUI(); 

        if (prepTextObj != null)
        {
            prepTextObj.SetActive(false);
            prepTextComponent = prepTextObj.GetComponent<TMP_Text>();
        }
    }

    #region JUTPS UI Management
    public void ShowJUTPSUI()
    {
        if (jutpsGameplayUI != null) jutpsGameplayUI.SetActive(true);
    }

    public void HideJUTPSUI()
    {
        if (jutpsGameplayUI != null) jutpsGameplayUI.SetActive(false);
    }
    #endregion

    #region Messages System
    public void ShowSystemMessage(string message)
    {
        if (messagesPanel != null)
        {
            messagesPanel.SetActive(false); 
            messagesPanel.SetActive(true);
            messagesPanel.transform.SetAsLastSibling();

            if (messageText != null) 
            {
                messageText.text = message;
            }
        }
    }
    #endregion

    #region Bomb Progress
    public void ShowBombProgress()
    {
        if (bombProgressContainer != null) bombProgressContainer.SetActive(true);
    }

    public void HideBombProgress()
    {
        if (bombProgressContainer != null) bombProgressContainer.SetActive(false);
    }

    public void UpdateBombProgress(float fillAmount)
    {
        if (bombProgressBarFill != null) bombProgressBarFill.fillAmount = fillAmount;
    }

    public void UpdateBombStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    public void SetCheckpointMarker(bool show, bool hasReached = false)
    {
        if (checkpointMarkerImage != null)
        {
            checkpointMarkerImage.gameObject.SetActive(show);
            if (show)
            {
                checkpointMarkerImage.color = hasReached ? checkpointReachedColor : checkpointDefaultColor;
            }
        }
    }
    #endregion

    #region Loading & Team Selection
    public void StartLoadingTransition()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
        
        HideJUTPSUI();
        
        StartCoroutine(TransitionToTeamSelection());
    }

    private IEnumerator TransitionToTeamSelection()
    {
        yield return new WaitForSeconds(loadingDuration);

        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(true);
    }

    public void ShowLoadingScreen()
    {
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
        HideJUTPSUI();
    }

    public void HideLoadingScreen()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void HideTeamSelection()
    {
        if (teamSelectionPanel != null) teamSelectionPanel.SetActive(false);
    }
    #endregion

    #region Match HUD Updates
    public void UpdateTimer(string timeString, Color color)
    {
        if (timerText != null)
        {
            timerText.text = timeString;
            timerText.color = color;
        }
    }

    public void UpdateScores(int scoreTeamA, int scoreTeamB)
    {
        if (teamAScoreText != null) teamAScoreText.text = scoreTeamA.ToString();
        if (teamBScoreText != null) teamBScoreText.text = scoreTeamB.ToString();
    }

    public void ShowPrepPhaseText(string text)
    {
        if (prepTextObj != null)
        {
            prepTextObj.SetActive(true);
            if (prepTextComponent != null) prepTextComponent.text = text;
        }
    }

    public void HidePrepPhaseText()
    {
        if (prepTextObj != null) prepTextObj.SetActive(false);
    }

    public void ShowRoundAnnouncement(string text)
    {
        if (roundAnnouncementPanel != null)
        {
            roundAnnouncementPanel.SetActive(true);
            if (roundAnnouncementText != null) roundAnnouncementText.text = text;
        }
    }

    public void HideRoundAnnouncement()
    {
        if (roundAnnouncementPanel != null) roundAnnouncementPanel.SetActive(false);
    }
    #endregion
}