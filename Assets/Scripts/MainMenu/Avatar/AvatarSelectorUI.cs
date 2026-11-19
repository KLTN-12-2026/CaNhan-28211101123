using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelectorUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject avatarSelectPanel;   
    public Image currentAvatarImage;

    public Animator animator;
    public ProfileCreateValidator validator;

    [Header("Avatar Database")]
    public AvatarDatabaseSO avatarDatabase;

    private int currentAvatarIndex = -1;

    private void Awake()
    {
        if (avatarSelectPanel != null)
            avatarSelectPanel.SetActive(false);
    }

    private void Start()
    {
        if(animator != null) animator.SetBool("isStart",true);
    }

    public void OpenPanel()
    {
        if (avatarSelectPanel != null)
            avatarSelectPanel.SetActive(true);
    }

    public void SelectAvatar(int index)
    {
        if (avatarDatabase == null || index < 0 || index >= avatarDatabase.avatars.Length)
        {
            Debug.LogError("Avatar index không hợp lệ hoặc chưa kéo thả Avatar Database vào Inspector: " + index);
            return;
        }

        currentAvatarIndex = index;
        
        if (currentAvatarImage != null)
        {
            currentAvatarImage.sprite = avatarDatabase.GetAvatar(index);
        }

        ProfileDataManager.AvatarIndex = index;
        PlayerPrefs.SetInt("PlayerAvatarIndex", index);
        PlayerPrefs.Save();

        avatarSelectPanel.SetActive(false);
        if(animator != null) animator.SetBool("isStart", false);
      
        if (validator != null)
        {
            validator.SetAvatarSelected();
        }
        
        AvatarManager avatarManager = FindFirstObjectByType<AvatarManager>();
        if (avatarManager != null)
        {
            avatarManager.OnAvatarSelectedFromScrollView(index);
        }
    }

    public int GetSelectedAvatarIndex()
    {
        return currentAvatarIndex;
    }
}