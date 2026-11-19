using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;

public class RegisterManager : MonoBehaviour
{
    public static RegisterManager Instance;

    [Header("Register UI References")]
    public TMP_InputField regIngamenameInput;
    public TMP_InputField regUsernameInput;
    public TMP_InputField regEmailInput;
    public TMP_InputField regPasswordInput;
    public TMP_InputField regConfirmInput;

    private bool isRegistering = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ResetRegisterInputs()
    {
        isRegistering = false;

        if (regIngamenameInput != null) regIngamenameInput.text = "";
        if (regUsernameInput != null) regUsernameInput.text = "";
        if (regEmailInput != null) regEmailInput.text = "";
        if (regPasswordInput != null) regPasswordInput.text = "";
        if (regConfirmInput != null) regConfirmInput.text = "";
    }

    public void OnRegisterButtonClicked()
    {
        if (isRegistering) return;

        if (LoginManager.Instance == null)
        {
            Debug.LogError("RegisterManager: LoginManager.Instance is null, cannot show errors or proceed.");
            return;
        }

        string displayName = regIngamenameInput != null ? regIngamenameInput.text.Trim() : "";
        string idName = regUsernameInput != null ? regUsernameInput.text.Trim() : "";
        string email = regEmailInput != null ? regEmailInput.text.Trim() : "";
        string pass = regPasswordInput != null ? regPasswordInput.text : "";
        string confirmPass = regConfirmInput != null ? regConfirmInput.text : "";

        if (string.IsNullOrEmpty(displayName))
        {
            LoginManager.Instance.ShowError("Please enter your ingame name!");
            return;
        }
        if (displayName.Length < 3 || displayName.Length > 20)
        {
            LoginManager.Instance.ShowError("Ingame Name must be between 3 and 20 characters long!");
            return;
        }

        if (string.IsNullOrEmpty(idName))
        {
            LoginManager.Instance.ShowError("Please enter your Username!");
            return;
        }
        if (idName.Length < 3 || idName.Length > 16)
        {
            LoginManager.Instance.ShowError("Username must be between 3 and 16 characters!");
            return;
        }
        if (!Regex.IsMatch(idName, "^[a-zA-Z0-9_]+$"))
        {
            LoginManager.Instance.ShowError("Username must not contain special characters or spaces!");
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            LoginManager.Instance.ShowError("Please enter your email address!");
            return;
        }
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            LoginManager.Instance.ShowError("Invalid email format!");
            return;
        }

        if (string.IsNullOrEmpty(pass))
        {
            LoginManager.Instance.ShowError("Please enter your password!");
            return;
        }
        if (pass.Length < 8)
        {
            LoginManager.Instance.ShowError("Passwords must be at least 8 characters long!");
            return;
        }
        if (!Regex.IsMatch(pass, "[a-zA-Z]") || !Regex.IsMatch(pass, "[0-9]"))
        {
            LoginManager.Instance.ShowError("Passwords must contain at least one letter and one digit!");
            return;
        }

        if (pass != confirmPass)
        {
            LoginManager.Instance.ShowError("The verification password doesn't match!");
            return;
        }

        isRegistering = true;
        if (LoginManager.Instance.loadingCanvas != null)
        {
            LoginManager.Instance.loadingCanvas.SetActive(true);
            LoginManager.Instance.loadingCanvas.transform.SetAsLastSibling();
        }

        var request = new RegisterPlayFabUserRequest
        {
            Username = idName,
            Email = email,
            Password = pass,
            DisplayName = displayName
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnRegisterError);
    }

    private void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        isRegistering = false;
        if (this == null || !gameObject.activeInHierarchy) return;

        if (LoginManager.Instance != null)
        {
            if (LoginManager.Instance.loadingCanvas != null) LoginManager.Instance.loadingCanvas.SetActive(false);

            LoginManager.Instance.ShowLoginPanel(true);
            
            if (LoginManager.Instance.usernameInput != null && regUsernameInput != null)
            {
                LoginManager.Instance.usernameInput.text = regUsernameInput.text; 
            }
            
            LoginManager.Instance.ShowError("Account created successfully! Please log in.");
        }

        if (regPasswordInput != null) regPasswordInput.text = "";
        if (regConfirmInput != null) regConfirmInput.text = "";
    }

    private void OnRegisterError(PlayFabError error)
    {
        isRegistering = false;
        if (this == null || !gameObject.activeInHierarchy) return;

        if (LoginManager.Instance != null && LoginManager.Instance.loadingCanvas != null)
        {
            LoginManager.Instance.loadingCanvas.SetActive(false);
        }

        string errorMessage = "Registration error!";
        
        if (error.Error == PlayFabErrorCode.ConnectionError || string.IsNullOrEmpty(error.ErrorMessage)) 
        {
            errorMessage = "Cannot connect to server. Please check your internet connection!";
        }
        else if (error.Error == PlayFabErrorCode.UsernameNotAvailable) 
        {
            errorMessage = "This Username is already in use!";
        }
        else if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable) 
        {
            errorMessage = "This email address has already been registered!";
        }
        else if (error.Error == PlayFabErrorCode.InvalidEmailAddress) 
        {
            errorMessage = "Invalid email address!";
        }
        else 
        {
            errorMessage = error.ErrorMessage; 
        }

        if (LoginManager.Instance != null)
        {
            LoginManager.Instance.ShowError(errorMessage);
        }
    }
}