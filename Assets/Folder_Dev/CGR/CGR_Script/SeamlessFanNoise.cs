using UnityEngine;

/// <summary>
/// [완벽한 루프] 팬 소리처럼 지속적인 소음이 끊기지 않게
/// 2개의 오디오 소스를 시차를 두고 겹쳐서 재생합니다.
/// </summary>
public class SeamlessFanNoise : MonoBehaviour
{
    [Header("오디오 설정")]
    public AudioClip fanClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    [Tooltip("0=2D(배경음), 1=3D(거리감)")]
    [Range(0f, 1f)] public float spatialBlend = 0.0f;

    void Start()
    {
        if (fanClip == null) return;

        // 1. 첫 번째 스피커 생성 (0초부터 시작)
        CreateAudioSource("Fan_Layer_1", 0f);

        // 2. 두 번째 스피커 생성 (오디오 길이의 절반 지점부터 시작)
        // 이렇게 하면 하나의 소리가 끝나는 지점(틱 소리)을 다른 소리가 덮어줍니다.
        float halfDuration = fanClip.length / 2f;
        CreateAudioSource("Fan_Layer_2", halfDuration);
    }

    void CreateAudioSource(string name, float delaySeconds)
    {
        // 새 자식 오브젝트 생성
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform);
        go.transform.localPosition = Vector3.zero; // 부모 위치에 고정

        // 오디오 소스 설정
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = fanClip;
        source.volume = volume * 0.6f; // 두 개가 겹치므로 볼륨을 살짝 줄임
        source.loop = true;
        source.spatialBlend = spatialBlend;
        source.playOnAwake = false;

        // 딜레이를 주고 재생 (중요!)
        // PlayDelayed대신 timeSamples를 조절하여 즉시 해당 위치에서 시작하게 함
        source.Play();
        source.time = delaySeconds;
    }
}