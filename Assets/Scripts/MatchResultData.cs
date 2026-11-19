public static class MatchResultData
{
    public static bool HasResult = false;

    public static int ScoreTeamA = 0;
    public static int ScoreTeamB = 0;

    public static string MvpName = "Unknown";
    public static int MvpKills = 0;
    public static int MvpDeaths = 0;
    public static int MvpAssists = 0;
    public static int MvpAvatarID = -1;

    public static void Reset()
    {
        HasResult = false;
        ScoreTeamA = 0;
        ScoreTeamB = 0;
        MvpName = "Unknown";
        MvpKills = 0;
        MvpDeaths = 0;
        MvpAssists = 0;
        MvpAvatarID = -1;
    }
}