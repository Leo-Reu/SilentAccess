
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("BGM")]
    [SerializeField] private AudioClip defaultBgm;
    [SerializeField] private bool playBgmOnStart = true;

    [Header("UI Sound")]
    [SerializeField] private AudioClip hoverSfx;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private AudioClip closeSfx;

    [Header("System Sound")]
    [SerializeField] private AudioClip confirmSfx;
    [SerializeField] private AudioClip errorSfx;
    [SerializeField] private AudioClip notificationSfx;

    [Header("Mission Sound")]
    [SerializeField] private AudioClip missionSuccessSfx;
    [SerializeField] private AudioClip missionFailSfx;

    [Header("Terminal UI Sound")]
    [SerializeField] private AudioClip terminalHoverSfx;
    [SerializeField] private AudioClip terminalClickSfx;

    [Header("Download Sound")]
    [SerializeField] private AudioClip downloadProgressSfx;
    [SerializeField] private AudioClip downloadCompleteSfx;

    [Header("Detection Sound")]
    [SerializeField] private AudioClip cctvDetectionSfx;
    [SerializeField] private AudioClip guardDetectionSfx;
    [SerializeField, Min(0f)] private float detectionSoundCooldown = 2f;

    private float lastCCTVDetectionTime = float.NegativeInfinity;
    private float lastGuardDetectionTime = float.NegativeInfinity;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource downloadSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        SoundManager prefab = Resources.Load<SoundManager>("SoundManager");

        if (prefab == null)
        {
            Debug.LogError("SoundManager 프리팹을 찾을 수 없습니다.");
            return;
        }

        Instantiate(prefab);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
    }

    private void Start()
    {
        if (playBgmOnStart)
        {
            PlayBGM();
        }
    }

    private void InitializeAudioSources()
    {
        sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        AudioSource[] sources = GetComponents<AudioSource>();
        bgmSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmGroup;
        bgmSource.clip = defaultBgm;

        // 다운로드 진행음 전용 AudioSource
        downloadSource = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();

        downloadSource.playOnAwake = false;
        downloadSource.loop = true;
        downloadSource.spatialBlend = 0f;
        downloadSource.volume = 0.4f;
        downloadSource.outputAudioMixerGroup = sfxGroup;
    }

    // BGM
    public void PlayBGM()
    {
        if (bgmSource == null || bgmSource.clip == null || bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.Play();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        // 동일한 BGM이 재생 중이라면 처음부터 재생하지 않음
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }
    }

    // SFX
    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    public void PlayHover() => PlaySfx(hoverSfx);
    public void PlayClick() => PlaySfx(clickSfx);
    public void PlayOpen() => PlaySfx(openSfx);
    public void PlayClose() => PlaySfx(closeSfx);
    public void PlayConfirm() => PlaySfx(confirmSfx);
    public void PlayError() => PlaySfx(errorSfx);
    public void PlayNotification() => PlaySfx(notificationSfx);
    public void PlayMissionSuccess() => PlaySfx(missionSuccessSfx);
    public void PlayMissionFail() => PlaySfx(missionFailSfx);
    public void PlayTerminalHover() => PlaySfx(terminalHoverSfx != null ? terminalHoverSfx : hoverSfx);
    public void PlayTerminalClick() => PlaySfx(terminalClickSfx != null ? terminalClickSfx : clickSfx);

    public void PlayCCTVDetection()
    {
        if (cctvDetectionSfx == null || Time.timeScale <= 0f || Time.unscaledTime - lastCCTVDetectionTime < detectionSoundCooldown)
        {
            return;
        }

        lastCCTVDetectionTime = Time.unscaledTime;
        PlaySfx(cctvDetectionSfx);
    }

    public void PlayGuardDetection()
    {
        if (guardDetectionSfx == null || Time.timeScale <= 0f || Time.unscaledTime - lastGuardDetectionTime < detectionSoundCooldown)
        {
            return;
        }

        lastGuardDetectionTime = Time.unscaledTime;
        PlaySfx(guardDetectionSfx);
    }
    public void PlayDownloadProgress()
    {
        if (downloadSource == null || downloadProgressSfx == null)
        {
            return;
        }

        downloadSource.Stop();
        downloadSource.clip = downloadProgressSfx;
        downloadSource.Play();
    }

    public void StopDownloadProgress()
    {
        if (downloadSource != null)
        {
            downloadSource.Stop();
        }
    }

    public void PlayDownloadComplete()
    {
        PlaySfx(downloadCompleteSfx);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
