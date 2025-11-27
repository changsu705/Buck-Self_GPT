using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필수

/// <summary>
/// CardData(ScriptableObject) 목록을 '덱'으로 관리합니다.
/// - 게임 시작 시 덱을 무작위로 셔플합니다.
/// - 덱에서 카드를 뽑는(DrawCard) 기능을 제공합니다.
/// - 덱이 비면 자동으로 다시 셔플하여 채웁니다.
/// </summary>
public class CardDeck : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("게임에서 사용할 모든 종류의 CardData 애셋들을 여기에 등록합니다. (원본 목록)")]
    public List<CardData> allCardsData = new List<CardData>();

    // --- 내부 상태 ---
    // _drawPile: 실제 게임에서 사용될 '뽑을 카드 더미'입니다.
    // allCardsData(원본)는 건드리지 않고, 이 리스트를 셔플하고 사용합니다.
    private List<CardData> _drawPile = new List<CardData>();

    void Awake()
    {
        // 씬이 로드될 때(게임 시작 시) 덱을 셔플하여 뽑을 준비를 합니다.
        ShuffleAndResetDeck();
    }

    /// <summary>
    /// 'allCardsData' 목록을 기반으로 '뽑을 덱(_drawPile)'을 새로 만들고 셔플합니다.
    /// (피셔-예이츠 셔플 알고리즘 사용)
    /// </summary>
    public void ShuffleAndResetDeck()
    {
        _drawPile.Clear(); // 기존에 남아있던 카드를 모두 비웁니다.
        _drawPile.AddRange(allCardsData); // 원본(allCardsData) 목록으로 덱을 다시 채웁니다.

        // 피셔-예이츠 셔플 (Fisher-Yates Shuffle)
        // 리스트의 앞에서부터 순서대로, 자신을 포함한 뒤쪽의 임의의 카드와 자리를 바꿉니다.
        // (가장 공정하고 효율적인 셔플 알고리즘 중 하나입니다.)
        for (int i = 0; i < _drawPile.Count; i++)
        {
            CardData temp = _drawPile[i]; // 현재 카드를 임시 보관
            int randomIndex = Random.Range(i, _drawPile.Count); // i부터 끝까지 중에서 랜덤 인덱스 선택

            // i번째 카드와 randomIndex번째 카드의 자리를 맞바꿈
            _drawPile[i] = _drawPile[randomIndex];
            _drawPile[randomIndex] = temp;
        }

        Debug.Log($"[CardDeck] 덱 셔플 완료. (총 {_drawPile.Count}장)");
    }

    /// <summary>
    /// 덱 맨 위에서 카드 데이터 1장을 뽑습니다. (리스트의 0번째 요소)
    /// 덱이 비면 자동으로 셔플 후 다시 뽑습니다.
    /// </summary>
    /// <returns>뽑은 CardData, 만약 allCardsData가 비어있으면 null 반환</returns>
    public CardData DrawCard()
    {
        // 1. 덱(뽑을 카드 더미)이 비었는지 확인
        if (_drawPile.Count == 0)
        {
            // 1-1. 원본 카드(allCardsData)조차 등록되지 않은 치명적인 경우
            if (allCardsData.Count == 0)
            {
                Debug.LogError("[CardDeck] 'All Cards Data' 리스트에 CardData가 1장도 등록되지 않았습니다!");
                return null;
            }

            // 1-2. 덱을 다 쓴 정상적인 경우
            Debug.LogWarning("[CardDeck] 덱이 비어서 다시 셔플합니다.");
            ShuffleAndResetDeck(); // 덱을 다시 채우고 셔플
        }

        // 2. 덱 맨 위 카드(0번 인덱스)를 뽑습니다.
        CardData drawnCard = _drawPile[0];
        _drawPile.RemoveAt(0); // 뽑은 카드는 덱에서 제거

        return drawnCard; // 뽑은 카드 반환
    }
}