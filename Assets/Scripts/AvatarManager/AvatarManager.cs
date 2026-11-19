using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class AvatarManager : MonoBehaviour
{
    [Header("UI References")]
    public Image currentAvatarDisplay; 
    
    [Header("Scroll View")]
    public AvatarSelectorUI scrollviewSelector;

    [Header("Avatar Database")]
    public AvatarDatabaseSO avatarDatabase;

    private void Start()
    {
        LoadSavedAvatar();
    }

    public void OpenAvatarSelection()
    {
        if (scrollviewSelector != null) 
        {
            scrollviewSelector.OpenPanel();
        }
        else
        {
            Debug.LogError("Chưa kéo thả AvatarSelectorUI vào AvatarManager!");
        }
    }

    public void OnAvatarSelectedFromScrollView(int index)
    {
        if (avatarDatabase != null && index >= 0 && index < avatarDatabase.avatars.Length)
        {
            currentAvatarDisplay.sprite = avatarDatabase.GetAvatar(index);
            SaveAvatarIndex(index);
        }
    }

    private void SaveAvatarIndex(int index)
    {
        PlayerPrefs.SetInt("PlayerAvatarIndex", index);
        PlayerPrefs.Save();

        Hashtable hash = new Hashtable();
        hash.Add("AvatarID", index);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    private void LoadSavedAvatar()
    {
        int savedIndex = PlayerPrefs.GetInt("PlayerAvatarIndex", 0);
        
        if (avatarDatabase != null && savedIndex >= 0 && savedIndex < avatarDatabase.avatars.Length)
        {
            currentAvatarDisplay.sprite = avatarDatabase.GetAvatar(savedIndex);

            Hashtable hash = new Hashtable();
            hash.Add("AvatarID", savedIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
        else
        {
            if (avatarDatabase != null && avatarDatabase.avatars.Length > 0)
            {
                currentAvatarDisplay.sprite = avatarDatabase.GetAvatar(0);
            }
        }
    }
}