using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchResultDisplay : MonoBehaviour
{
    [Header("Ti So (Score) UI")]
    public TMP_Text scoreTeamAText;
    public TMP_Text scoreTeamBText;

    [Header("MVP UI")]
    public TMP_Text mvpNameText;
    public TMP_Text mvpKdaText;
    public Image mvpAvatarImage;
    public AvatarDatabaseSO avatarDatabase;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!MatchResultData.HasResult)
        {
            HandleMissingResult();
            return;
        }

        if (scoreTeamAText != null) scoreTeamAText.text = MatchResultData.ScoreTeamA.ToString();
        if (scoreTeamBText != null) scoreTeamBText.text = MatchResultData.ScoreTeamB.ToString();

        if (mvpNameText != null) mvpNameText.text = MatchResultData.MvpName;

        if (mvpKdaText != null)
        {
            mvpKdaText.text = $"{MatchResultData.MvpKills}/{MatchResultData.MvpDeaths}/{MatchResultData.MvpAssists}";
        }

        if (mvpAvatarImage != null && avatarDatabase != null && MatchResultData.MvpAvatarID >= 0)
        {
            Sprite avatarSprite = avatarDatabase.GetAvatar(MatchResultData.MvpAvatarID);
            if (avatarSprite != null) mvpAvatarImage.sprite = avatarSprite;
        }
    }

    private void HandleMissingResult()
    {
        if (scoreTeamAText != null) scoreTeamAText.text = "-";
        if (scoreTeamBText != null) scoreTeamBText.text = "-";
        if (mvpNameText != null) mvpNameText.text = "Dang dong bo...";
        if (mvpKdaText != null) mvpKdaText.text = "";

    }
}