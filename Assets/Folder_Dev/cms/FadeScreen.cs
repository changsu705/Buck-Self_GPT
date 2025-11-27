using System.Collections;
using UnityEngine;

public class FadeScreen : MonoBehaviour
{
    // === [공통 설정] ===
    public float fadeDuration = 2f; // 일반 페이드 시간
    public Color baseColor = Color.white;
    private Renderer rend;

    // === [시작 시 설정] ===
    public bool fadeOnStart = true;
    public float startFadeDelay = 0.5f; // 시작 페이드 전 딜레이 (선택 사항)

    // === [피격 시 설정] ===
    // 피격시 깜빡이는 데 사용할 별도 시간과 색상
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor = Color.red;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) return;

        // 1. 시작 시 초기 알파값 설정 (불투명)
        Color startColor = baseColor;
        startColor.a = 1f;
        rend.material.SetColor("_Color", startColor);

        // 2. 시작 페이드 아웃 실행
        if (fadeOnStart)
        {
            StartCoroutine(StartFadeCoroutine());
        }
    }

    // --- [시작 페이드 코루틴] ---
    IEnumerator StartFadeCoroutine()
    {
        // 딜레이가 있다면 잠시 대기 (선택 사항)
        if (startFadeDelay > 0)
        {
            yield return new WaitForSeconds(startFadeDelay);
        }

        // 불투명(1) -> 투명(0)으로 일반 페이드 아웃 실행
        Fade(1, 0, fadeDuration);
    }

    // --- [피격 플래시 코루틴] ---
    public void FlashOnHit()
    {
        // 피격 효과는 기존 코루틴을 멈추지 않고 별도로 짧게 실행
        StartCoroutine(FlashRoutine(hitFlashColor, hitFlashDuration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        // 1. 기존 색상을 저장해 둠
        Color originalColor = rend.material.GetColor("_Color");

        // 2. 즉시 피격 색상 (예: 빨간색, 불투명 1f)으로 변경
        Color flashColor = color;
        flashColor.a = 1f;
        rend.material.SetColor("_Color", flashColor);

        // 3. 짧은 시간 대기
        yield return new WaitForSeconds(duration);

        // 4. 다시 원래 색상으로 되돌립니다.
        // 이때, 기존에 실행 중이던 FadeRoutine이 있다면 그 상태로 돌아갑니다.
        rend.material.SetColor("_Color", originalColor);
    }

    // --- [공통 페이드 함수] ---

    // 페이드 인/아웃 요청 (일반적인 전환용)
    public void Fadein() { Fade(0, 1, fadeDuration); }
    public void Fadeout() { Fade(1, 0, fadeDuration); }

    // 페이드 실행 (외부에서 직접 호출 가능, 시간을 매개변수로 받음)
    public void Fade(float alphaIn, float alphaOut, float duration)
    {
        // 일반 페이드 전환 시에는 기존 코루틴을 중단하고 새로운 코루틴 시작
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(alphaIn, alphaOut, duration));
    }

    // 코루틴: 지정된 Alpha 값으로 부드럽게 전환
    public IEnumerator FadeRoutine(float alphaStart, float alphaEnd, float duration)
    {
        float timer = 0;
        while (timer < duration)
        {
            Color newColor = baseColor;
            newColor.a = Mathf.Lerp(alphaStart, alphaEnd, timer / duration);

            rend.material.SetColor("_Color", newColor);

            timer += Time.deltaTime;
            yield return null;
        }

        // 루프 종료 후 목표 Alpha 값으로 고정
        Color finalColor = baseColor;
        finalColor.a = alphaEnd;
        rend.material.SetColor("_Color", finalColor);
    }
}