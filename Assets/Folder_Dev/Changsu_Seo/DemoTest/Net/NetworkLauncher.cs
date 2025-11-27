using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Buckshot.PhotonInfra
{
    public class NetworkLauncher : MonoBehaviourPunCallbacks
    {
        [SerializeField] string gameVersion = "1.0";
        [SerializeField] string roomName = "RouletteDemoRoom";
        [SerializeField] bool autoConnect = true;

        void Start()
        {
            if (autoConnect)
                ConnectToServer();
        }

        public void ConnectToServer()
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("[Network] Connected to Master");
            PhotonNetwork.JoinOrCreateRoom(
                roomName,
                new RoomOptions { MaxPlayers = 2 },
                TypedLobby.Default
            );
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[Network] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        }
    }
}
