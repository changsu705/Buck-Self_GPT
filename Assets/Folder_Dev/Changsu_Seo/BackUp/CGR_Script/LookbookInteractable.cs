using UnityEngine;

/// <summary>
/// 룩북(책) 오브젝트에 부착하여 식별하는 '마커(Marker)' 스크립트입니다.
/// - 이 스크립트 자체는 아무 기능도 하지 않습니다.
/// - RevolverTurnPossession이 십자선(CrosshairRaycaster)으로 쳐다본 오브젝트에서
///   이 컴포넌트(LookbookInteractable)가 있는지 'GetComponent'로 확인하여,
///   이게 붙어있으면 "아, 룩북을 클릭했구나"라고 판단하고 UI 패널을 엽니다.
/// </summary>
public class LookbookInteractable : MonoBehaviour
{
    // 마커 스크립트이므로 내부 로직(Start, Update 등)이 전혀 필요 없습니다.
    // (InteractableHighlighter는 하이라이트 기능,
    //  LookbookInteractable은 클릭 시 기능 구분을 위해 존재합니다.)
}