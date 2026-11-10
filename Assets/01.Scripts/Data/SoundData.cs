using System;
using System.Collections.Generic;
using UnityEngine;

public enum SFXType
{
    UIHover,
    UIClick,
    UIOpen,
    UIClose,

    TerminalHover,
    TerminalClick,

    Confirm,
    Error,
    Notification,

    CCTVDetection,
    GuardDetection,
    SecurityAlert,

    DownloadProgress,
    DownloadComplete,

    MissionSuccess,
    MissionFail,

    Throw,
    ThrowImpact
}

public enum BGMType
{
    MainMenu,
    Stealth,
    Escape
}

[Serializable]
public class SFXData
{
    public SFXType sfxType;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

[Serializable]
public class BGMData
{
    public BGMType bgmType;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}

[CreateAssetMenu(fileName = "SoundData", menuName = "Silent Access/Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("BGM 목록")]
    public List<BGMData> bgmList = new List<BGMData>();

    [Header("SFX 목록")]
    public List<SFXData> sfxList = new List<SFXData>();
}
