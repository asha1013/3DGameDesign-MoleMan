using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private static PauseMenu instance;
    public static PauseMenu Instance => instance;

    [Header("Panels")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;

    [Header("Pause Menu Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button leaveButton;

    [Header("Settings Menu")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider mouseSensitivitySlider;
    public Button acceptButton;

    public bool isPaused = false;
    private bool inSettings = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Start()
    {
        // Update leave button text based on scene
        UpdateLeaveButtonText();

        // Ensure menus start closed
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Add slider listeners for real-time updates
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inSettings)
            {
                CloseSettings();
            }
            else if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void Pause()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;

        if (GameManager.instance != null && GameManager.instance.controller != null)
        {
           GameManager.instance.controller.controllerPaused = true;
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;  
        

        
    }

    public void Resume()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        inSettings = false;

        if (GameManager.instance != null && GameManager.instance.controller != null)
        {
          GameManager.instance.controller.controllerPaused = false;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; 
        

        
    }

    public void OpenSettings()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        inSettings = true;

        // Load current settings values into sliders (all stored as 0-1)
        if (Settings.Instance != null)
        {
            if (musicSlider != null) musicSlider.value = Settings.Instance.GetMusicVolume();
            if (sfxSlider != null) sfxSlider.value = Settings.Instance.GetSFXVolume();
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = Settings.Instance.GetMouseSensitivity();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        inSettings = false;
    }

    public void AcceptSettings()
    {
        // Send slider values to Settings
        if (Settings.Instance != null)
        {
            float music = musicSlider != null ? musicSlider.value : 0f;
            float sfx = sfxSlider != null ? sfxSlider.value : 0f;
            float mouseSens = mouseSensitivitySlider != null ? mouseSensitivitySlider.value : 1f;

            Settings.Instance.ApplySettings(music, sfx, mouseSens);
        }

        CloseSettings();
    }

    void UpdateLeaveButtonText()
    {
        if (leaveButton == null) return;

        Text buttonText = leaveButton.GetComponentInChildren<Text>();
        if (buttonText == null) return;

        // Check if in garden or dungeon
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Garden"))
        {
            buttonText.text = "Exit Game";
        }
        else if (sceneName.Contains("Dungeon"))
        {
            buttonText.text = "Leave Dungeon";
        }
        else
        {
            buttonText.text = "Main Menu";
        }
    }

    public void Leave()
    {
        Time.timeScale = 1f;
        isPaused = false;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Garden"))
        {
            Application.Quit();
        }
        else if (sceneName.Contains("Dungeon"))
        {
            SceneManager.LoadScene("Garden");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Real-time slider update callbacks
    void OnMusicVolumeChanged(float value)
    {
        if (Settings.Instance != null)
            Settings.Instance.SetMusicVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        if (Settings.Instance != null)
            Settings.Instance.SetSFXVolume(value);
    }

    void OnMouseSensitivityChanged(float value)
    {
        if (Settings.Instance != null)
            Settings.Instance.SetMouseSensitivity(value);
    }
}
