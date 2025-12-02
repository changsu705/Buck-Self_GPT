using Photon.Pun;
using UnityEngine;

public class NetPlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform[] spawnPoints;
    private bool hasSpawned;

    public override void OnJoinedRoom()
    {
        SpawnLocalPlayer();
    }

    public void SpawnLocalPlayer()
    {
        if (hasSpawned)
        {
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[NetPlayerSpawner] No spawn points assigned for player spawn.");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("[NetPlayerSpawner] PhotonNetwork is not ready for spawning.");
            return;
        }

        var index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        var spawnPoint = spawnPoints[index];

        PhotonNetwork.Instantiate("SEVER_LobbyPlayerRig", spawnPoint.position, spawnPoint.rotation);
        hasSpawned = true;
    }
}
