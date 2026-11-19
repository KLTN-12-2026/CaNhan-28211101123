using UnityEngine;
using UnityEngine.UI;

public class AvatarItemButton : MonoBehaviour
{
    public int avatarIndex;             
    public AvatarSelectorUI selectorUI;     

    public void OnClick()
    {
        selectorUI.SelectAvatar(avatarIndex);
    }
}
