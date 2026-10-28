using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }


    private AudioSource audioSource = null;
    private AudioSource mudEscapeAudioSource = null;
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip ClearClip;
    [SerializeField] private AudioClip FailClip;
    [SerializeField] private AudioClip StartBgmClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        mudEscapeAudioSource = gameObject.AddComponent<AudioSource>();
        mudEscapeAudioSource.loop = false;
        mudEscapeAudioSource.playOnAwake = false;
    }

    public void PlaySE(int index)
    {
        if (index < 0 || index >= audioClips.Length)
        {
            Debug.LogWarning($"AudioManager: Invalid audio clip index {index}");
            return;
        }
        else
        {
            audioSource.PlayOneShot(audioClips[index]);
        }
    }

    public void PlayBGM()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("AudioManager: BGM clip is not assigned");
            return;
        }

        if (audioSource.clip == bgmClip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void PlayStartBGM()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("AudioManager: StartBGM clip is not assigned");
            return;
        }

        if (audioSource.clip == bgmClip && audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = StartBgmClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void PlayClear()
    {
        PlayResultSE(ClearClip, 1.0f);
    }

    public void PlayFail()
    {
        PlayResultSE(FailClip, 0.5f);
    }

    private void PlayResultSE(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("Result clip is not assigned");
            return;
        }

        StopAllGameAudio();

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void StopBGM()
    {
        if (audioSource == null)
        {
            return;
        }

        Debug.Log($"StopBGM 呼ばれた  isPlaying={audioSource.isPlaying}");

        audioSource.Stop();
        audioSource.loop = false;

        Debug.Log($"Stop後 isPlaying={audioSource.isPlaying}");
    }

    public Coroutine FadeOutBGM(MonoBehaviour runner, float duration)
    {
        if (runner == null || audioSource == null)
        {
            return null;
        }

        return runner.StartCoroutine(FadeOutBGMRoutine(duration));
    }

    private IEnumerator FadeOutBGMRoutine(float duration)
    {
        if (audioSource == null || !audioSource.isPlaying)
        {
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.volume = startVolume;
        audioSource.loop = false;
    }

    public void PlayMudEscapeSE(int index)
    {
        // ぬかるみ脱出音はIndex 2～11のみ使用する
        if (index < 2 || index > 11 || index >= audioClips.Length)
        {
            Debug.LogWarning(
                $"AudioManager: Invalid mud escape audio clip index {index}"
            );
            return;
        }

        AudioClip clip = audioClips[index];

        if (clip == null)
        {
            Debug.LogWarning(
                $"AudioManager: Mud escape audio clip {index} is not assigned"
            );
            return;
        }

        // 同じ音がすでにループ再生中なら何もしない
        if (mudEscapeAudioSource.isPlaying &&
            mudEscapeAudioSource.clip == clip)
        {
            return;
        }

        // 現在の段階の音を停止
        mudEscapeAudioSource.Stop();

        // 次の段階の音を設定
        mudEscapeAudioSource.clip = clip;

        // ぬかるみ脱出中はループさせる
        mudEscapeAudioSource.loop = true;

        // 再生
        mudEscapeAudioSource.Play();
    }

    public void StopMudEscapeSE()
    {
        if (mudEscapeAudioSource == null)
        {
            return;
        }

        mudEscapeAudioSource.Stop();
        mudEscapeAudioSource.loop = false;
        mudEscapeAudioSource.clip = null;
    }

    public void StopAllGameAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        if (mudEscapeAudioSource != null)
        {
            mudEscapeAudioSource.Stop();
        }
    }

    public void SetAudioVolume(float volume)
    {
        audioSource.volume = volume;
    }
}
