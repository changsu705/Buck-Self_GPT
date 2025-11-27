using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string roomName = "DefaultRoom";
    [SerializeField] private byte maxPlayers = 16;

    void Start()
    {
        // 닉네임 기본값 (원하면 UI InputField로 받아서 대체)
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "User_" + Random.Range(1000, 9999);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("[Launcher] Connecting to Photon...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Launcher] Connected. Joining Lobby...");
        // 로비 생략하고 바로 방 입장해도 됨. 여기선 간단히 바로 방 시도.
        JoinOrCreateRoom();
    }

    private void JoinOrCreateRoom()
    {
        var options = new RoomOptions { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Launcher] Joined room: {roomName}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        // 방 입장 성공 시, PlayerListManager가 자동으로 목록을 갱신하도록 설계
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Launcher] Disconnected: {cause}");
        // 간단 재접속 로직 (선택)
        PhotonNetwork.ReconnectAndRejoin();
    }
}