using UnityEngine;

/// <summary>
/// 3D 카드 오브젝트(프리팹)에 부착되는 스크립트입니다.
/// SkillCardManager가 카드를 생성할 때 Initialize()를 호출하여,
/// 전달받은 CardData를 기반으로 카드의 외형(머티리얼)을 설정합니다.
/// </summary>
public class CardVisual : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("카드 모델 중 '앞면'에 해당하는 MeshRenderer를 여기에 연결해야 합니다.")]
    public MeshRenderer cardFaceRenderer;

    // --- 내부 상태 ---
    private InteractableHighlighter _highlighter;  // 이 카드의 하이라이터 스크립트 (캐시)

    // _myMaterialInstance: 이 카드 '전용'으로 복제된 머티리얼 인스턴스입니다.
    // (CardData의 원본 머티리얼을 직접 사용하면, 모든 카드의 색이 동시에 변하는 문제가 생기므로
    // 반드시 Instantiate로 복제하여 사용해야 합니다.)
    private Material _myMaterialInstance;

    private CardData _cardData;                   // 이 카드가 가지고 있는 원본 데이터 (참조)

    /// <summary>
    /// [읽기 전용] 이 카드가 어떤 원본 CardData를 기반으로 만들어졌는지 외부에 공개합니다.
    /// </summary>
    public CardData Data => _cardData; // (Data 프로퍼티는 _cardData 값을 반환)

    void Awake()
    {
        // 하이라이터 컴포넌트 캐시 (가져오기)
        // (카드 앞면 오브젝트에 붙어있을 수도 있고, 루트 오브젝트에 붙어있을 수도 있어 2번 확인)
        if (cardFaceRenderer != null)
        {
            _highlighter = cardFaceRenderer.GetComponent<InteractableHighlighter>();
        }

        if (_highlighter == null)
        {
            _highlighter = GetComponent<InteractableHighlighter>();
        }

        if (cardFaceRenderer == null)
        {
            Debug.LogError($"[CardVisual] {name}의 'Card Face Renderer'가 연결되지 않았습니다!", this);
        }
    }

    /// <summary>
    /// 이 카드의 외형을 특정 CardData로 초기화하고 데이터를 저장합니다.
    /// (SkillCardManager가 Instantiate(cardPrefab) 직후에 호출합니다.)
    /// </summary>
    public void Initialize(CardData data)
    {
        if (data == null)
        {
            Debug.LogError("[CardVisual] 초기화할 CardData가 null입니다!");
            return;
        }

        _cardData = data; // 카드 데이터 저장

        // 카드 앞면 재질을 CardData에 지정된 재질로 변경
        if (cardFaceRenderer != null && data.cardFaceMaterial != null)
        {
            // [중요] 원본 머티리얼(data.cardFaceMaterial)을 '복제(Instantiate)'합니다.
            _myMaterialInstance = Instantiate(data.cardFaceMaterial);

            // 렌더러의 머티리얼을 이 복제본으로 교체합니다.
            cardFaceRenderer.material = _myMaterialInstance;

            // 하이라이터 스크립트에게 이 복제본이 '원본' 머티리얼이라고 알려줍니다.
            // (하이라이트가 꺼졌을 때 이 머티리얼로 복구하도록 설정)
            if (_highlighter != null)
            {
                _highlighter.SetOriginalMaterial(_myMaterialInstance);
            }
        }

        // 씬에서 쉽게 식별할 수 있도록 게임 오브젝트의 이름을 변경합니다.
        gameObject.name = $"Card - {data.cardName}";
    }

    /// <summary>
    /// 카드가 파괴될 때 (OnDestroy) 호출됩니다.
    /// 이 카드를 위해 '복제'했던 머티리얼 인스턴스(_myMaterialInstance)도 함께 파괴하여
    /// 메모리 누수(Memory Leak)를 방지합니다.
    /// </summary>
    void OnDestroy()
    {
        // _myMaterialInstance가 null이 아닐 때 (즉, Initialize가 성공적으로 복제했을 때)
        if (_myMaterialInstance != null)
        {
            // Unity는 Instantiate로 생성된 머티리얼을 자동으로 파괴해주지 않으므로,
            // 우리가 직접 Destroy 해주어야 합니다.
            Destroy(_myMaterialInstance);
        }
    }
}