using Photon.Pun;
using UnityEngine;

public class NetGameCore : MonoBehaviourPun
{
    [PunRPC]
    public void RpcRequestStartMatch(PhotonMessageInfo info)
    {
        Debug.Log($"StartMatch requested by MasterClient. Sender: {info.Sender}");
    }
}
