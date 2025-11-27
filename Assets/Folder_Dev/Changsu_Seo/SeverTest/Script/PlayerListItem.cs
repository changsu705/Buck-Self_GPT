using TMPro; // TextMeshPro를 쓰지 않으면 UnityEngine.UI.Text로 변경
using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text label; // 또는 public Text label;

    // 강조 등 스타일 바꾸고 싶다면 isLocal로 처리
    public void SetInfo(string nickname, int ping, bool isLocal)
    {
        if (label == null) return;
        label.text = isLocal ? $"● {nickname} (me)  |  {ping} ms"
                             : $"• {nickname}       |  {ping} ms";
    }
}