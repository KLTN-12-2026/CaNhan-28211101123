using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.Realtime;

public class PlayerTeamHighlight : MonoBehaviourPunCallbacks
{
    [Header("Highlight Materials")]
    public Material allyMaterial;
    public Material enemyMaterial;

    [Header("Renderers")]
    public Renderer[] playerRenderers;

    private void Start()
    {
        if (photonView.IsMine) return;

        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            playerRenderers = GetComponentsInChildren<Renderer>(true);
        }

        StartCoroutine(WaitForTeamDataAndHighlight());
    }

    private IEnumerator WaitForTeamDataAndHighlight()
    {
        while (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team") || 
               !photonView.Owner.CustomProperties.ContainsKey("Team"))
        {
            yield return null;
        }

        string myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        string targetTeam = (string)photonView.Owner.CustomProperties["Team"];

        Material matToApply = (myTeam == targetTeam) ? allyMaterial : enemyMaterial;

        if (matToApply != null)
        {
            foreach (Renderer rend in playerRenderers)
            {
                if (rend is ParticleSystemRenderer) continue;

                CreateOverlayRenderer(rend, matToApply);
            }
        }
    }

    private void CreateOverlayRenderer(Renderer originalRend, Material overlayMat)
    {
        GameObject overlayObj = new GameObject("Overlay_" + originalRend.gameObject.name);
        overlayObj.transform.SetParent(originalRend.transform, false);
        overlayObj.transform.localPosition = Vector3.zero;
        overlayObj.transform.localRotation = Quaternion.identity;
        overlayObj.transform.localScale = Vector3.one;

        if (originalRend is SkinnedMeshRenderer smr)
        {
            SkinnedMeshRenderer overlaySmr = overlayObj.AddComponent<SkinnedMeshRenderer>();
            
            overlaySmr.sharedMesh = smr.sharedMesh;
            overlaySmr.rootBone = smr.rootBone;
            overlaySmr.bones = smr.bones;
            
            Material[] overlayMats = new Material[smr.sharedMaterials.Length];
            for (int i = 0; i < overlayMats.Length; i++) 
            {
                overlayMats[i] = overlayMat;
            }
            overlaySmr.materials = overlayMats;
        }
        
        else if (originalRend is MeshRenderer mr)
        {
            MeshFilter mf = originalRend.GetComponent<MeshFilter>();
            if (mf != null)
            {
                MeshFilter overlayMf = overlayObj.AddComponent<MeshFilter>();
                overlayMf.sharedMesh = mf.sharedMesh;

                MeshRenderer overlayMr = overlayObj.AddComponent<MeshRenderer>();
                
                Material[] overlayMats = new Material[mr.sharedMaterials.Length];
                for (int i = 0; i < overlayMats.Length; i++) 
                {
                    overlayMats[i] = overlayMat;
                }
                overlayMr.materials = overlayMats;
            }
        }
    }
}