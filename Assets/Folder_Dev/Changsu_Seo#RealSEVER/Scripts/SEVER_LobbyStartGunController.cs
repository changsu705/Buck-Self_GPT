using Photon.Pun;
using UnityEngine;

public class SEVER_LobbyStartGunController : MonoBehaviourPun
{
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

        photonView.RPC("RpcRequestStartMatch", RpcTarget.MasterClient);
    }
}
