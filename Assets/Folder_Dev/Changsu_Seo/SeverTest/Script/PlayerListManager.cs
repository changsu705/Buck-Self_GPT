using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerListManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private RectTransform content;   // Scroll View/Viewport/Content
    [SerializeField] private GameObject itemPrefab;   // PlayerListItem 프리팹(루트 Active)

    private readonly Dictionary<int, GameObject> rows = new();

    void Start()
    {
        if (!content) Debug.LogError("[PLM] content NULL");
        if (!itemPrefab) Debug.LogError("[PLM] itemPrefab NULL");

        // UI 파이프 확인용: 포톤과 무관하게 한 줄 강제 생성
        var test = Instantiate(itemPrefab, content);
        test.name = "TEST_ROW";
        test.SetActive(true);
        test.transform.localScale = Vector3.one;
        SetText(test, "TEST ROW");

        if (PhotonNetwork.InRoom) BuildAll();
    }

    public override void OnJoinedRoom() => BuildAll();
    public override void OnPlayerEnteredRoom(Player p) => Upsert(p);
    public override void OnPlayerLeftRoom(Player p) => Remove(p);
    public override void OnPlayerPropertiesUpdate(Player p, ExitGames.Client.Photon.Hashtable _) => Upsert(p);

    private void BuildAll()
    {
        foreach (Transform c in content)
            if (c.name != "TEST_ROW") Destroy(c.gameObject);

        rows.Clear();
        foreach (var p in PhotonNetwork.PlayerList) Upsert(p);
    }

    private void Upsert(Player p)
    {
        if (!content || !itemPrefab) return;

        int key = p.ActorNumber;
        if (!rows.TryGetValue(key, out var go) || !go)
        {
            go = Instantiate(itemPrefab, content);
            go.name = $"Player_{key}";
            go.SetActive(true);
            go.transform.localScale = Vector3.one;
            rows[key] = go;
        }

        string nickname = string.IsNullOrEmpty(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName;
        int ping = PhotonNetwork.GetPing();
        string line = (p.IsLocal ? "● " : "• ") + nickname + $" | {ping} ms";

        var comp = go.GetComponent<PlayerListItem>();
        if (comp) comp.SetInfo(nickname, ping, p.IsLocal);
        else SetText(go, line);
    }

    private void Remove(Player p)
    {
        int key = p.ActorNumber;
        if (rows.TryGetValue(key, out var go) && go) Destroy(go);
        rows.Remove(key);
    }

    private static void SetText(GameObject go, string text)
    {
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp) tmp.text = text;
        else { var ui = go.GetComponentInChildren<Text>(true); if (ui) ui.text = text; }
    }
}
