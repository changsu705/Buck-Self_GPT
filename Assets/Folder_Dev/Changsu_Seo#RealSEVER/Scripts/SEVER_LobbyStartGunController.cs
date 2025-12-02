using Photon.Pun;
using UnityEngine;

public class SEVER_LobbyStartGunController : MonoBehaviourPun
{
    [SerializeField] private NetGameCore netGameCore;

    /// <summary>
    /// Called when the lobby start gun is grabbed by a player.
    /// Only the MasterClient should request to start the match.
    /// </summary>
    public void OnGrabbed()
    {
        if (!PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            return;
        }

        if (netGameCore == null)
        {
            netGameCore = FindObjectOfType<NetGameCore>();
        }

        if (netGameCore == null)
        {
            Debug.LogWarning("[SEVER_LobbyStartGunController] NetGameCore not found in the scene.");
            return;
        }

        netGameCore.photonView.RPC("RpcRequestStartMatch", RpcTarget.MasterClient);
    }
}
