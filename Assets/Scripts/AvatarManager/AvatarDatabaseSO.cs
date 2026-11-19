using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatarDatabase", menuName = "GameData/Avatar Database")]
public class AvatarDatabaseSO : ScriptableObject
{
    public Sprite[] avatars;

    public Sprite GetAvatar(int index)
    {
        if (avatars != null && index >= 0 && index < avatars.Length)
        {
            return avatars[index];
        }
        return null;
    }
}