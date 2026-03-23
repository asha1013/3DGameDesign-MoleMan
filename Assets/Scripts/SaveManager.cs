using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    private static SaveManager instance;
    public static SaveManager Instance => instance;

    private string saveFilePath;
    private SaveData currentSaveData;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Application.persistentDataPath + "/savedata.json";
        LoadOrCreateSaveFile();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SaveToFile();
            instance = null;
        }
    }

    void LoadOrCreateSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            currentSaveData = new SaveData
            {
                // Settings defaults
                musicVolume = 1f,
                SFXVolume = 1f,
                mouseSensitivity = 0.5f,

                // Game data defaults
                myteUnlocked = false,
                gnomeUnlocked = false,
                flyUnlocked = false,
                frogUnlocked = false,
                myteProgress = 0,
                gnomeProgress = 0,
                flyProgress = 0,
                frogProgress = 0,
                playerMaxHP = 6,
                totalWins = 0,
                totalDeaths = 0,
                persistentInventoryItemNames = new string[0],
                persistentInventoryQuantities = new int[0]
            };
            SaveToFile();
        }
    }

    void SaveToFile()
    {
        if (currentSaveData == null) return;
        string json = JsonUtility.ToJson(currentSaveData, true);
        File.WriteAllText(saveFilePath, json);
    }

    // Settings methods
    public void SaveSettings(float musicVol, float sfxVol, float mouseSens)
    {
        currentSaveData.musicVolume = musicVol;
        currentSaveData.SFXVolume = sfxVol;
        currentSaveData.mouseSensitivity = mouseSens;
        SaveToFile();
    }

    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    // Game data methods
    public void SavePersistentInventory(Dictionary<Item, int> inventory)
    {
        if (inventory == null || inventory.Count == 0)
        {
            currentSaveData.persistentInventoryItemNames = new string[0];
            currentSaveData.persistentInventoryQuantities = new int[0];
        }
        else
        {
            currentSaveData.persistentInventoryItemNames = inventory.Keys.Select(item => item.itemName).ToArray();
            currentSaveData.persistentInventoryQuantities = inventory.Values.ToArray();
        }
        SaveToFile();
    }

    public Dictionary<string, int> LoadPersistentInventory()
    {
        Dictionary<string, int> inventory = new Dictionary<string, int>();

        if (currentSaveData.persistentInventoryItemNames != null &&
            currentSaveData.persistentInventoryQuantities != null)
        {
            for (int i = 0; i < currentSaveData.persistentInventoryItemNames.Length; i++)
            {
                if (i < currentSaveData.persistentInventoryQuantities.Length)
                {
                    inventory[currentSaveData.persistentInventoryItemNames[i]] = currentSaveData.persistentInventoryQuantities[i];
                }
            }
        }

        return inventory;
    }

    public void SaveGameData(
        bool myteUnlocked, bool gnomeUnlocked, bool flyUnlocked, bool frogUnlocked,
        int myteProgress, int gnomeProgress, int flyProgress, int frogProgress,
        int playerMaxHP, int totalWins, int totalDeaths)
    {
        currentSaveData.myteUnlocked = myteUnlocked;
        currentSaveData.gnomeUnlocked = gnomeUnlocked;
        currentSaveData.flyUnlocked = flyUnlocked;
        currentSaveData.frogUnlocked = frogUnlocked;
        currentSaveData.myteProgress = myteProgress;
        currentSaveData.gnomeProgress = gnomeProgress;
        currentSaveData.flyProgress = flyProgress;
        currentSaveData.frogProgress = frogProgress;
        currentSaveData.playerMaxHP = playerMaxHP;
        currentSaveData.totalWins = totalWins;
        currentSaveData.totalDeaths = totalDeaths;
        SaveToFile();
    }

    public SaveData LoadGameData()
    {
        return currentSaveData;
    }
}
