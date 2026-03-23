using UnityEngine;

public class Metaprogression : MonoBehaviour
{
    private static Metaprogression instance;
    public static Metaprogression Instance => instance;

    public bool myteUnlocked;
    public bool gnomeUnlocked;
    public bool flyUnlocked;
    public bool frogUnlocked;
    public int myteProgress;
    public int gnomeProgress;
    public int flyProgress;
    public int frogProgress;
    public int playerMaxHP;
    public bool seeClearly = false;
    public Weapon equippedWeapon;
    public int totalWins;
    public int totalDeaths;

    public string[] plotMushroomNames = new string[4];
    public int[] plotProgress = new int[4];
    public bool[] plotCompleted = new bool[4];

    void Awake()
    {
        if (instance != null && instance != this)
        {
            return;
        }
        instance = this;

        LoadFromSave();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ApplyToGame();
    }

    public void LoadFromSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveData data = SaveManager.Instance.GetCurrentSaveData();
            myteUnlocked = data.myteUnlocked;
            gnomeUnlocked = data.gnomeUnlocked;
            flyUnlocked = data.flyUnlocked;
            frogUnlocked = data.frogUnlocked;
            myteProgress = data.myteProgress;
            gnomeProgress = data.gnomeProgress;
            flyProgress = data.flyProgress;
            frogProgress = data.frogProgress;
            playerMaxHP = data.playerMaxHP;
            seeClearly = data.seeClearly;

            // Load equipped weapon by name
            if (!string.IsNullOrEmpty(data.equippedWeaponName))
            {
                equippedWeapon = Resources.Load<Weapon>("Weapons/" + data.equippedWeaponName);
            }

            totalWins = data.totalWins;
            totalDeaths = data.totalDeaths;

            plotMushroomNames = data.plotMushroomNames ?? new string[4];
            plotProgress = data.plotProgress ?? new int[4];
            plotCompleted = data.plotCompleted ?? new bool[4];
        }
    }

    public void SaveToFile()
    {
        if (SaveManager.Instance != null)
        {
            SaveData data = SaveManager.Instance.GetCurrentSaveData();
            data.myteUnlocked = myteUnlocked;
            data.gnomeUnlocked = gnomeUnlocked;
            data.flyUnlocked = flyUnlocked;
            data.frogUnlocked = frogUnlocked;
            data.myteProgress = myteProgress;
            data.gnomeProgress = gnomeProgress;
            data.flyProgress = flyProgress;
            data.frogProgress = frogProgress;
            data.playerMaxHP = playerMaxHP;
            data.seeClearly = seeClearly;

            // Save equipped weapon name
            data.equippedWeaponName = equippedWeapon != null ? equippedWeapon.weaponName : "";

            data.totalWins = totalWins;
            data.totalDeaths = totalDeaths;

            data.plotMushroomNames = plotMushroomNames;
            data.plotProgress = plotProgress;
            data.plotCompleted = plotCompleted;
        }
    }

    public void ApplyToGame()
    {
        if (GameManager.instance != null && GameManager.instance.playerObj != null)
        {
            PlayerState playerState = GameManager.instance.playerObj.GetComponent<PlayerState>();
            if (playerState != null)
            {
                playerState.maxHP = playerMaxHP;
                playerState.hp = playerMaxHP;
                playerState.seeClearly = seeClearly;

                // Apply equipped weapon
                if (equippedWeapon != null)
                {
                    playerState.equippedWeapon = equippedWeapon;
                }

                playerState.UpdateHealthDisplay();
            }
        }

        // Apply fog settings based on seeClearly
        if (GameManager.instance != null && (GameManager.instance.inGarden || GameManager.instance.inDungeon))
        {
            RenderSettings.fog = !seeClearly;
        }
    }
}
