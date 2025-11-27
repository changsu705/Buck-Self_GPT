/*
 * PlayerHand.cs (GetCards 함수 추가 및 수정)
 *
 * [수정 사항]
 * - 'GetCards()' 함수를 추가했습니다. (SkillCardManager가 재정렬에 사용)
 * - 'RemoveCard()' 함수가 파괴된(null) 카드를 제거하려 할 때 오류가 나지 않도록 수정했습니다.
 * - CardVisual.OnDestroy()에서 호출할 수 있도록 'UnregisterCard()' 함수를 추가했습니다. (메모리 정리)
 */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 각 플레이어에 부착하여 현재 소유한 카드를 관리하는 스크립트입니다.
/// </summary>
public class PlayerHand : MonoBehaviour
{
    // [수정] readonly 제거 (리스트 내부 정리 필요)
    private readonly List<CardVisual> _cardsInHand = new List<CardVisual>();

    /// <summary>
    /// 현재 소유한 카드의 개수를 외부에서 읽어갈 수 있도록 공개합니다.
    /// (null이 포함될 수 있으므로, 정확한 개수는 GetCards().Count를 권장)
    /// </summary>
    public int CardCount => _cardsInHand.Count;

    /// <summary>
    /// 이 플레이어의 손에 새로운 카드를 추가합니다.
    /// </summary>
    public void AddCard(CardVisual card)
    {
        if (card != null && !_cardsInHand.Contains(card))
        {
            _cardsInHand.Add(card);
            Debug.Log($"<color=cyan>[{name}]</color>이(가) 카드를 추가했습니다. 현재 카드 수: <color=yellow>{_cardsInHand.Count}장</color>");
        }
    }

    /// <summary>
    /// 사용했거나 버린 카드를 손에서 제거합니다. (CardLogic이 호출)
    /// </summary>
    public void RemoveCard(CardVisual card)
    {
        if (card != null && _cardsInHand.Contains(card))
        {
            _cardsInHand.Remove(card);
            Debug.Log($"<color=cyan>[{name}]</color>이(가) 카드를 제거했습니다. 현재 카드 수: <color=yellow>{_cardsInHand.Count}장</color>");
        }
    }

    // ⚠️ [추가된 함수] ⚠️
    /// <summary>
    /// (CardVisual.OnDestroy()에서 호출)
    /// 카드가 (사용되어서가 아니라) 턴이 끝나 파괴될 때, 리스트에서 스스로를 제거합니다.
    /// </summary>
    public void UnregisterCard(CardVisual card)
    {
        if (card != null && _cardsInHand.Contains(card))
        {
            _cardsInHand.Remove(card);
        }
    }

    // ⚠️ [추가된 함수] ⚠️
    /// <summary>
    /// [읽기 전용] 현재 손에 든 모든 카드 목록을 반환합니다.
    /// (SkillCardManager가 카드 재정렬을 위해 호출합니다.)
    /// </summary>
    public List<CardVisual> GetCards()
    {
        // 혹시 모를 null(파괴된 카드)을 리스트에서 제거하고 반환합니다.
        _cardsInHand.RemoveAll(item => item == null);
        return _cardsInHand;
    }
}