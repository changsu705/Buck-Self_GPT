using UnityEngine;

/// <summary>
/// 'ScriptableObject'를 상속받아, 유니티 프로젝트 내에서 '데이터 애셋'으로 존재할 수 있는
/// 카드 데이터의 원본(템플릿)입니다.
/// - MonoBehaviour와 달리 씬(Scene)의 게임 오브젝트에 부착되지 않습니다.
/// - 카드 이름, 설명, 카드 앞면 머티리얼 등 '변하지 않는' 원본 정보를 저장합니다.
/// </summary>
// [CreateAssetMenu]: 유니티 에디터의 Assets > Create 메뉴에 새로운 항목을 추가합니다.
// (fileName: 새 파일 생성 시 기본 이름, menuName: 메뉴에 표시될 경로)
[CreateAssetMenu(fileName = "New Card", menuName = "Card/Create New Card")]
public class CardData : ScriptableObject
{
    [Header("카드 정보")]
    [Tooltip("카드 이름 (예: 힐링 포션)")]
    public string cardName;

    [Tooltip("카드 효과 설명 (예: HP를 1 회복합니다.)")]
    [TextArea(3, 5)] // 인스펙터에서 여러 줄로 입력 가능하도록 설정
    public string description;

    [Header("시각 정보")]
    [Tooltip("카드 앞면에 적용될 Material (CardVisual에서 이 머티리얼을 복제하여 사용)")]
    public Material cardFaceMaterial;
}