using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sound Data")]
    [SerializeField] private SoundData soundData;

    [Header("BGM")]
    [SerializeField] private bool playBgmOnStart = true;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionSoundCooldown = 2f;

    private readonly Dictionary<SFXType, SFXData> sfxDict = new Dictionary<SFXType, SFXData>();
    private readonly Dictionary<BGMType, AudioClip> bgmDict = new Dictionary<BGMType, AudioClip>();

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource downloadSource;

    private float lastCCTVDetectionTime = float.NegativeInfinity;
    private float lastGuardDetectionTime = float.NegativeInfinity;

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
        InitSoundDictionary();
    }

    private void Start()
    {
        if (playBgmOnStart)
        {
            PlayBGM();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // AudioSource 초기화
    private void InitializeAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        // 첫 번째 AudioSource : SFX
        sfxSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1f;
        sfxSource.pitch = 1f;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        // 두 번째 AudioSource : BGM
        bgmSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = 1f;
        bgmSource.outputAudioMixerGroup = bgmGroup;

        // 세 번째 AudioSource : 다운로드 진행음
        downloadSource = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();

        downloadSource.playOnAwake = false;
        downloadSource.loop = true;
        downloadSource.spatialBlend = 0f;
        downloadSource.volume = 1f;
        downloadSource.pitch = 1f;
        downloadSource.outputAudioMixerGroup = sfxGroup;
    }

    // SoundData를 Dictionary로 캐싱
    private void InitSoundDictionary()
    {
        sfxDict.Clear();
        bgmDict.Clear();

        if (soundData == null)
        {
            Debug.LogWarning("[SoundManager] SoundData가 연결되지 않았습니다.");
            return;
        }

        if (soundData.sfxList != null)
        {
            for (int i = 0; i < soundData.sfxList.Count; i++)
            {
                SFXData data = soundData.sfxList[i];

                if (data == null)
                {
                    continue;
                }

                if (sfxDict.ContainsKey(data.sfxType))
                {
                    Debug.LogWarning($"[SoundManager] 중복된 SFXType: {data.sfxType}");
                    continue;
                }

                sfxDict.Add(data.sfxType, data);
            }
        }

        if (soundData.bgmList != null)
        {
            for (int i = 0; i < soundData.bgmList.Count; i++)
            {
                BGMData data = soundData.bgmList[i];

                if (data == null)
                {
                    continue;
                }

                if (bgmDict.ContainsKey(data.bgmType))
                {
                    Debug.LogWarning($"[SoundManager] 중복된 BGMType: {data.bgmType}");
                    continue;
                }

                bgmDict.Add(data.bgmType, data.clip);
            }
        }
    }

    private bool TryGetSFXData(SFXType type, out SFXData data)
    {
        data = null;

        if (!sfxDict.TryGetValue(type, out SFXData found))
        {
            return false;
        }

        if (found == null || found.clip == null)
        {
            return false;
        }

        data = found;
        return true;
    }

    // 공통 SFX 재생
    public void PlaySFX(SFXType type)
    {
        if (sfxSource == null || !TryGetSFXData(type, out SFXData data))
        {
            return;
        }

        sfxSource.PlayOneShot(data.clip, data.volume);
    }

    public void PlaySFXAtPosition(SFXType type, Vector3 position)
    {
        if (!TryGetSFXData(type, out SFXData data))
        {
            return;
        }

        // 효과음을 재생할 임시 3D 오브젝트 생성
        GameObject soundObject = new GameObject($"3D SFX - {type}");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = 20f;
        source.dopplerLevel = 0f;
        source.volume = 1f;
        source.outputAudioMixerGroup = sfxGroup;

        source.PlayOneShot(data.clip, data.volume);

        // 클립 재생이 끝나면 임시 오브젝트 제거
        Destroy(soundObject, data.clip.length + 0.1f);
    }

    // UI Sound
    public void PlayHover() => PlaySFX(SFXType.UIHover);
    public void PlayClick() => PlaySFX(SFXType.UIClick);
    public void PlayOpen() => PlaySFX(SFXType.UIOpen);
    public void PlayClose() => PlaySFX(SFXType.UIClose);

    public void PlayTerminalHover()
    {
        if (TryGetSFXData(SFXType.TerminalHover, out _))
        {
            PlaySFX(SFXType.TerminalHover);
        }
        else
        {
            PlayHover();
        }
    }

    public void PlayTerminalClick()
    {
        if (TryGetSFXData(SFXType.TerminalClick, out _))
        {
            PlaySFX(SFXType.TerminalClick);
        }
        else
        {
            PlayClick();
        }
    }

    // System Sound
    public void PlayConfirm() => PlaySFX(SFXType.Confirm);
    public void PlayError() => PlaySFX(SFXType.Error);
    public void PlayNotification() => PlaySFX(SFXType.Notification);

    // Mission Sound
    public void PlayMissionSuccess() => PlaySFX(SFXType.MissionSuccess);
    public void PlayMissionFail() => PlaySFX(SFXType.MissionFail);

    // Detection Sound
    public void PlayCCTVDetection()
    {
        if (Time.timeScale <= 0f || !TryGetSFXData(SFXType.CCTVDetection, out _))
        {
            return;
        }

        if (Time.unscaledTime - lastCCTVDetectionTime < detectionSoundCooldown)
        {
            return;
        }

        lastCCTVDetectionTime = Time.unscaledTime;
        PlaySFX(SFXType.CCTVDetection);
    }

    public void PlayGuardDetection()
    {
        if (Time.timeScale <= 0f || !TryGetSFXData(SFXType.GuardDetection, out _))
        {
            return;
        }

        if (Time.unscaledTime - lastGuardDetectionTime < detectionSoundCooldown)
        {
            return;
        }

        lastGuardDetectionTime = Time.unscaledTime;
        PlaySFX(SFXType.GuardDetection);
    }

    // Download Sound
    public void PlayDownloadProgress()
    {
        if (downloadSource == null || !TryGetSFXData(SFXType.DownloadProgress, out SFXData data))
        {
            return;
        }

        downloadSource.Stop();
        downloadSource.clip = data.clip;
        downloadSource.volume = data.volume;
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
        PlaySFX(SFXType.DownloadComplete);
    }

    // Security Alert
    public void PlaySecurityAlert()
    {
        PlaySFX(SFXType.SecurityAlert);
    }

    // Throw Sound - 추후 투척물 연동
    public void PlayThrow()
    {
        PlaySFX(SFXType.Throw);
    }

    // BGM
    public void PlayBGM()
    {
        PlayBGM(BGMType.MainMenu);
    }

    public void PlayBGM(BGMType type)
    {
        if (!bgmDict.TryGetValue(type, out AudioClip clip))
        {
            return;
        }

        PlayBGM(clip);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
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
}
