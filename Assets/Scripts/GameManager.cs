using UnityEngine;
using NaughtyAttributes;
using Unity.Properties;
using System.Collections;
using Unity.VisualScripting;
using Unity.AppUI.UI;
using UnityEngine.Audio;
using SUPERCharacter;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameManager Instance => instance;

    [BoxGroup("Object References")] public GameObject camObj;
    [BoxGroup("Object References")] public GameObject playerObj;
    [BoxGroup("Object References")] public GameObject gmObj;

    [BoxGroup("Script References")] public PlayerState playerState;
    [BoxGroup("Script References")] public Inventory inventoryScript;
    [BoxGroup("Script References")] public UIManager uiScript;
    [BoxGroup("Script References")] public SUPERCharacterAIO controller;
    [BoxGroup("Script References")] public MusicPlayer musicPlayer;
    [BoxGroup("Script References")] public SceneTransitionManager sceneManager;
     [BoxGroup("Script References")] public MinimapManager minimapManager;



    [BoxGroup("Visuals References")] public Camera mainCam;

    [BoxGroup("Audio References")] public AudioMixerGroup sfxsMixer;
    [BoxGroup("Audio References")] public AudioMixerGroup musicMixer;
    [BoxGroup("Audio References")] public AudioLowPassFilter lowPass;

    [BoxGroup("Audio Clips")] public AudioClip damagedSFX;
    [BoxGroup("Audio Clips")] public AudioClip healedSFX;

    [BoxGroup("State Bools")] public bool inGarden;
    [BoxGroup("State Bools")] public bool inDungeon;

    [BoxGroup("Debug")]
    #if UNITY_EDITOR
    public bool debugEnabled = true;
    #else
    public bool debugEnabled = false;
    #endif
    [BoxGroup("Debug")] public GameObject debugText;



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

        RefreshReferences();
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UpdateSceneState(scene);
        RefreshReferences();
    }

    void UpdateSceneState(UnityEngine.SceneManagement.Scene scene)
    {
        string sceneName = scene.name.ToLower();

        if (sceneName.Contains("garden"))
        {
            inGarden = true;
            inDungeon = false;
        }
        else if (sceneName.Contains("dungeon"))
        {
            inGarden = false;
            inDungeon = true;
        }
        else if (sceneName.Contains("mainmenu") || sceneName.Contains("menu"))
        {
            inGarden = false;
            inDungeon = false;
        }
    }

    void RefreshReferences()
    {
        gmObj = gameObject;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerObj = player;
            playerState = playerObj.GetComponent<PlayerState>();
            controller = playerObj.GetComponent<SUPERCharacterAIO>();
        }

        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null)
        {
            camObj = camera;
            mainCam = camObj.GetComponent<Camera>();
        }

        uiScript = this.transform.Find("UI").GetComponent<UIManager>();
        inventoryScript = GetComponent<Inventory>();
        musicPlayer = GetComponent<MusicPlayer>();
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        #if !UNITY_EDITOR
        // Debug toggle: F+U+N+G+I pressed simultaneously
        if (Input.GetKey(KeyCode.F) && Input.GetKey(KeyCode.U) &&
            Input.GetKey(KeyCode.N) && Input.GetKey(KeyCode.G) &&
            Input.GetKeyDown(KeyCode.I))
        {
            debugEnabled = !debugEnabled;
            if (debugText != null) debugText.SetActive(debugEnabled);
        }
        #endif
    }
}
