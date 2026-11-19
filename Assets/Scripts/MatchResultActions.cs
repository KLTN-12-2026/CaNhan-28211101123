using UnityEngine;
using Photon.Pun;

public class MatchResultActions : MonoBehaviourPunCallbacks
{
    public void OnReturnButtonClicked()
    {
        if (PlayerPrefs.GetInt("RoomDisbanded", 0) == 1)
        {
            OnExitButtonClicked();
            return; 
        }

        PlayerSessionUtils.ResetPlayerProperties(true);
        MatchResultData.Reset();

        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("MainMenu");
    }

    public void OnExitButtonClicked()
    {
        PlayerSessionUtils.ResetPlayerProperties(false);

        MatchResultData.Reset();

        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}