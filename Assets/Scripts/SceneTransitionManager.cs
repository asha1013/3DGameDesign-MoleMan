using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance => instance;
    public float transitionWait;
    public float fadeTime=1f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void LoadGardenFromMainMenu()
    {
        // Set state bools
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inGarden = true;
            GameManager.Instance.inDungeon = false;
        }

        // Load Garden scene (index 1)
        StartCoroutine(SceneLoad(1,false));
    }

    public void LoadDungeonFromGarden()
    {
        // Set state bools
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inGarden = false;
            GameManager.Instance.inDungeon = true;
        }

        // Save persistent inventory before entering dungeon
        if (SaveManager.Instance != null && Inventory.Instance != null)
        {
            SaveManager.Instance.SavePersistentInventory(Inventory.Instance.persistentInventory);
        }

        // Load Dungeon scene (index 2)
        StartCoroutine(SceneLoad(2));
    }

    public void LoadGardenFromDungeon()
    {
        // Set state bools
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inGarden = true;
            GameManager.Instance.inDungeon = false;
        }

        // Transfer dungeon materials to persistent inventory
        if (Inventory.Instance != null)
        {
            Inventory.Instance.CombineInventory(false);
        }

        // Save persistent inventory after dungeon run
        if (SaveManager.Instance != null && Inventory.Instance != null)
        {
            SaveManager.Instance.SavePersistentInventory(Inventory.Instance.persistentInventory);
        }

        // Load Garden scene (index 1)
        StartCoroutine(SceneLoad(1));
    }

    private float sceneLoadedTime = 0f;

    public void Update()
    {
        // Only allow auto-load from main menu (scene 0) and after a delay to prevent issues during scene load
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0
            && Input.anyKeyDown
            && Time.time > sceneLoadedTime + 0.5f)
        {
            LoadGardenFromMainMenu();
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedEvent;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedEvent;
    }

    void OnSceneLoadedEvent(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        sceneLoadedTime = Time.time;
    }

    public IEnumerator SceneLoad(int index, bool musicReset = true)
    {
        StartCoroutine(GameManager.instance.uiScript.ScreenFade(true));
        if (musicReset)StartCoroutine(GameManager.instance.musicPlayer.FadeOut(fadeTime));
        yield return new WaitForSeconds(fadeTime);
        UnityEngine.SceneManagement.SceneManager.LoadScene(index);
        yield return new WaitForSeconds(transitionWait);
        StartCoroutine(GameManager.instance.uiScript.ScreenFade(false));
        if (musicReset)StartCoroutine(GameManager.instance.musicPlayer.FadeIn(index,fadeTime,true));
    }
}
