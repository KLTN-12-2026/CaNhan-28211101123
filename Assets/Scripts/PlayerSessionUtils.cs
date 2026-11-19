using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PlayerSessionUtils
{
    public static void ResetPlayerProperties(bool isFullReset = false)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        Hashtable hash = new Hashtable();
        
        hash.Add("Team", ""); 
        hash.Add("IsDead", false); 
        
        if (isFullReset)
        {
            hash.Add("RoundReady", 0);
            hash.Add("Kills", 0); 
            hash.Add("Deaths", 0); 
            hash.Add("Assists", 0);
            hash.Add("Damage", 0f); 
            hash.Add("SavedArmor", 0); 
            hash.Add("SavedPrimary", "");
            hash.Add("SavedSecondary", ""); 
            hash.Add("Cash", 800);
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }
}