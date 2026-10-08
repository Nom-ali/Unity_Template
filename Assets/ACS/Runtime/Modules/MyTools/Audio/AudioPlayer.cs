using ACS.SaveManager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioPlayer : Singleton<AudioPlayer>
{
    
    [Header("Settings")]
    public bool playMusicAtStart = true;
    public float initialDelay = 0f;

    [Header("Audio References")]
    public AudioSettings musicSettings;
    public AudioSettings sfxSettings;

    // Internal tracking
    private Dictionary<string, AudioSource> activeSources = new Dictionary<string, AudioSource>();
    private Dictionary<string, float> sfxVolumeMultipliers = new Dictionary<string, float>();
    private Coroutine fadeCoroutine;

    public float MusicVolume => musicSettings != null ? musicSettings.volume : 0f;
    public float SFXVolume => sfxSettings != null ? sfxSettings.volume : 0f;

    private static readonly string KeyDefaultsSet = SharedVariables.DefaultSetting;
    private static readonly string KeyMusicVolume = SharedVariables.MusicVolume;
    private static readonly string KeySFXVolume = SharedVariables.SoundVolume;
    private static readonly string KeyMusicEnabled = SharedVariables.Music;
    private static readonly string KeySFXEnabled = SharedVariables.Sound;
    private static readonly string KeyVibrationEnabled = SharedVariables.Vibration;

    #region Singleton Setup

    protected override void Awake()
    {
        base.Awake();
        LoadSettings();
    }

    #endregion

    private void Start()
    {
        if (playMusicAtStart)
        {
            StartCoroutine(PlayMusicWithDelay());
        }
    }

    private IEnumerator PlayMusicWithDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        PlayBGMusic();
    }

    #region Settings Management
    private void LoadSettings()
    {
        // First time setup
        if (!SaveManager.Prefs.HasKey(KeyDefaultsSet))
        {
            SetDefaultSettings();
        }

        // Load saved settings
        musicSettings.volume = Mathf.Clamp01(SaveManager.Prefs.GetFloat(KeyMusicVolume, 1f));
        sfxSettings.volume = Mathf.Clamp01(SaveManager.Prefs.GetFloat(KeySFXVolume, 1f));
        musicSettings.enabled = SaveManager.Prefs.GetBool(KeyMusicEnabled, true);
        sfxSettings.enabled = SaveManager.Prefs.GetBool(KeySFXEnabled, true);
        VibrationEnabled = SaveManager.Prefs.GetBool(KeyVibrationEnabled, true);
    }

    private void SetDefaultSettings()
    {
        SaveManager.Prefs.SetFloat(KeyMusicVolume, 0.7f);
        SaveManager.Prefs.SetFloat(KeySFXVolume, 1f);
        SaveManager.Prefs.SetBool(KeyMusicEnabled, true);
        SaveManager.Prefs.SetBool(KeySFXEnabled, true);
        SaveManager.Prefs.SetBool(KeyVibrationEnabled, true);
        SaveManager.Prefs.SetBool(KeyDefaultsSet, true);
        SaveManager.Prefs.Save();
    }

    public void SaveSettings()
    {
        SaveManager.Prefs.SetFloat(KeyMusicVolume, musicSettings.volume);
        SaveManager.Prefs.SetFloat(KeySFXVolume, sfxSettings.volume);
        SaveManager.Prefs.SetBool(KeyMusicEnabled, musicSettings.enabled);
        SaveManager.Prefs.SetBool(KeySFXEnabled, sfxSettings.enabled);
        SaveManager.Prefs.SetBool(KeyVibrationEnabled, VibrationEnabled);
        SaveManager.Prefs.Save();
    }
    #endregion

    #region Music Controls
    public void PlayBGMusic()
    {
        PlayMusic(AudioType.MainMenu, false);
    }

    public void PlayBGMusic(bool random)
    {
        PlayMusic(AudioType.MainMenu, true);
    }

    public void PlayMusic(AudioType audioType, bool random = false, float fadeInTime = 1f)
    {
        if (!musicSettings.enabled || audioType == AudioType.None)
            return;

        // Create or get music source
        AudioSource musicSource = GetOrCreateAudioSource("Music", true);
        if (musicSource != null && musicSource.isPlaying)
            return;

        AudioClipData clipData = musicSettings.GetClipById(audioType);
        if (clipData == null)
        {
            Debug.LogWarning($"Music clip with ID '{audioType}' not found!");
            return;
        }

        // Stop any existing music
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }


        AudioClip selectedClip = clipData.clip;
        if (random == true && clipData.variations != null && clipData.variations.Length > 0)
        {
            int randomIndex = Random.Range(0, clipData.variations.Length);
            selectedClip = clipData.variations[randomIndex];
        }

        // Set music properties
        musicSource.clip = selectedClip;
        musicSource.volume = 0f;
        musicSource.loop = clipData.loop;
        musicSource.priority = 0;

        // Start playing and fade in
        musicSource.Play();
        fadeCoroutine = StartCoroutine(FadeMusicVolume(musicSource, 0f, musicSettings.volume, fadeInTime));
        //StartCoroutine(PlayNextMusic(musicSource.clip, selectedClip.length));
    }

    IEnumerator PlayNextMusic(AudioClip oldClip, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioClipData clipData = musicSettings.GetClipById(AudioType.MainMenu);
        if (clipData == null)
        {
            Debug.LogError($"Music clip with ID '{AudioType.MainMenu}' not found!");
            yield break;
        }
        // Create or get music source
        AudioSource musicSource = GetOrCreateAudioSource("Music", true);

        AudioClip selectedClip = oldClip;
        if (clipData.variations != null && clipData.variations.Length > 0)
        {
            List<AudioClip> availableClips = clipData.variations
             .Where(clip => clip != oldClip).ToList();

            int randomIndex = Random.Range(0, clipData.variations.Length);
            selectedClip = availableClips[Random.Range(0, availableClips.Count)];
        }

        // Set music properties
        musicSource.clip = selectedClip;
        musicSource.volume = 0f;
        musicSource.loop = clipData.loop;
        musicSource.priority = 0;
        // Start playing and fade in
        musicSource.Play();
        StartCoroutine(FadeMusicVolume(musicSource, 0f, musicSettings.volume, 1f));
        //StartCoroutine(PlayNextMusic(musicSource.clip, musicSource.clip.length));
    }

    public void StopMusic(float fadeOutTime = 1f)
    {
        if (!activeSources.TryGetValue("Music", out AudioSource musicSource) || !musicSource || !musicSource.isPlaying)
            return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeMusicVolume(musicSource, musicSource.volume, 0f, fadeOutTime, true));
    }

    private IEnumerator FadeMusicVolume(AudioSource source, float startVolume, float targetVolume, float duration, bool stopAfterFade = false)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            source.volume = Mathf.Lerp(startVolume, targetVolume, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade && Mathf.Approximately(targetVolume, 0f))
        {
            source.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicSettings.volume = Mathf.Clamp01(volume);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (activeSources.TryGetValue("Music", out AudioSource musicSource) && musicSource)
            musicSource.volume = musicSettings.volume;

        SaveSettings();
    }

    public void ToggleMusic(bool enabled)
    {
        musicSettings.enabled = enabled;

        if (enabled)
        {
            if (!activeSources.TryGetValue("Music", out AudioSource musicSource) || !musicSource || !musicSource.isPlaying)
                PlayBGMusic();
        }
        else
        {
            StopMusic();
        }

        SaveSettings();
    }
    #endregion

    #region SFX Controls

    public void PlayButtonSound()
    {
        PlayOneShot(AudioType.Button);
    }

    public void PlayOneShot(AudioType audioType)
    {
        if (!sfxSettings.enabled || audioType == AudioType.None)
            return;

        AudioClipData clipData = sfxSettings.GetClipById(audioType);
        if (clipData == null)
        {
            Debug.LogWarning($"SFX clip with ID '{audioType}' not found!");
            return;
        }

        // Create random name to allow multiple instances of the same sound
        string sourceName = $"SFX_{audioType}_{Random.Range(0, 10000)}";
        AudioSource sfxSource = CreateAudioSource(sourceName);

        // Set properties
        AudioClip selectedClip = clipData.clip;
        if (clipData.variations != null && clipData.variations.Length > 0)
        {
            int randomIndex = Random.Range(0, clipData.variations.Length);
            selectedClip = clipData.variations[randomIndex];
        }

        sfxSource.clip = selectedClip;
        SetSFXSourceVolume(sourceName, sfxSource, clipData.volumeMultiplier);
        sfxSource.loop = false;

        // Play and cleanup
        sfxSource.Play();
        StartCoroutine(ReleaseAudioSourceWhenFinished(sourceName, selectedClip.length));
    }
    
    public void PlayOneShot(AudioType audioType, int index)
    {
        if (!sfxSettings.enabled || audioType == AudioType.None)
            return;

        AudioClipData clipData = sfxSettings.GetClipById(audioType);
        if (clipData == null)
        {
            Debug.LogWarning($"SFX clip with ID '{audioType}' not found!");
            return;
        }

        // Create random name to allow multiple instances of the same sound
        string sourceName = $"SFX_{audioType}_{Random.Range(0, 10000)}";
        AudioSource sfxSource = CreateAudioSource(sourceName);

        // Set properties
        AudioClip selectedClip = clipData.clip;
        if (clipData.variations != null && clipData.variations.Length > 0)
        {
            selectedClip = clipData.variations[index];
        }

        sfxSource.clip = selectedClip;
        SetSFXSourceVolume(sourceName, sfxSource, clipData.volumeMultiplier);
        sfxSource.loop = false;

        // Play and cleanup
        sfxSource.Play();
        StartCoroutine(ReleaseAudioSourceWhenFinished(sourceName, selectedClip.length));
    }

    public void PlayOneShotAtPosition(AudioType audioType, Vector3 position)
    {
        if (!sfxSettings.enabled || audioType == AudioType.None)
            return;

        AudioClipData clipData = sfxSettings.GetClipById(audioType);
        if (clipData == null)
        {
            Debug.LogWarning($"SFX clip with ID '{audioType}' not found!");
            return;
        }

        // Create random name to allow multiple instances of the same sound
        string sourceName = $"SFX_{audioType}_{Random.Range(0, 10000)}";
        AudioSource sfxSource = CreateAudioSource(sourceName);
        sfxSource.transform.position = position;

        // Set properties
        AudioClip selectedClip = clipData.clip;
        if (clipData.variations != null && clipData.variations.Length > 0)
        {
            int randomIndex = Random.Range(0, clipData.variations.Length);
            selectedClip = clipData.variations[randomIndex];
        }

        sfxSource.clip = selectedClip;
        SetSFXSourceVolume(sourceName, sfxSource, clipData.volumeMultiplier);
        sfxSource.loop = false;
        sfxSource.spatialBlend = 1f; // Full 3D
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 1f;
        sfxSource.maxDistance = 20f;

        // Play and cleanup
        sfxSource.Play();
        StartCoroutine(ReleaseAudioSourceWhenFinished(sourceName, selectedClip.length));
    }

    private IEnumerator ReleaseAudioSourceWhenFinished(string sourceName, float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f);

        if (activeSources.TryGetValue(sourceName, out AudioSource source))
        {
            activeSources.Remove(sourceName);
            sfxVolumeMultipliers.Remove(sourceName);
            Destroy(source.gameObject);
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxSettings.volume = Mathf.Clamp01(volume);
        UpdateActiveSFXVolumes();
        SaveSettings();
    }

    public void ToggleSound(bool enabled)
    {
        sfxSettings.enabled = enabled;
        if (enabled)
        {
            PlayButtonSound();
        }
        else
        {
            StopActiveSFX();
        }
        SaveSettings();
    }

    private void SetSFXSourceVolume(string sourceName, AudioSource source, float volumeMultiplier)
    {
        sfxVolumeMultipliers[sourceName] = volumeMultiplier;
        source.volume = sfxSettings.volume * volumeMultiplier;
    }

    private void UpdateActiveSFXVolumes()
    {
        foreach (KeyValuePair<string, AudioSource> pair in activeSources)
        {
            if (pair.Key.StartsWith("SFX_") && pair.Value && pair.Value.isPlaying &&
                sfxVolumeMultipliers.TryGetValue(pair.Key, out float volumeMultiplier))
            {
                pair.Value.volume = sfxSettings.volume * volumeMultiplier;
            }
        }
    }

    private void StopActiveSFX()
    {
        foreach (KeyValuePair<string, AudioSource> pair in activeSources)
        {
            if (pair.Key.StartsWith("SFX_") && pair.Value)
                pair.Value.Stop();
        }

        UnloadUnusedSources();
    }
    #endregion

    #region Vibration Controls
    public bool VibrationEnabled { get; private set; } = true;

    public void ToggleVibration(bool enabled)
    {
        VibrationEnabled = enabled;
        SaveSettings();
    }

    public void Vibrate()
    {
        if (!VibrationEnabled)
            return;
        Vibrate(VibrationType.Medium); // Unity's built-in vibration
        // For more advanced vibration, you could integrate with a plugin
    }

    public void Vibrate(VibrationType type)
    {
        if (!VibrationEnabled)
            return;

        switch (type)
        {
            case VibrationType.Light:
                Handheld.Vibrate(); // Unity's built-in vibration
                // For more advanced vibration, you could integrate with a plugin
                break;

            case VibrationType.Medium:
                Handheld.Vibrate();
                break;

            case VibrationType.Heavy:
                Handheld.Vibrate();
                break;
        }
    }
    #endregion

    #region Audio Source Management
    private AudioSource GetOrCreateAudioSource(string name, bool persist = false)
    {
        // Check if source already exists
        if (activeSources.TryGetValue(name, out AudioSource existingSource))
        {
            return existingSource;
        }

        // Create new source
        return CreateAudioSource(name, persist);
    }

    private AudioSource CreateAudioSource(string name, bool persist = false)
    {
        GameObject sourceObj = new GameObject($"AudioSource_{name}");
        sourceObj.transform.SetParent(transform);

        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        if (persist)
        {
            activeSources[name] = source;
        }
        else
        {
            activeSources[name] = source;
        }

        return source;
    }

    public void UnloadUnusedSources()
    {
        List<string> sourcesToRemove = new List<string>();

        foreach (var pair in activeSources)
        {
            if (pair.Key != "Music" && !pair.Value.isPlaying)
            {
                sourcesToRemove.Add(pair.Key);
                Destroy(pair.Value.gameObject);
            }
        }

        foreach (string key in sourcesToRemove)
        {
            activeSources.Remove(key);
            sfxVolumeMultipliers.Remove(key);
        }
    }
    #endregion
}

public enum VibrationType
{
    Light,
    Medium,
    Heavy
}


[System.Serializable]
public enum AudioType
{
    None,
    Button,
    Coins,
    GamePlay,
    MainMenu,
    LevelComplete,
    LevelFail,
    Droppable,
    PickUps,
    Died,
    Effects,
    Pop,
    Typing, Buy, Reward, Shoot, Obstacle1, Obstacle2,
    BoxFall1, 
    BoxFall2, 
    BoxFall3, 
}



