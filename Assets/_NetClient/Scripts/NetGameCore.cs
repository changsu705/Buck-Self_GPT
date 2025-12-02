using System.Collections.Generic;
using System.Linq;
using Buckshot.Contracts;
using Buckshot.Core;
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
    private const int DefaultLiveCount = 2;
    private const int DefaultBlankCount = 4;

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
        SetupNewRound();
        Debug.Log("[NetGameCore] Match started");
    }

    private void SetupNewRound()
    {
        int seed = Random.Range(0, int.MaxValue);

        var builder = new DefaultShellDeckBuilder();
        shellPattern = builder.Build(seed, DefaultLiveCount, DefaultBlankCount)
            .Select(s => s == ShellType.Live ? 1 : 0)
            .ToArray();

        currentShellIndex = 0;

        var turnPolicy = new RandomFirstTurnPolicy();
        var actors = players.Keys.OrderBy(x => x).ToList();
        if (actors.Count > 0)
        {
            currentTurnActorNumber = turnPolicy.PickFirstActor(actors, seed);
        }

        Debug.Log($"[NetGameCore] Round setup complete. Seed={seed}, Pattern=[{string.Join(",", shellPattern ?? new int[0])}], FirstTurn={currentTurnActorNumber}");
    }

    [PunRPC]
    public void RpcRequestStartMatch(PhotonMessageInfo info)
    {
        Debug.Log($"[NetGameCore] StartMatch requested. Sender: {info.Sender}.");
        StartMatch();
    }

    [PunRPC]
    public void RpcRequestFire(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (state != MatchState.InGame)
        {
            return;
        }

        int shooterActor = info.Sender.ActorNumber;
        if (shooterActor != currentTurnActorNumber)
        {
            Debug.LogWarning("[NetGameCore] Fire request ignored: not shooter's turn.");
            return;
        }

        if (shellPattern == null || currentShellIndex >= shellPattern.Length)
        {
            Debug.LogWarning("[NetGameCore] Fire request ignored: no shells left.");
            return;
        }

        int targetActor = DecideTarget(shooterActor);
        bool isLive = shellPattern[currentShellIndex] == 1;

        currentShellIndex++;

        int newHp = players.TryGetValue(targetActor, out var targetState) ? targetState.hp : 0;

        if (isLive && targetState != null)
        {
            newHp = Mathf.Max(0, targetState.hp - 1);
            targetState.hp = newHp;
        }

        int nextTurn = isLive ? GetOpponent(shooterActor) : shooterActor;

        currentTurnActorNumber = nextTurn;

        bool someoneDead = isLive && newHp <= 0;

        bool liveEmpty = true;
        for (int i = currentShellIndex; i < (shellPattern?.Length ?? 0); i++)
        {
            if (shellPattern[i] == 1)
            {
                liveEmpty = false;
                break;
            }
        }

        photonView.RPC(
            nameof(RpcShotResult),
            RpcTarget.All,
            shooterActor,
            targetActor,
            isLive,
            newHp,
            currentShellIndex,
            nextTurn);

        if (someoneDead || liveEmpty)
        {
            state = MatchState.GameOver;
        }
    }

    private int DecideTarget(int shooterActor)
    {
        foreach (var actor in players.Keys)
        {
            if (actor != shooterActor)
            {
                return actor;
            }
        }

        return shooterActor;
    }

    private int GetOpponent(int shooterActor)
    {
        foreach (var actor in players.Keys)
        {
            if (actor != shooterActor)
            {
                return actor;
            }
        }

        return shooterActor;
    }

    [PunRPC]
    public void RpcShotResult(int shooterActor, int targetActor, bool isLive, int newHp, int newShellIndex, int nextTurnActorNumber)
    {
        currentShellIndex = newShellIndex;
        currentTurnActorNumber = nextTurnActorNumber;

        if (players.TryGetValue(targetActor, out var targetState))
        {
            targetState.hp = newHp;

            var health = targetState.rig != null ? targetState.rig.GetComponent<PlayerHealth>() : null;
            if (health != null)
            {
                health.ApplyNetworkedDamage(newHp);
            }
        }

        Debug.Log($"[NetGameCore] ShotResult: shooter={shooterActor}, target={targetActor}, live={isLive}, hp={newHp}, shellIndex={newShellIndex}, nextTurn={nextTurnActorNumber}");

        // TODO: Hook up VFX/SFX/UI updates here (HP bars, turn indicator, hit effects).
    }
}
