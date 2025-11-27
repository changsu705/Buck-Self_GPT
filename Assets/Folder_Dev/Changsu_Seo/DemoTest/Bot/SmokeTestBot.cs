using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using Buckshot.App;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class SmokeTestBot : MonoBehaviourPunCallbacks
{
    public enum ShootMode { AlternateSelfOpp, SelfOnly, OppOnly, RandomTarget }

    [Header("Bot Settings")]
    public ShootMode mode = ShootMode.AlternateSelfOpp;
    public float shotInterval = 0.8f;   // 발사 간 간격(초)
    public int maxShotsPerRound = 20;   // 라운드당 최대 시도 수(안전장치)

    [Header("Refs")]
    public PhotonGameCoordinator coordinator; // 씬의 Coordinator를 드래그 연결

    // 내부 상태
    int _me;
    int _opp;
    int _lastTurn = -1;
    int _lastShellIdx = -1;
    int _lastHpMe = -1, _lastHpOpp = -1;
    int _roundCounter = 0;
    int _localShotCounter = 0;
    bool _running;

    // 방 입장 시 자동 시작
    public override void OnJoinedRoom()
    {
        _me = PhotonNetwork.LocalPlayer.ActorNumber;
        _opp = FindOpponentActor();

        if (coordinator == null)
        {
            Debug.LogError("[SMOKE] Coordinator is NULL. Inspector에서 PhotonGameCoordinator를 연결해야 한다.");
            return;
        }

        Debug.Log($"[SMOKE] Bot OnJoinedRoom — me={_me}, opp={_opp}, isMaster={PhotonNetwork.IsMasterClient}");
        SnapshotAndLog("[SMOKE] Initial");

        _running = true;
        StartCoroutine(BotLoop());
    }

    // 상대가 뒤늦게 들어온 경우 opp를 갱신
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        _opp = FindOpponentActor();
        Debug.Log($"[SMOKE] Opponent joined. opp={_opp}");
    }

    public override void OnRoomPropertiesUpdate(PhotonHashtable changedProps)
    {
        SnapshotAndLog("[SMOKE] OnRoomPropertiesUpdate");
    }

    int FindOpponentActor()
    {
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return p.ActorNumber;
        return -1;
    }

    IEnumerator BotLoop()
    {
        // 라운드 시작 여유
        yield return new WaitForSeconds(0.5f);

        while (_running)
        {
            if (!PhotonNetwork.InRoom) yield break;
            var room = PhotonNetwork.CurrentRoom;
            if (room == null) { yield return null; continue; }

            // 상대가 아직 없으면 대기
            if (_opp == -1) { yield return null; continue; }

            // 턴/HP/인덱스 읽기
            room.CustomProperties.TryGetValue("turnActor", out object tObj);
            room.CustomProperties.TryGetValue("shellIdx", out object siObj);

            int turn = tObj != null ? (int)tObj : -1;
            int shellIdx = siObj != null ? (int)siObj : -1;
            int hpMe = room.CustomProperties.TryGetValue("hp_" + _me, out object h1) ? (int)h1 : -1;
            int hpOpp = room.CustomProperties.TryGetValue("hp_" + _opp, out object h2) ? (int)h2 : -1;

            // 라운드 새로 시작 감지
            if (shellIdx == 0 && _lastShellIdx > shellIdx)
            {
                _roundCounter++;
                _localShotCounter = 0;
                Debug.Log($"[SMOKE] ===== New Round #{_roundCounter} =====");
            }

            // 내 턴이면 발사
            if (turn == _me)
            {
                bool shootOpp = true;
                switch (mode)
                {
                    case ShootMode.SelfOnly: shootOpp = false; break;
                    case ShootMode.OppOnly: shootOpp = true; break;
                    case ShootMode.RandomTarget: shootOpp = (Random.value > 0.5f); break;
                    case ShootMode.AlternateSelfOpp: shootOpp = (_localShotCounter % 2 == 0); break;
                }

                if (_localShotCounter >= maxShotsPerRound)
                {
                    Debug.LogWarning("[SMOKE] Max shots per round reached. Waiting for next round.");
                }
                else
                {
                    if (shootOpp)
                    {
                        Debug.Log($"[SMOKE] TryShootOpponent() — turn={turn}, shellIdx={shellIdx}, hpMe={hpMe}, hpOpp={hpOpp}");
                        coordinator.TryShootOpponent();
                    }
                    else
                    {
                        Debug.Log($"[SMOKE] TryShootSelf() — turn={turn}, shellIdx={shellIdx}, hpMe={hpMe}, hpOpp={hpOpp}");
                        coordinator.TryShootSelf();
                    }
                    _localShotCounter++;
                }

                yield return new WaitForSeconds(shotInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    void SnapshotAndLog(string prefix)
    {
        if (!PhotonNetwork.InRoom) return;
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        int turn = room.CustomProperties.TryGetValue("turnActor", out object t) ? (int)t : -1;
        int shellIdx = room.CustomProperties.TryGetValue("shellIdx", out object si) ? (int)si : -1;
        string shells = room.CustomProperties.TryGetValue("shells", out object s) ? (string)s : "?";
        int hpMe = room.CustomProperties.TryGetValue("hp_" + _me, out object h1) ? (int)h1 : -1;
        int hpOpp = (_opp != -1 && room.CustomProperties.TryGetValue("hp_" + _opp, out object h2)) ? (int)h2 : -1;

        if (_lastTurn != -1 && turn != _lastTurn)
            Debug.Log($"[SMOKE][TURN] {_lastTurn} → {turn}");
        if (_lastShellIdx != -1 && shellIdx > _lastShellIdx)
            Debug.Log($"[SMOKE][SHELL] idx {_lastShellIdx} → {shellIdx}");
        if (_lastHpMe != -1 && hpMe != _lastHpMe)
            Debug.Log($"[SMOKE][HP] me: {_lastHpMe} → {hpMe}");
        if (_lastHpOpp != -1 && hpOpp != _lastHpOpp)
            Debug.Log($"[SMOKE][HP] opp: {_lastHpOpp} → {hpOpp}");

        Debug.Log($"{prefix} :: turn={turn}, shellIdx={shellIdx}, shells={shells}, hp_me={hpMe}, hp_opp={hpOpp}");

        _lastTurn = turn;
        _lastShellIdx = shellIdx;
        _lastHpMe = hpMe;
        _lastHpOpp = hpOpp;
    }
}
