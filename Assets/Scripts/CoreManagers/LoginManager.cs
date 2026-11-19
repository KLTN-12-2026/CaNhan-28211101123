using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic; 
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject loadingCanvas;
    
    [Header("Messages Panel")]
    public GameObject messagesPanel;
    public Text messageText;

    [Header("Login UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput; 
    public Toggle rememberMeToggle;

    private Coroutine hideErrorCoroutine;
    
    private bool isLoggingIn = false; 
    private bool isAutoLoginAttempt = false;
    private string lastLoginInputTrimmed = "";

    [Header("Rate-limit Protection")]
    public float loginCooldown = 3f;
    private float lastLoginAttemptTime = -999f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        HideError();
        ShowLoginPanel(false); 

        if (loadingCanvas != null) loadingCanvas.SetActive(false);
        
        string disconnectReason = PlayerPrefs.GetString("DisconnectReason", "");
        if (!string.IsNullOrEmpty(disconnectReason))
        {
            ShowError(disconnectReason);
            PlayerPrefs.DeleteKey("DisconnectReason");
        }

        bool isRemembered = PlayerPrefs.GetInt("RememberMe", 0) == 1;
        if (rememberMeToggle != null) rememberMeToggle.isOn = isRemembered;

        string savedToken = PlayerPrefs.GetString("RememberMeToken", "");

        if (isRemembered && !string.IsNullOrEmpty(savedToken))
        {
            if (usernameInput != null) usernameInput.text = PlayerPrefs.GetString("SavedUsername", "");
            AttemptAutoLogin(savedToken);
        }
        else if (usernameInput != null)
        {
            usernameInput.text = PlayerPrefs.GetString("LastLoginID", "");
        }
    }

    private void AttemptAutoLogin(string token)
    {
        isLoggingIn = true;
        isAutoLoginAttempt = true;

        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            loadingCanvas.transform.SetAsLastSibling();
        }

        var request = new LoginWithCustomIDRequest
        {
            CustomId = token,
            CreateAccount = false,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, error =>
        {
            isLoggingIn = false;
            isAutoLoginAttempt = false;
            if (loadingCanvas != null) loadingCanvas.SetActive(false);

            PlayerPrefs.DeleteKey("RememberMeToken");
            PlayerPrefs.SetInt("RememberMe", 0);
            PlayerPrefs.Save();
            if (rememberMeToggle != null) rememberMeToggle.isOn = false;

            ShowError("Your saved session has expired. Please log in again.");
        });
    }
    
    public void ShowRegisterPanel()
    {
        if (isLoggingIn) return;
        
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        
        ResetLoginInputs();
        if (RegisterManager.Instance != null) RegisterManager.Instance.ResetRegisterInputs();
        
        HideError();
    }

    public void ShowLoginPanel()
    {
        ShowLoginPanel(true);
    }

    public void ShowLoginPanel(bool resetInputs)
    {
        if (isLoggingIn) return;
        
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        
        if (resetInputs) ResetLoginInputs();
        HideError();
    }

    private void ResetLoginInputs()
    {
        if (rememberMeToggle != null && rememberMeToggle.isOn) return;

        if (usernameInput != null) usernameInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
    }

    public void OnLoginButtonClicked()
    {
        if (isLoggingIn) return;

        if (Time.unscaledTime - lastLoginAttemptTime < loginCooldown)
        {
            ShowError("Please wait a moment before trying again!");
            return;
        }

        string input = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(input))
        {
            ShowError("Please enter your Account or Email!");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter your password!");
            return;
        }

        lastLoginAttemptTime = Time.unscaledTime;
        isLoggingIn = true;
        isAutoLoginAttempt = false;
        lastLoginInputTrimmed = input;

        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            loadingCanvas.transform.SetAsLastSibling();
        }

        if (input.Contains("@"))
        {
            var emailRequest = new LoginWithEmailAddressRequest
            {
                Email = input,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
            };
            PlayFabClientAPI.LoginWithEmailAddress(emailRequest, OnLoginSuccess, OnLoginError);
        }
        else
        {
            var usernameRequest = new LoginWithPlayFabRequest
            {
                Username = input,
                Password = password,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
            };
            PlayFabClientAPI.LoginWithPlayFab(usernameRequest, OnLoginSuccess, OnLoginError);
        }
    }

    private void OnLoginSuccess(LoginResult result)
    {
        bool wasAutoLogin = isAutoLoginAttempt;
        isAutoLoginAttempt = false;

        string displayName = "Player";
        if (result.InfoResultPayload != null && result.InfoResultPayload.PlayerProfile != null && !string.IsNullOrEmpty(result.InfoResultPayload.PlayerProfile.DisplayName))
        {
            displayName = result.InfoResultPayload.PlayerProfile.DisplayName;
        }
        else if (!string.IsNullOrEmpty(lastLoginInputTrimmed))
        {
            displayName = lastLoginInputTrimmed;
        }
        else
        {
            string previousName = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(previousName)) displayName = previousName;
        }

        PlayerPrefs.SetString("PlayerName", displayName);
        PlayerPrefs.SetString("PlayFabId", result.PlayFabId);

        if (!wasAutoLogin)
        {
            if (rememberMeToggle != null && rememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("SavedUsername", lastLoginInputTrimmed);
                PlayerPrefs.SetString("LastLoginID", lastLoginInputTrimmed);
                PlayerPrefs.Save();

                string token = PlayerPrefs.GetString("RememberMeToken", "");
                if (string.IsNullOrEmpty(token)) token = System.Guid.NewGuid().ToString("N");
                string tokenToLink = token;

                var linkRequest = new LinkCustomIDRequest { CustomId = tokenToLink, ForceLink = true };
                PlayFabClientAPI.LinkCustomID(linkRequest,
                    _ =>
                    {
                        if (this == null) return;
                        PlayerPrefs.SetString("RememberMeToken", tokenToLink);
                        PlayerPrefs.SetInt("RememberMe", 1);
                        PlayerPrefs.Save();
                    },
                    err =>
                    {
                        Debug.LogWarning("Failed to link remember-me token: " + err.ErrorMessage);
                        PlayerPrefs.SetInt("RememberMe", 0);
                        PlayerPrefs.Save();
                    });
            }
            else
            {
                string existingToken = PlayerPrefs.GetString("RememberMeToken", "");
                if (!string.IsNullOrEmpty(existingToken))
                {
                    PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest { CustomId = existingToken }, _ => { }, _ => { });
                }

                PlayerPrefs.DeleteKey("SavedUsername");
                PlayerPrefs.DeleteKey("RememberMeToken");
                PlayerPrefs.SetInt("RememberMe", 0);
                PlayerPrefs.SetString("LastLoginID", lastLoginInputTrimmed);
                PlayerPrefs.Save();
            }
        }

        string newSessionId = System.Guid.NewGuid().ToString(); 
        PlayerPrefs.SetString("SessionID", newSessionId);
        PlayerPrefs.Save();

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "SessionID", newSessionId } }
        };

        PlayFabClientAPI.UpdateUserData(request, 
            res => { 
                if (this == null || !gameObject.activeInHierarchy) return;
                SessionGuardian.StartGuarding(newSessionId);
                StartCoroutine(LoadMainMenuAsync()); 
            }, 
            err => { 
                if (this == null || !gameObject.activeInHierarchy) return;
                isLoggingIn = false;
                if (loadingCanvas != null) loadingCanvas.SetActive(false);
                ShowError("PlayFab system error! Cannot save session."); 
            }
        );
    }

    private IEnumerator LoadMainMenuAsync()
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            loadingCanvas.transform.SetAsLastSibling();
        }

        yield return new WaitForSecondsRealtime(0.5f);

        AsyncOperation operation = SceneManager.LoadSceneAsync("MainMenu");
        
        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private void OnLoginError(PlayFabError error)
    {
        isLoggingIn = false;
        isAutoLoginAttempt = false;
        if (loadingCanvas != null) loadingCanvas.SetActive(false);

        string errorMessage = "Login failed!";
        
        if (error.Error == PlayFabErrorCode.ConnectionError || string.IsNullOrEmpty(error.ErrorMessage)) 
        {
            errorMessage = "Cannot connect to server. Please check your internet connection!";
        }
        else if (error.Error == PlayFabErrorCode.InvalidUsernameOrPassword) 
        {
            errorMessage = "Incorrect Account or Password!";
        }
        else if (error.Error == PlayFabErrorCode.AccountNotFound) 
        {
            errorMessage = "The account does not exist!";
        }
        else 
        {
            errorMessage = error.ErrorMessage;
        }

        ShowError(errorMessage);
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    public void ShowError(string message)
    {
        if (messagesPanel != null)
        {
            messagesPanel.transform.SetAsLastSibling();
            messagesPanel.SetActive(true);

            if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);
            if (messageText != null) messageText.text = message;
            hideErrorCoroutine = StartCoroutine(AutoHideError(3f));
        }
    }

    private IEnumerator AutoHideError(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); 
        HideError();
    }

    public void HideError()
    {
        if (messagesPanel != null) messagesPanel.SetActive(false);
        if (hideErrorCoroutine != null) StopCoroutine(hideErrorCoroutine);
        hideErrorCoroutine = null;
    }
}