using UnityEngine;
using Photon.Pun;

public class MiniMapTargetSetup : MonoBehaviourPun
{
    void Start()
    {
        if (photonView.IsMine)
        {
            if (bl_MiniMap.ActiveMiniMap != null)
            {
                bl_MiniMap.ActiveMiniMap.SetTarget(this.gameObject);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy bl_MiniMap nào đang hoạt động trong Scene!");
            }
        }
    }
}