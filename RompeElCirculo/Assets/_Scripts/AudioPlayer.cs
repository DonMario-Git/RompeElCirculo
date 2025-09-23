using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class AudioPlayer : MonoBehaviour
{
    public static AudioPlayer singleton;

    public AudioSource audioSource;
    public AudioEffect[] clips;
    private Tween fadeTween;

    private void Awake()
    {
        singleton = this;
    }

    private void FadeOutAndStop(float duration, System.Action onComplete)
    {
        if (fadeTween != null && fadeTween.IsActive())
            fadeTween.Kill();

        fadeTween = audioSource.DOFade(0f, duration).OnComplete(() =>
        {
            audioSource.Stop();
            onComplete?.Invoke();
        });
    }

    public void PlayVoiceClip(AudioClip clip)
    {
        FadeOutAndStop(0.1f, () =>
        {
            audioSource.clip = clip;
            audioSource.pitch = 1;
            audioSource.volume = 1;
            audioSource.Play();
        });
    }

    public void PlayAudio(int index)
    {
        FadeOutAndStop(0.1f, () =>
        {
            audioSource.clip = clips[index].clip;
            audioSource.pitch = Random.Range(clips[index].pitchMin, clips[index].pitchMax);
            audioSource.volume = clips[index].volume;
            audioSource.Play();
        });
    } 

    public void PlayAudioEffect(AudioEffect audio)
    {
        FadeOutAndStop(0.1f, () =>
        {
            audioSource.clip = audio.clip;
            audioSource.pitch = Random.Range(audio.pitchMin, audio.pitchMax);
            audioSource.volume = audio.volume;
            audioSource.Play();
        });
    }
}

[System.Serializable]
public class AudioEffect
{
    public AudioClip clip;
    public float volume = 1f;
    public float pitchMin = 1f;
    public float pitchMax = 1f;
}
