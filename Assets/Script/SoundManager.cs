// 유니티 엔진 및 오디오 관련 기능을 사용하기 위해 선언합니다.
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // UI 컴포넌트(Slider)를 사용하기 위해 필요합니다.

/// <summary>
/// 게임의 사운드를 총괄하는 관리자 스크립트입니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 유니티 에디터에서 설정할 오디오 믹서 변수입니다.
    public AudioMixer masterMixer;

    // BGM과 SFX의 음소거 기능을 위해 슬라이더 참조가 필요합니다.
    public Slider bgmSlider;
    public Slider sfxSlider;

    /// <summary>
    /// BGM 슬라이더의 값이 변경될 때 호출될 함수입니다.
    /// </summary>
    /// <param name="sliderValue">슬라이더의 현재 값 (0.0001 ~ 1)</param>
    public void SetBGMVolume(float sliderValue)
    {
        // 오디오 믹서는 데시벨(Decibel) 단위를 사용합니다. (선형적이지 않음)
        // 슬라이더의 값(0~1)을 데시벨 값(-80~0)으로 변환해줘야 합니다.
        // Log10을 사용하는 것이 인간의 청각 인지와 가장 유사합니다.
        // sliderValue가 0이 되면 -Infinity가 되므로, 슬라이더의 최소값을 0.0001로 설정해야 합니다.
        masterMixer.SetFloat("BGMVolume", Mathf.Log10(sliderValue) * 20);
    }

    /// <summary>
    /// SFX 슬라이더의 값이 변경될 때 호출될 함수입니다.
    /// </summary>
    /// <param name="sliderValue">슬라이더의 현재 값 (0.0001 ~ 1)</param>
    public void SetSFXVolume(float sliderValue)
    {
        masterMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20);
    }

    /// <summary>
    /// BGM 음소거 토글(체크박스)의 값이 변경될 때 호출될 함수입니다.
    /// </summary>
    /// <param name="isMuted">음소거 여부 (true/false)</param>
    public void SetBGMMute(bool isMuted)
    {
        if (isMuted)
        {
            // 음소거 시 볼륨을 가장 낮은 값(-80dB)으로 설정합니다.
            masterMixer.SetFloat("BGMVolume", -80f);
        }
        else
        {
            // 음소거 해제 시, 현재 슬라이더의 값으로 볼륨을 복원합니다.
            SetBGMVolume(bgmSlider.value);
        }
    }

    /// <summary>
    /// SFX 음소거 토글(체크박스)의 값이 변경될 때 호출될 함수입니다.
    /// </summary>
    /// <param name="isMuted">음소거 여부 (true/false)</param>
    public void SetSFXMute(bool isMuted)
    {
        if (isMuted)
        {
            masterMixer.SetFloat("SFXVolume", -80f);
        }
        else
        {
            SetSFXVolume(sfxSlider.value);
        }
    }
}