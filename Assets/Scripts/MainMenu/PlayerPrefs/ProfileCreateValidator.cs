using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text.RegularExpressions;

public class ProfileCreateValidator : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nameInputField;
    public AvatarSelectorUI avatarSelector;

    [Header("Avatar")]
    public bool hasSelectedAvatar = false;

    [Header("Button")]
    public GameObject continueButton;
    public GameObject LoadingScene;

    [Header("Global Error UI")]
    public GameObject globalBugPanel; 
    public TMP_Text globalBugText;
    private Coroutine hideErrorCoroutine;

    [Header("Exit Game UI")]
    public GameObject exitConfirmPanel;

    [Header("Validation Settings")]
    [SerializeField] private int minPlayerNameLength = 5;
    [SerializeField] private int maxNameLength = 13;
    [SerializeField] private int maxRepeatingChars = 3;

    private void Start()
    {
        if (continueButton != null) continueButton.SetActive(false);
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
        
        if (nameInputField != null) 
        {
            nameInputField.characterLimit = maxNameLength;
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);
        }
        HideError();
    }

    private void OnNameInputChanged(string input)
    {
        if (input.Contains(" "))
        {
            ShowGlobalError("Tên không được chứa dấu cách!");
            nameInputField.text = input.Replace(" ", "");
        }
    }

    public void OnClickContinue()
    {
        string inputName = nameInputField.text;

        if (ValidateInput(inputName, "Họ & Tên", minPlayerNameLength))
        {
            HideError();
            SaveAndLoadMainMenu(inputName, ProfileDataManager.AvatarIndex);
        }
    }

    public void OnClickGuestLogin()
    {
        HideError();

        int randomNum = Random.Range(1000, 10000);
        string guestName = "Guest" + randomNum;

        if (nameInputField != null) 
        {
            nameInputField.text = guestName;
        }

        int finalAvatarIndex = hasSelectedAvatar ? ProfileDataManager.AvatarIndex : 0;
        SaveAndLoadMainMenu(guestName, finalAvatarIndex);
    }

    public void OnClickQuitGame()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(true);
            exitConfirmPanel.transform.SetAsLastSibling();
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("Người chơi đã xác nhận thoát game!");
        Application.Quit();
    }

    public void CancelExit()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(false);
        }
    }

    private void SaveAndLoadMainMenu(string playerName, int avatarIndex)
    {
        ProfileDataManager.SaveProfile(playerName, avatarIndex);
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetInt("PlayerAvatarIndex", avatarIndex);
        PlayerPrefs.Save();

        StartCoroutine(LoadSceneSequence("MainMenu"));
    }

    private bool ValidateInput(string text, string fieldName, int minLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            ShowGlobalError($"Vui lòng nhập {fieldName}!");
            return false;
        }

        if (text.Length < minLength)
        {
            ShowGlobalError($"{fieldName} quá ngắn (Tối thiểu {minLength} ký tự)!");
            return false;
        }

        if (!Regex.IsMatch(text, "^[a-zA-Z0-9_]*$"))
        {
            ShowGlobalError($"{fieldName} không được chứa ký tự đặc biệt!");
            return false;
        }

        string spamPattern = @"(\w)\1{" + maxRepeatingChars + ",}";
        if (Regex.IsMatch(text, spamPattern))
        {
            ShowGlobalError($"{fieldName} không được lặp lại quá nhiều ký tự!");
            return false;
        }

        return true;
    }

    private void ShowGlobalError(string message)
    {
        if (globalBugPanel != null)
        {
            if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);

            globalBugPanel.SetActive(true);
            globalBugPanel.transform.SetAsLastSibling();

            if (globalBugText != null) globalBugText.text = message;

            hideErrorCoroutine = StartCoroutine(AutoHideError(3f));
        }
    }

    IEnumerator AutoHideError(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideError();
    }

    public void OnCloseBugPanel()
    {
        HideError();
    }

    private void HideError()
    {
        if (globalBugPanel != null) globalBugPanel.SetActive(false);
        if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);
        hideErrorCoroutine = null;
    }

    IEnumerator LoadSceneSequence(string sceneName)
    {
        if (LoadingScene != null) LoadingScene.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f); 

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float minLoadTime = 2f;
        float timer = 0f;
        
        while (!operation.isDone)
        {
            timer += Time.unscaledDeltaTime;
            
            if (operation.progress >= 0.9f && timer >= minLoadTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private void Update()
    {
        bool hasName = !string.IsNullOrEmpty(nameInputField.text);
        bool canContinue = hasName && hasSelectedAvatar;
        if (continueButton != null) continueButton.SetActive(canContinue);
    }

    public void SetAvatarSelected()
    {
        hasSelectedAvatar = true;
    }
}