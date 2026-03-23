using UnityEngine;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    private static Settings instance;
    public static Settings Instance => instance;


    public AudioMixer audioMixer;     
    private AudioMixerGroup musicGroup;
    private AudioMixerGroup sfxGroup;

    private float musicVolume;
    private float SFXVolume;
    private float mouseSensitivity;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        musicGroup = audioMixer.FindMatchingGroups("Music")[0];
        sfxGroup = audioMixer.FindMatchingGroups("Sfx")[0];
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        LoadSettingsFromSaveManager();
        ApplySettingsToAudioMixer();
        ApplyMouseSensitivityToController();
    }

    void Start()
    {
        LoadSettingsFromSaveManager();
        ApplySettingsToAudioMixer();
        ApplyMouseSensitivityToController();
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }

    void LoadSettingsFromSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
            musicVolume = saveData.musicVolume;
            SFXVolume = saveData.SFXVolume;
            mouseSensitivity = saveData.mouseSensitivity;
        }
    }

    void ApplySettingsToAudioMixer()
    {
        if (sfxGroup != null && musicGroup!=null)
        {
            // Convert 0-1 to dB range
            float musicDB = musicVolume <= 0.0001f ? -80f : Mathf.Log10(musicVolume) * 20;
            float sfxDB = SFXVolume <= 0.0001f ? -80f : Mathf.Log10(SFXVolume) * 20;

            audioMixer.SetFloat("MusicVolume", musicDB);
            audioMixer.SetFloat("SfxVolume", sfxDB);
        }
    }

    void ApplyMouseSensitivityToController()
    {
        if (GameManager.instance != null && GameManager.instance.controller != null)
        {
            float actualSensitivity = 1f + (mouseSensitivity * 11f);
            GameManager.instance.controller.Sensitivity = actualSensitivity;
        }
    }

    public void ApplySettings(float music, float sfx, float mouseSliderValue)
    {
        // Store values as 0-1 from sliders
        musicVolume = music;
        SFXVolume = sfx;
        mouseSensitivity = mouseSliderValue;

        ApplySettingsToAudioMixer();
        ApplyMouseSensitivityToController();
        SaveSettingsToSaveManager();
    }

    void SaveSettingsToSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveSettings(musicVolume, SFXVolume, mouseSensitivity);
        }
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => SFXVolume;
    public float GetMouseSensitivity() => mouseSensitivity;

    // Real-time setters for slider updates
    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        ApplySettingsToAudioMixer();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
        ApplySettingsToAudioMixer();
    }

    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = value;
        ApplyMouseSensitivityToController();
    }
}
