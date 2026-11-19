using UnityEngine;

public static class ProfileDataManager
{
    public static string PlayerName;
    public static int AvatarIndex;

    const string NAME_KEY = "PLAYER_NAME";
    const string AVATAR_KEY = "PLAYER_AVATAR";
    public static void SaveProfile(string name, int avatarIndex)
    {
        PlayerName = name;
        AvatarIndex = avatarIndex;

        PlayerPrefs.SetString(NAME_KEY, name);
        PlayerPrefs.SetInt(AVATAR_KEY, avatarIndex);
        PlayerPrefs.Save();
    }
    public static void LoadProfile()
    {
        PlayerName = PlayerPrefs.GetString(NAME_KEY, "");
        AvatarIndex = PlayerPrefs.GetInt(AVATAR_KEY, -1);
    }
    public static bool HasProfile()
    {
        return PlayerPrefs.HasKey(NAME_KEY) && PlayerPrefs.HasKey(AVATAR_KEY);
    }
}
