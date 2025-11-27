using UnityEngine;

/// <summary>
/// [데이터 애셋] 카드의 '원본 데이터'를 정의하는 ScriptableObject입니다.
/// 카드 이름, 설명, 외형(머티리얼), 기능(로직 프리팹) 등
/// '변하지 않는' 원본 정보를 저장합니다.
/// </summary>
// [CreateAssetMenu]: Assets > Create > Card > Create New Card 메뉴를 생성합니다.
[CreateAssetMenu(fileName = "New Card", menuName = "Card/Create New Card")]
public class CardData : ScriptableObject
{
    [Header("카드 정보")]
    [Tooltip("카드 이름 (예: 힐링 포션)")]
    public string cardName;

    [Tooltip("카드 효과 설명 (예: HP를 1 회복합니다.)")]
    [TextArea(3, 5)] // 인스펙터에서 여러 줄로 입력 가능
    public string description;

    [Header("시각 정보")]
    [Tooltip("카드 앞면에 적용될 Material (CardVisual이 이 머티리얼을 복제하여 사용)")]
    public Material cardFaceMaterial;

    [Header("기능(로직) 정보")]
    [Tooltip("이 카드를 냈을 때 실행될 '기능' 프리팹.\n" +
             "(예: TurnSkip.cs 스크립트가 붙어있는 프리팹)")]
    public GameObject cardLogicPrefab;
}