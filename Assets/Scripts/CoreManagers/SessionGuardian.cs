using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;

public class SessionGuardian : MonoBehaviour
{
    public static SessionGuardian Instance;

    [Header("Settings")]
    public float checkInterval = 15f;
    public string loginSceneName = "Login Scene";

    private string localSessionId;
    private bool isChecking = false;
    private Coroutine checkRoutine;

    public static void StartGuarding(string sessionId, float interval = 15f, string loginScene = "Login Scene")
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("SessionGuardian");
            Instance = go.AddComponent<SessionGuardian>();
            DontDestroyOnLoad(go);
        }

        Instance.checkInterval = interval;
        Instance.loginSceneName = loginScene;
        Instance.localSessionId = sessionId;
        Instance.Begin();
    }

    public static void StopGuarding()
    {
        if (Instance == null) return;

        if (Instance.checkRoutine != null) Instance.StopCoroutine(Instance.checkRoutine);
        Destroy(Instance.gameObject);
        Instance = null;
    }

    private void Begin()
    {
        if (checkRoutine != null) StopCoroutine(checkRoutine);
        checkRoutine = StartCoroutine(CheckLoop());
    }

    private IEnumerator CheckLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(checkInterval);

            if (isChecking) continue;
            isChecking = true;

            var request = new GetUserDataRequest { Keys = new List<string> { "SessionID" } };
            PlayFabClientAPI.GetUserData(request, OnGetUserDataSuccess, OnGetUserDataError);
        }
    }

    private void OnGetUserDataSuccess(GetUserDataResult result)
    {
        if (this == null || Instance != this) return;
        isChecking = false;

        if (result.Data != null && result.Data.ContainsKey("SessionID"))
        {
            string serverSessionId = result.Data["SessionID"].Value;

            if (!string.IsNullOrEmpty(serverSessionId) && serverSessionId != localSessionId)
            {
                ForceLogout("This account was logged in on another device!");
            }
        }
    }

    private void OnGetUserDataError(PlayFabError error)
    {
        if (this == null || Instance != this) return;
        isChecking = false;

        switch (error.Error)
        {
            case PlayFabErrorCode.ConnectionError:
            case PlayFabErrorCode.ServiceUnavailable:
            case PlayFabErrorCode.InternalServerError:
            case PlayFabErrorCode.APIClientRequestRateLimitExceeded:
                break;

            case PlayFabErrorCode.NotAuthenticated:
            case PlayFabErrorCode.InvalidSessionTicket:
            case PlayFabErrorCode.AccountBanned:
                ForceLogout("Your session has expired. Please log in again.");
                break;

            default:
                Debug.LogWarning("SessionGuardian: unhandled GetUserData error: " + error.Error);
                break;
        }
    }

    private void ForceLogout(string reason)
    {
        if (checkRoutine != null) StopCoroutine(checkRoutine);

        PlayerPrefs.SetString("DisconnectReason", reason);
        PlayerPrefs.DeleteKey("SessionID");

        string rememberMeToken = PlayerPrefs.GetString("RememberMeToken", "");
        PlayerPrefs.DeleteKey("RememberMeToken");
        PlayerPrefs.DeleteKey("SavedUsername");
        PlayerPrefs.SetInt("RememberMe", 0);

        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(rememberMeToken))
        {
            PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest { CustomId = rememberMeToken }, _ => { }, _ => { });
        }

        PlayFabClientAPI.ForgetAllCredentials();

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        Instance = null;

#pragma warning disable CS0618
        if (Application.CanStreamedLevelBeLoaded(loginSceneName))
#pragma warning restore CS0618
        {
            SceneManager.LoadScene(loginSceneName);
        }
        else
        {
            Debug.LogError($"SessionGuardian: scene '{loginSceneName}' is not in Build Settings - cannot return to login. Check the loginSceneName field/build settings.");
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}