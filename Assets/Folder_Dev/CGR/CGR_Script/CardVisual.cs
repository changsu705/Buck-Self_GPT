using UnityEngine;

/// <summary>
/// 3D 카드 오브젝트(프리팹)에 부착되는 스크립트입니다.
/// SkillCardManager가 카드를 생성할 때 Initialize()를 호출하여,
/// 전달받은 CardData를 기반으로 카드의 외형(머티리얼)과 기능(로직)을 설정합니다.
/// </summary>
public class CardVisual : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("카드 모델 중 '앞면'에 해당하는 MeshRenderer를 여기에 연결해야 합니다. (필수!)")]
    public MeshRenderer cardFaceRenderer;

    // --- 내부 상태 ---
    private InteractableHighlighter _highlighter;  // 이 카드의 하이라이터 스크립트 (캐시)
    private Material _myMaterialInstance; // 이 카드 '전용'으로 복제된 머티리얼 (메모리 관리용)
    private CardData _cardData;           // 이 카드가 가지고 있는 원본 데이터 (참조)
    private PlayerHand _myHand;           // 이 카드를 소유한 플레이어의 손 (제거 용도)

    /// <summary>
    /// [읽기 전용] 이 카드가 어떤 원본 CardData를 기반으로 만들어졌는지 외부에 공개합니다.
    /// </summary>
    public CardData Data => _cardData;

    void Awake()
    {
        // 1. 하이라이터 컴포넌트 캐시 (가져오기)
        if (cardFaceRenderer != null)
        {
            _highlighter = cardFaceRenderer.GetComponent<InteractableHighlighter>();
        }
        if (_highlighter == null)
        {
            _highlighter = GetComponent<InteractableHighlighter>();
        }

        // 2. 렌더러가 비어있으면 치명적 오류이므로 미리 경고
        if (cardFaceRenderer == null)
        {
            Debug.LogError($"[CardVisual] {name}의 'Card Face Renderer'가 연결되지 않았습니다! (프리팹 확인 필요)", this);
        }
    }

    /// <summary>
    /// 이 카드의 외형과 기능을 특정 CardData로 초기화합니다.
    /// (SkillCardManager가 Instantiate(cardPrefab) 직후에 호출합니다.)
    /// </summary>
    /// <param name="data">적용할 카드 데이터 (머티리얼, 로직 프리팹 포함)</param>
    /// <param name="ownerHand">이 카드를 소유할 플레이어의 PlayerHand</param>
    public void Initialize(CardData data, PlayerHand ownerHand)
    {
        if (data == null)
        {
            Debug.LogError("[CardVisual] 초기화할 CardData가 null입니다!");
            return;
        }

        _cardData = data; // 카드 데이터 저장
        _myHand = ownerHand; // 소유자 PlayerHand 저장 (OnDestroy에서 사용)

        // --- 1. 외형(머티리얼) 설정 ---
        if (cardFaceRenderer != null && data.cardFaceMaterial != null)
        {
            // [중요] 원본 머티리얼을 '복제(Instantiate)'합니다.
            _myMaterialInstance = Instantiate(data.cardFaceMaterial);
            // 렌더러의 머티리얼을 이 복제본으로 교체합니다.
            cardFaceRenderer.material = _myMaterialInstance;

            // 하이라이터 스크립트에게 이 복제본이 '원본' 머티리얼이라고 알려줍니다.
            if (_highlighter != null)
            {
                _highlighter.SetOriginalMaterial(_myMaterialInstance);
            }
        }
        else if (cardFaceRenderer != null)
        {
            // (디버깅) 머티리얼이 비어있음을 경고
            Debug.LogWarning($"[CardVisual] {data.name} 애셋의 'Card Face Material'이 비어있습니다.", data);
        }

        // --- 2. 기능(로직) 설정 ---
        if (data.cardLogicPrefab != null)
        {
            // 로직 프리팹을 이 카드의 '자식'으로 생성
            GameObject logicGO = Instantiate(data.cardLogicPrefab, this.transform);

            // 생성된 로직의 'Initialize()' 함수를 호출 (소유자 참조 전달)
            CardLogic logic = logicGO.GetComponent<CardLogic>();
            if (logic != null)
            {
                logic.Initialize(ownerHand);
            }
        }

        // 씬에서 쉽게 식별할 수 있도록 게임 오브젝트의 이름을 변경
        gameObject.name = $"Card - {data.cardName}";
    }

    /// <summary>
    /// (게임 도중) 이 카드의 앞면 머티리얼을 다른 머티리얼로 안전하게 교체합니다.
    /// (하이라이터 오류 및 메모리 누수를 방지합니다.)
    /// </summary>
    /// <param name="newOriginalMaterial">새로 적용할 '원본' 머티리얼 (복제해서 사용)</param>
    public void ChangeFaceMaterial(Material newOriginalMaterial)
    {
        if (newOriginalMaterial == null)
        {
            Debug.LogError("[CardVisual] 교체할 새 머티리얼(newOriginalMaterial)이 null입니다!");
            return;
        }
        if (cardFaceRenderer == null)
        {
            Debug.LogError("[CardVisual] Card Face Renderer가 연결되지 않아 머티리얼을 교체할 수 없습니다.");
            return;
        }

        // 1. 기존 머티리얼 인스턴스 파괴 (메모리 누수 방지)
        if (_myMaterialInstance != null)
        {
            Destroy(_myMaterialInstance);
        }

        // 2. 새 머티리얼 복제
        _myMaterialInstance = Instantiate(newOriginalMaterial);

        // 3. 렌더러에 새 복제본 적용
        cardFaceRenderer.material = _myMaterialInstance;

        // 4. 하이라이터에게 새 원본 머티리얼 알림 (중요!)
        if (_highlighter != null)
        {
            _highlighter.SetOriginalMaterial(_myMaterialInstance);
        }
    }

    /// <summary>
    /// 카드가 파괴될 때 (OnDestroy) 호출됩니다.
    /// (머티리얼 인스턴스 파괴 + PlayerHand에서 제거)
    /// </summary>
    void OnDestroy()
    {
        // 1. 머티리얼 인스턴스 파괴 (메모리 누수 방지)
        if (_myMaterialInstance != null)
        {
            Destroy(_myMaterialInstance);
        }

        // 2. 나를 소유했던 PlayerHand 리스트에서 나를 제거
        // (이걸 안 하면, PlayerHand.GetCards()에 null이 포함되어 오류 발생)
        if (_myHand != null)
        {
            _myHand.UnregisterCard(this);
        }
    }
}