using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using JUTPS;
using JUTPS.CameraSystems;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    private List<JUCharacterController> alivePlayers = new List<JUCharacterController>();
    private int currentIndex = 0;
    private bool isSpectating = false;

    private JUCharacterController currentSpectatedPlayer;
    
    private JUCameraController localCameraController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartSpectating()
    {
        isSpectating = true;
        
        FindLocalCamera();
        
        RefreshAlivePlayers();

        if (alivePlayers.Count == 0) return; 

        SwitchTarget(0);
    }

    public void StopSpectating()
    {
        isSpectating = false;
        
        if (currentSpectatedPlayer != null)
        {
            if (localCameraController != null)
            {
                GameObject localPlayer = GetLocalPlayer();
                if (localPlayer != null)
                {
                    localCameraController.TargetToFollow = localPlayer.transform; 
                }
            }
            currentSpectatedPlayer = null;
        }
    }

    public JUCharacterController GetCurrentSpectatedPlayer()
    {
        return currentSpectatedPlayer;
    }

    private void Update()
    {
        if (!isSpectating || alivePlayers.Count == 0) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) SwitchTarget(1);
        else if (Mouse.current.rightButton.wasPressedThisFrame) SwitchTarget(-1);
    }

    private void RefreshAlivePlayers()
    {
        alivePlayers.Clear();
        JUCharacterController[] players = FindObjectsByType<JUCharacterController>(FindObjectsSortMode.None);

        string myTeam = "";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        foreach (var p in players)
        {
            var health = p.GetComponent<JUHealth>();
            var pv = p.GetComponent<PhotonView>();
            
            if (health != null && !health.IsDead && pv != null && !pv.IsMine)
            {
                string theirTeam = "";
                if (pv.Owner != null && pv.Owner.CustomProperties.ContainsKey("Team"))
                {
                    theirTeam = (string)pv.Owner.CustomProperties["Team"];
                }

                if (!string.IsNullOrEmpty(myTeam) && theirTeam == myTeam)
                {
                    alivePlayers.Add(p);
                }
            }
        }
    }

    private void SwitchTarget(int direction)
    {
        RefreshAlivePlayers();

        if (alivePlayers.Count == 0) return;

        currentIndex += direction;
        if (currentIndex >= alivePlayers.Count) currentIndex = 0;
        if (currentIndex < 0) currentIndex = alivePlayers.Count - 1;
    }

    private void LateUpdate()
    {
        if (!isSpectating || alivePlayers.Count == 0) return;

        if (currentIndex < 0 || currentIndex >= alivePlayers.Count)
        {
            SwitchTarget(1);
            return;
        }

        JUCharacterController target = alivePlayers[currentIndex];

        if (target == null)
        {
            SwitchTarget(1);
            return;
        }

        var health = target.GetComponent<JUHealth>();

        if (health == null || health.IsDead)
        {
            SwitchTarget(1);
            return;
        }

        if (currentSpectatedPlayer != target)
        {
            currentSpectatedPlayer = target;
            
            if (localCameraController != null)
            {
                localCameraController.TargetToFollow = target.HumanoidSpine != null ? target.HumanoidSpine : target.transform;
            }
        }
    }
    
    private void FindLocalCamera()
    {
        if (localCameraController != null) return;
        
        GameObject localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localCameraController = localPlayer.GetComponentInChildren<JUCameraController>(true);
        }
    }
    
    private GameObject GetLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine) return p;
        }
        return null;
    }
}