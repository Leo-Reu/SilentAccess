using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject returnPanel;
    [SerializeField] private ControlsUI controlsUI;

    [Header("Navigation")]
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private Selectable returnSelected;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer sfxMixer;

    private const string ResolutionWidthKey = "SilentAccess.ResolutionWidth";
    private const string ResolutionHeightKey = "SilentAccess.ResolutionHeight";
    private const string ScreenModeKey = "SilentAccess.ScreenMode";
    private const string MasterVolumeKey = "SilentAccess.MasterVolume";
    private const string SFXVolumeKey = "SilentAccess.SFXVolume"; 
    private const string BGMVolumeKey = "SilentAccess.BGMVolume";

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;
    public int LastClosedFrame { get; private set; } = -1;

    private void Awake()
    {
        BuildResolutionOptions();
        BuildScreenModeOptions();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        LoadSavedValuesIntoUI();
    }

    private void Start()
    {
        ApplySavedSettings();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (controlsUI != null && controlsUI.IsOpen)
        {
            if (controlsUI.IsRebinding || controlsUI.LastRebindFinishedFrame == Time.frameCount)
            {
                return;
            }

            bool controlsEscapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool controlsCancelPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

            if (controlsEscapePressed || controlsCancelPressed)
            {
                controlsUI.CloseControls();
            }

            return;
        }

        bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadCancelPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        if (!cancelPressed && !gamepadCancelPressed)
        {
            return;
        }

        if ((resolutionDropdown != null && resolutionDropdown.IsExpanded) || (screenModeDropdown != null && screenModeDropdown.IsExpanded))
        {
            return;
        }

        Close();
    }

    private void BuildResolutionOptions()
    {
        resolutions.Clear();

        Resolution[] supportedResolutions = Screen.resolutions;

        for (int i = 0; i < supportedResolutions.Length; i++)
        {
            Vector2Int size = new Vector2Int(supportedResolutions[i].width, supportedResolutions[i].height);

            if (!resolutions.Contains(size))
            {
                resolutions.Add(size);
            }
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        if (resolutionDropdown == null)
        {
            return;
        }

        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Count; i++)
        {
            options.Add($"{resolutions[i].x} × {resolutions[i].y}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void BuildScreenModeOptions()
    {
        if (screenModeDropdown == null)
        {
            return;
        }

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string>
        {
            "전체 화면",
            "테두리 없는 창",
            "창 모드"
        });
    }

    private void LoadSavedValuesIntoUI()
    {
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        int resolutionIndex = -1;

        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].x == savedWidth && resolutions[i].y == savedHeight)
            {
                resolutionIndex = i;
                break;
            }
        }

        if (resolutionIndex < 0)
        {
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].x == Screen.width && resolutions[i].y == Screen.height)
                {
                    resolutionIndex = i;
                    break;
                }
            }
        }

        if (resolutionIndex < 0)
        {
            resolutionIndex = resolutions.Count - 1;
        }

        int savedMode = PlayerPrefs.GetInt(ScreenModeKey, GetCurrentScreenModeIndex());

        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        if (screenModeDropdown != null)
        {
            screenModeDropdown.SetValueWithoutNotify(Mathf.Clamp(savedMode, 0, 2));
            screenModeDropdown.RefreshShownValue();
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = Mathf.Clamp(PlayerPrefs.GetInt(MasterVolumeKey, 100), 0, 100);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = Mathf.Clamp(PlayerPrefs.GetInt(BGMVolumeKey, 100), 0, 100);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = Mathf.Clamp(PlayerPrefs.GetInt(SFXVolumeKey, 100), 0, 100);
        }
    }

    private int GetCurrentScreenModeIndex()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return 0;

            case FullScreenMode.FullScreenWindow:
            case FullScreenMode.MaximizedWindow:
                return 1;

            default:
                return 2;
        }
    }

    private FullScreenMode GetScreenMode(int index)
    {
        switch (index)
        {
            case 0:
                return FullScreenMode.ExclusiveFullScreen;

            case 1:
                return FullScreenMode.FullScreenWindow;

            default:
                return FullScreenMode.Windowed;
        }
    }

    private void ApplyDisplay(int resolutionIndex, int screenModeIndex)
    {
        Vector2Int size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        FullScreenMode mode = GetScreenMode(screenModeIndex);

        if (Screen.width != size.x || Screen.height != size.y || Screen.fullScreenMode != mode)
        {
            Screen.SetResolution(size.x, size.y, mode);
        }
    }


    private void ApplyAudio(int masterVolume, int bgmVolume, int sfxVolume)
    {
        AudioListener.volume = masterVolume / 100f;

        if (sfxMixer != null)
        {
            float bgm = Mathf.Max(bgmVolume / 100f, 0.0001f);
            float bgmDecibels = Mathf.Log10(bgm) * 20f;

            if (!sfxMixer.SetFloat("BGMVolume", bgmDecibels))
            {
                Debug.LogWarning("AudioMixer에서 BGMVolume 노출 파라미터를 찾을 수 없습니다");
            }

            float sfx = Mathf.Max(sfxVolume / 100f, 0.0001f);
            float sfxDecibels = Mathf.Log10(sfx) * 20f;

            if (!sfxMixer.SetFloat("SFXVolume", sfxDecibels))
            {
                Debug.LogWarning("AudioMixer에서 SFXVolume 노출 파라미터를 찾을 수 없습니다");
            }
        }
    }


    private void ApplySavedSettings()
    {
        if (resolutionDropdown != null && screenModeDropdown != null)
        {
            ApplyDisplay(resolutionDropdown.value, screenModeDropdown.value);
        }

        int masterVolume = Mathf.Clamp(PlayerPrefs.GetInt(MasterVolumeKey, 100), 0, 100);
        int bgmVolume = Mathf.Clamp(PlayerPrefs.GetInt(BGMVolumeKey, 100), 0, 100);
        int sfxVolume = Mathf.Clamp(PlayerPrefs.GetInt(SFXVolumeKey, 100), 0, 100);

        ApplyAudio(masterVolume, bgmVolume, sfxVolume);
    }

    public void Apply()
    {
        if (resolutionDropdown == null || screenModeDropdown == null || masterVolumeSlider == null || bgmVolumeSlider == null || sfxVolumeSlider == null)
        {
            return;
        }

        Vector2Int size = resolutions[resolutionDropdown.value];
        int screenMode = screenModeDropdown.value;
        int masterVolume = Mathf.RoundToInt(masterVolumeSlider.value);
        int bgmVolume = Mathf.RoundToInt(bgmVolumeSlider.value);
        int sfxVolume = Mathf.RoundToInt(sfxVolumeSlider.value);

        PlayerPrefs.SetInt(ResolutionWidthKey, size.x);
        PlayerPrefs.SetInt(ResolutionHeightKey, size.y);
        PlayerPrefs.SetInt(ScreenModeKey, screenMode);
        PlayerPrefs.SetInt(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetInt(BGMVolumeKey, bgmVolume);
        PlayerPrefs.SetInt(SFXVolumeKey, sfxVolume);
        PlayerPrefs.Save();

        ApplyDisplay(resolutionDropdown.value, screenMode);
        ApplyAudio(masterVolume, bgmVolume, sfxVolume);
    }
    public void Open()
    {
        if (settingsPanel == null || settingsPanel.activeSelf)
        {
            return;
        }

        if (returnPanel != null)
        {
            returnPanel.SetActive(false);
        }

        settingsPanel.SetActive(true);
        LoadSavedValuesIntoUI();
        SelectUI(firstSelected);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }


    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        settingsPanel.SetActive(false);

        if (returnPanel != null)
        {
            returnPanel.SetActive(true);
        }

        SelectUI(returnSelected);

        LastClosedFrame = Time.frameCount;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    public void CloseWithoutReturn()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        LastClosedFrame = Time.frameCount;
    }

    private void SelectUI(Selectable selectable)
    {
        if (selectable == null || EventSystem.current == null || !selectable.IsActive() || !selectable.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }
}
