using UnityEngine;
// using QuickOutline; // Outline.cs에 namespace QuickOutline을 썼다면 이 줄 주석 해제

/// <summary>
/// 플레이어가 십자선으로 쳐다보는 상호작용 가능 오브젝트의 QuickOutline을 on/off 합니다.
/// (예: 총, 카드, 룩북)
/// 
/// - 'SetSelected(true)' (예: 총을 픽업) 상태가 되면 하이라이트가 비활성화됩니다.
/// - 머티리얼을 직접 건드리지 않고, Outline 컴포넌트 enabled 값만 토글합니다.
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    [Header("QuickOutline 설정")]
    [Tooltip("자동 추가될 Outline 컴포넌트의 모드")]
    public Outline.Mode outlineMode = Outline.Mode.OutlineAll;

    [Tooltip("하이라이트 색상")]
    public Color outlineColor = Color.yellow;

    [Range(0f, 10f)]
    [Tooltip("하이라이트 두께")]
    public float outlineWidth = 5f;

    [Tooltip("Renderer에 Outline 컴포넌트가 없으면 자동으로 AddComponent 할지 여부")]
    public bool autoAddOutline = true;

    //  기존 머티리얼 방식에서 쓰던 필드 (호환용, 더 이상 사용하지 않음)
    [HideInInspector]
    public Material highlightMaterial;

    // --- 내부 상태 ---
    private Renderer[] _renderers;          // 이 오브젝트와 자식들의 Renderer
    private readonly System.Collections.Generic.List<Outline> _outlines
        = new System.Collections.Generic.List<Outline>(); // 대상 Outline들

    private bool _isInitialized = false;    // 초기화 완료 여부
    private bool _isSelected = false;       // 선택 상태 (예: 총을 들고 있는 상태)

    void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 초기화: Renderer들을 찾고, Outline 컴포넌트를 준비합니다.
    /// </summary>
    private void Initialize()
    {
        if (_isInitialized) return;

        _renderers = GetComponentsInChildren<Renderer>();

        if (_renderers == null || _renderers.Length == 0)
        {
            Debug.LogError($"[Highlighter] {name} 또는 그 자식에서 Renderer를 찾지 못했습니다!", this);
            return;
        }

        _outlines.Clear();

        foreach (Renderer r in _renderers)
        {
            if (r == null) continue;

            // 이미 Outline이 붙어 있으면 재사용, 없으면 옵션에 따라 추가
            Outline outline = r.GetComponent<Outline>();
            if (outline == null)
            {
                if (!autoAddOutline)
                    continue;

                outline = r.gameObject.AddComponent<Outline>();
            }

            if (outline == null) continue;

            // QuickOutline 기본 설정
            outline.OutlineMode = outlineMode;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;

            // 기본은 꺼둔 상태
            outline.enabled = false;

            _outlines.Add(outline);
        }

        if (_outlines.Count == 0)
        {
            Debug.LogWarning($"[Highlighter] {name}에서 사용할 Outline 컴포넌트를 찾지 못했습니다. autoAddOutline={autoAddOutline}", this);
        }

        _isInitialized = true;
    }
    public void SetOriginalMaterial(Material cardMaterialInstance)
    {
    }

    /// <summary>
    /// CrosshairRaycaster가 호출하는 하이라이트 on/off 함수.
    /// </summary>
    /// <param name="active">true = 하이라이트 켜기, false = 끄기</param>
    public void SetHighlight(bool active)
    {
        if (!_isInitialized) Initialize();
        if (_outlines.Count == 0) return;

        if (active)
        {
            // 선택 중이면(예: 들고 있는 총) 하이라이트를 켜지 않음
            if (_isSelected) return;

            foreach (var outline in _outlines)
            {
                if (outline != null)
                    outline.enabled = true;
            }
        }
        else
        {
            // 선택 여부와 상관없이 하이라이트는 항상 끔
            foreach (var outline in _outlines)
            {
                if (outline != null)
                    outline.enabled = false;
            }
        }
    }

    /// <summary>
    /// 외부(예: RevolverTurnPossession)에서 이 오브젝트의 '선택' 상태를 설정합니다.
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        // 선택되면 하이라이트 즉시 제거
        if (selected)
        {
            SetHighlight(false);
        }
    }
}
