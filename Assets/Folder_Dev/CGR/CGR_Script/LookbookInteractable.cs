using UnityEngine;

/// <summary>
/// [마커] 룩북(책) 오브젝트에 부착하는 '표식' 컴포넌트입니다.
/// 
/// - 이 스크립트 자체는 아무 기능도 하지 않습니다.
/// - 'RevolverTurnPossession'이 십자선(CrosshairRaycaster)으로 쳐다본 오브젝트에서
///   이 컴포넌트(LookbookInteractable)가 있는지 'GetComponent'로 확인하여,
///   "룩북을 클릭했음"을 판단하고 UI 패널을 여는 용도로 사용됩니다.
/// </summary>
public class LookbookInteractable : MonoBehaviour
{
    // 마커 스크립트이므로 내부 로직(Start, Update 등)이 전혀 필요 없습니다.
}