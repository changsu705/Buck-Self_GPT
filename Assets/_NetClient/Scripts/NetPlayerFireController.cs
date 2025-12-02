using Photon.Pun;
using UnityEngine;

/// <summary>
/// Sends fire requests to <see cref="NetGameCore"/> when the local player presses the fire button.
/// Turn/HP validation is handled by the server-side NetGameCore.
/// </summary>
public class NetPlayerFireController : MonoBehaviourPun
{
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
    [SerializeField] private NetGameCore netGameCore;

    private void Awake()
    {
        if (netGameCore == null)
        {
            netGameCore = FindObjectOfType<NetGameCore>();
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (netGameCore == null)
        {
            return;
        }

        if (Input.GetKeyDown(fireKey))
        {
            netGameCore.photonView.RPC("RpcRequestFire", RpcTarget.MasterClient);
        }
    }
}
