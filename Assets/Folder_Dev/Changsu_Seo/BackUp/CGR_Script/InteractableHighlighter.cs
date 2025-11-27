using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CrosshairRaycaster가 쳐다봤을 때, 'highlightMaterial'로 교체하여 시각 효과를 주는 스크립트입니다.
/// - 총, 카드, 룩북 등 '상호작용 가능한' 모든 오브젝트에 부착됩니다.
/// - SetSelected(true) (예: 총을 픽업) 상태일 때는 하이라이트가 적용되지 않도록 제어합니다.
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    [Header("하이라이트 설정")]
    [Tooltip("쳐다봤을 때 교체할 하이라이트 전용 머티리얼")]
    public Material highlightMaterial;

    // --- 내부 상태 ---
    private Renderer[] _renderers; // 이 오브젝트와 자식들의 모든 렌더러 (캐시)

    // 각 렌더러의 '원본' 머티리얼을 저장하는 딕셔너리 (하이라이트 해제 시 복구용)
    private Dictionary<Renderer, Material> _originalMaterials = new Dictionary<Renderer, Material>();

    private bool _isInitialized = false; // 초기화 완료 여부

    // _ownsOriginalMaterial: 이 스크립트가 원본 머티리얼 인스턴스를 소유하고 파괴할 권한이 있는지
    // (true = 일반 오브젝트, false = CardVisual이 생성한 머티리얼을 참조)
    private bool _ownsOriginalMaterial = true;

    // _isSelected: 이 오브젝트가 '선택'(예: 총을 픽업)되었는지 여부
    // (RevolverTurnPossession이 SetSelected로 제어)
    private bool _isSelected = false;

    void Awake()
    {
        Initialize(); // 초기화 실행
    }

    /// <summary>
    /// 초기화: 이 오브젝트 및 자식 오브젝트의 모든 렌더러를 찾고,
    /// 각 렌더러의 현재(원본) 머티리얼을 _originalMaterials 딕셔너리에 저장합니다.
    /// </summary>
    private void Initialize()
    {
        if (_isInitialized) return; // 중복 초기화 방지

        _renderers = GetComponentsInChildren<Renderer>(); // 자식 포함 모든 렌더러 검색

        if (_renderers.Length == 0)
        {
            Debug.LogError($"[Highlighter] {name} 또는 그 자식에서 Renderer 컴포넌트를 찾지 못했습니다!", this);
            return;
        }

        if (highlightMaterial == null)
        {
            Debug.LogError($"[Highlighter] {name}에 'Highlight Material'이 연결되지 않았습니다!", this);
            return;
        }

        _originalMaterials.Clear();
        foreach (Renderer r in _renderers)
        {
            // [중요] r.material은 '인스턴스'를 생성합니다. (r.sharedMaterial과 다름)
            // 이 인스턴스를 저장해두어야 나중에 복구할 수 있습니다.
            _originalMaterials[r] = r.material;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// (CardVisual.cs 전용 함수)
    /// CardVisual이 동적으로 생성한 머티리얼 인스턴스를 '원본' 머티리얼로 등록합니다.
    /// 이 경우, 이 스크립트는 해당 머티리얼의 파괴 권한을 갖지 않습니다. (소유권 포기)
    /// </summary>
    public void SetOriginalMaterial(Material cardMaterialInstance)
    {
        // (CardVisual은 보통 루트에 Renderer가 있음)
        Renderer cardRenderer = GetComponent<Renderer>();
        if (cardRenderer != null)
        {
            // 만약 Awake()에서 이미 렌더러를 찾고 기본 머티리얼 인스턴스를 만들었다면,
            if (_ownsOriginalMaterial && _originalMaterials.ContainsKey(cardRenderer))
            {
                // 그 잘못된 인스턴스를 지금 파괴합니다.
                Destroy(_originalMaterials[cardRenderer]);
            }

            // CardVisual이 준 진짜 원본 머티리얼로 교체
            _originalMaterials[cardRenderer] = cardMaterialInstance;
            _ownsOriginalMaterial = false; // 소유권 포기 (CardVisual이 파괴할 것임)
        }
    }

    /// <summary>
    /// 하이라이트 상태를 켜거나 끕니다. (CrosshairRaycaster가 호출)
    /// </summary>
    /// <param name="active">하이라이트를 활성화할지(true), 해제할지(false)</param>
    public void SetHighlight(bool active)
    {
        if (!_isInitialized) Initialize(); // 혹시 초기화가 안됐으면 실행

        if (active)
        {
            // [하이라이트 켜기]

            // 단, 이 오브젝트가 '선택된' 상태(예: 총을 든 상태)라면 하이라이트를 켜지 않고 무시합니다.
            if (_isSelected) return;

            // 모든 렌더러의 머티리얼을 'highlightMaterial'로 교체
            foreach (Renderer r in _renderers)
            {
                r.material = highlightMaterial;
            }
        }
        else
        {
            // [하이라이트 끄기]

            // '선택된' 상태와 상관없이, 항상 '원본' 머티리얼로 복구합니다.
            foreach (Renderer r in _renderers)
            {
                if (r != null && _originalMaterials.ContainsKey(r))
                {
                    r.material = _originalMaterials[r]; // 저장해둔 원본 머티리얼로 복구
                }
            }
        }
    }

    /// <summary>
    /// 외부(예: RevolverTurnPossession)에서 이 오브젝트의 '선택' 상태를 설정합니다.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        // 오브젝트가 '선택'(true)되었다면 (예: 총을 집었다면),
        // 혹시 켜져있을지 모르는 하이라이트를 즉시 끕니다. (원본 머티리얼로 복구)
        if (selected)
        {
            SetHighlight(false);
        }
    }

    /// <summary>
    /// 이 스크립트가 파괴될 때(OnDestroy) 호출됩니다.
    /// 이 스크립트가 직접 생성하여 '소유'하고 있는 머티리얼 인스턴스만 파괴합니다.
    /// (메모리 누수 방지)
    /// </summary>
    void OnDestroy()
    {
        // _ownsOriginalMaterial이 true일 때만 (즉, CardVisual이 아닐 때만)
        if (_ownsOriginalMaterial)
        {
            foreach (Material mat in _originalMaterials.Values)
            {
                // (null이 아니고, highlightMaterial도 아닌) 우리가 Awake에서 생성한
                // '인스턴스' 머티리얼만 파괴합니다.
                if (mat != null && mat != highlightMaterial)
                {
                    Destroy(mat);
                }
            }
        }
    }
}