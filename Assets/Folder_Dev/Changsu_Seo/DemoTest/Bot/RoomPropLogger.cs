using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class RoomPropLogger : MonoBehaviourPunCallbacks
{
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!PhotonNetwork.InRoom) return;
        var room = PhotonNetwork.CurrentRoom;

        room.CustomProperties.TryGetValue("turnActor", out object t);
        room.CustomProperties.TryGetValue("shellIdx", out object si);
        room.CustomProperties.TryGetValue("shells", out object s);

        int me = PhotonNetwork.LocalPlayer.ActorNumber;
        int opp = -1;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.ActorNumber != me) { opp = p.ActorNumber; break; }

        room.CustomProperties.TryGetValue($"hp_{me}", out object hpMe);
        object hpOpp = null;
        if (opp != -1) room.CustomProperties.TryGetValue($"hp_{opp}", out hpOpp);

        Debug.Log($"[ROOM] turn={t}, shellIdx={si}, shells={s}, hp_me={hpMe}, hp_opp={hpOpp}");
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"[ROOM] Master switched ¡æ {newMasterClient.ActorNumber}");
    }
}
