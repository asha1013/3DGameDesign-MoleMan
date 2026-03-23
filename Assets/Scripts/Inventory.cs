using UnityEngine;
using System;
using Unity.Properties;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using TMPro;
public class Inventory : MonoBehaviour
{
    private static Inventory instance;
    public static Inventory Instance => instance;

    public Dictionary<Item, int> dungeonInventory = new Dictionary<Item, int>(); // Temporary dungeon inventory
    public Dictionary<Item, int> persistentInventory = new Dictionary<Item, int>(); // Permanent inventory for Meta progression

    [Header("UI")]
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] GameObject inventorySlot;
    Dictionary<Item, GameObject> inventorySlots = new Dictionary<Item, GameObject>();

    [Header("Components")]
    AudioSource audioSource;

    [Header("Assets")]
    [SerializeField] AudioClip pickupClip;
    [UnityEngine.Range(0f, 1f)] public float pickupClipVolume = 1f;

    [Header("Debug Materials")]
    [SerializeField] Item myteMaterial;
    [SerializeField] Item spitflyMaterial;
    [SerializeField] Item gnomeMaterial;
    [SerializeField] Item frogMaterial;

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
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RefreshReferences();
        RefreshInventoryDisplay();
    }

    void Start()
    {
        RefreshReferences();
    }

    void RefreshReferences()
    {
        if (GetComponent<AudioSource>() != null) audioSource = GetComponent<AudioSource>();

        // Find inventory panel by name since DDOL breaks serialized references
        if (inventoryPanel == null)
        {
            GameObject foundPanel = GameObject.Find("InventoryPanel");
            if (foundPanel != null) inventoryPanel = foundPanel;
        }
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    void RefreshInventoryDisplay()
    {
        if (inventoryPanel == null) return;

        // Clear existing slots
        foreach (var slot in inventorySlots.Values)
        {
            if (slot != null) Destroy(slot);
        }
        inventorySlots.Clear();

        // Determine which inventory to display
        Dictionary<Item, int> activeInventory = null;
        if (GameManager.Instance != null && GameManager.Instance.inDungeon)
        {
            activeInventory = dungeonInventory;
        }
        else if (GameManager.Instance != null && GameManager.Instance.inGarden)
        {
            activeInventory = persistentInventory;
        }

        if (activeInventory == null) return;

        // Populate UI with active inventory
        foreach (var kvp in activeInventory)
        {
            Item item = kvp.Key;
            int quantity = kvp.Value;

            GameObject slot = Instantiate(inventorySlot, inventoryPanel.transform);
            inventorySlots[item] = slot;
            slot.SetActive(true);
            slot.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = item.itemIcon;
            slot.transform.Find("Quantity").GetComponent<TextMeshProUGUI>().text = quantity.ToString();
        }
    }

    public void AddItem(Item item, int quantity)
    {
        if (GameManager.Instance != null && GameManager.Instance.inDungeon)
        {
            // Add to dungeon inventory
            if (dungeonInventory.ContainsKey(item))
            {
                dungeonInventory[item] += quantity;
            }
            else dungeonInventory[item] = quantity;
        }
        else if (GameManager.Instance != null && GameManager.Instance.inGarden)
        {
            // Add to persistent inventory
            if (persistentInventory.ContainsKey(item))
            {
                persistentInventory[item] += quantity;
            }
            else persistentInventory[item] = quantity;
        }

        RefreshInventoryDisplay();

        if (audioSource != null && pickupClip != null) audioSource.PlayOneShot(pickupClip, pickupClipVolume);
    }

    public void AddPersistentItem(Item item, int quantity)
    {
        if (persistentInventory.ContainsKey(item))
        {
            persistentInventory[item] += quantity;
        }
        else persistentInventory[item] = quantity;

        RefreshInventoryDisplay();
    }

    void Update()
    {
        if (inventoryPanel == null) return;

        // Don't allow inventory in main menu
        bool canOpenInventory = GameManager.Instance != null &&
                                (GameManager.Instance.inDungeon || GameManager.Instance.inGarden);

        if (!canOpenInventory) return;

        if (Input.GetKeyDown(KeyCode.Tab) && PauseMenu.Instance != null && !PauseMenu.Instance.isPaused)
        {
            inventoryPanel.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            inventoryPanel.SetActive(false);
        }

        // Debug material commands
        if (GameManager.Instance != null && GameManager.Instance.debugEnabled)
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && myteMaterial != null)
                {
                    AddPersistentItem(myteMaterial, 1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) && spitflyMaterial != null)
                {
                    AddPersistentItem(spitflyMaterial, 1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) && gnomeMaterial != null)
                {
                    AddPersistentItem(gnomeMaterial, 1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4) && frogMaterial != null)
                {
                    AddPersistentItem(frogMaterial, 1);
                }
            }
        }
    }

    public void CombineInventory(bool won)
    {
        // Transfer materials from dungeon inventory to persistent inventory
        foreach (var kvp in dungeonInventory)
        {
            Item item = kvp.Key;
            int quantity = kvp.Value;

            // Only transfer materials
            if (item.isMaterial && quantity > 0)
            {
                int transferQuantity = quantity;

                // If didn't win, halve the quantity (rounded up)
                if (!won)
                {
                    transferQuantity = Mathf.CeilToInt(quantity / 2f);
                }

                // Add to persistent inventory
                if (persistentInventory.ContainsKey(item))
                {
                    persistentInventory[item] += transferQuantity;
                }
                else
                {
                    persistentInventory[item] = transferQuantity;
                }
            }
        }

        // Clear dungeon inventory
        dungeonInventory.Clear();
    }

    public bool HasItem(Item item, int quantity)
    {
        if (persistentInventory.ContainsKey(item))
        {
            return persistentInventory[item] >= quantity;
        }
        return false;
    }

    public void RemoveItem(Item item, int quantity)
    {
        if (persistentInventory.ContainsKey(item))
        {
            persistentInventory[item] -= quantity;
            if (persistentInventory[item] <= 0)
            {
                persistentInventory.Remove(item);
            }
            RefreshInventoryDisplay();
        }
    }

}
