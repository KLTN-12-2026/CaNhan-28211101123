using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class KillFeedManager : MonoBehaviourPun
{
    public static KillFeedManager Instance;

    [Header("UI References")]
    public GameObject killFeedItemPrefab;
    public Transform killFeedContainer;

    [System.Serializable]
    public struct WeaponIconMapping
    {
        public string weaponName;
        public Sprite weaponSprite;
    }

    [Header("Weapon Database")]
    public WeaponIconMapping[] weaponIcons;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void BroadcastKill(string killerName, string victimName, string weaponName)
    {
        if (photonView != null)
        {
            photonView.RPC(nameof(RPC_ShowKillFeed), RpcTarget.All, killerName, victimName, weaponName);
        }
    }

    [PunRPC]
    private void RPC_ShowKillFeed(string killerName, string victimName, string weaponName)
    {
        if (killFeedItemPrefab == null || killFeedContainer == null) return;

        GameObject go = Instantiate(killFeedItemPrefab, killFeedContainer);
        
        go.transform.SetAsLastSibling();

        if (go.TryGetComponent(out KillFeedItem item))
        {
            Sprite icon = GetWeaponSprite(weaponName);
            item.Setup(killerName, victimName, icon);
        }
    }

    private Sprite GetWeaponSprite(string wName)
    {
        if (string.IsNullOrEmpty(wName)) return null;

        foreach (var w in weaponIcons)
        {
            if (wName.ToLower().Contains(w.weaponName.ToLower()) || w.weaponName.ToLower().Contains(wName.ToLower())) 
            {
                return w.weaponSprite;
            }
        }
        return null;
    }
}