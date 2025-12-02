using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class NetGameCore : MonoBehaviourPunCallbacks
{
    private class PlayerState
    {
        public int actorNumber;
        public int hp;
        public GameObject rig;
    }

    private readonly Dictionary<int, PlayerState> players = new();

    private int[] shellPattern;
    private int currentShellIndex;
    private int currentTurnActorNumber;

    private enum MatchState
    {
        Lobby,
        InGame,
        GameOver
    }

    private MatchState state = MatchState.Lobby;
    private const int DefaultStartingHp = 3;

    public void RegisterPlayer(int actorNumber, GameObject rig)
    {
        if (rig == null)
        {
            Debug.LogWarning("[NetGameCore] Cannot register player with a null rig reference.");
            return;
        }

        players[actorNumber] = new PlayerState
        {
            actorNumber = actorNumber,
            hp = DefaultStartingHp,
            rig = rig
        };

        Debug.Log($"[NetGameCore] Registered player {actorNumber} with starting HP {DefaultStartingHp}.");
    }

    public void StartMatch()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        state = MatchState.InGame;
        Debug.Log("[NetGameCore] Match started");
    }

    [PunRPC]
    public void RpcRequestStartMatch(PhotonMessageInfo info)
    {
        Debug.Log($"[NetGameCore] StartMatch requested. Sender: {info.Sender}.");
        StartMatch();
    }
}
