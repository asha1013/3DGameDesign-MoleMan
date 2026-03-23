using UnityEngine;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;

[System.Serializable]
public class WeaponMapping
{
    public Weapon weapon;
    public GameObject weaponObject;
}

public class PlayerState : MonoBehaviour
{
    [BoxGroup("Stats")][SerializeField] public int maxHP=6;
    [BoxGroup("Stats")][SerializeField] public int maxStamina=50;
    [BoxGroup("Stats")][SerializeField] public float baseStaminaRegen=2;
    [BoxGroup("Stats")][SerializeField] public float baseMoveSpeed=4;
    [BoxGroup("Stats")][SerializeField] public float gardenSpeed=2.5f;

    [BoxGroup("Weapon")] public Weapon equippedWeapon;
    [BoxGroup("Weapon")] public GameObject weaponObject;
    [BoxGroup("Weapon")] public Transform weaponParent;
    [BoxGroup("Weapon")] public Animator weaponAnimator;
    [BoxGroup("Weapon")] public List<WeaponMapping> weaponMappings = new List<WeaponMapping>();

    [BoxGroup("Weapon Stats")] public int damage = 1;
    [BoxGroup("Weapon Stats")] public int meleeDamage = 2;
    [BoxGroup("Weapon Stats")] public bool uniqueMelee = false;
    [BoxGroup("Weapon Stats")] public float meleeSpeed = 0.64f;
    [BoxGroup("Weapon Stats")] public bool uniqueRanged = false;
    [BoxGroup("Weapon Stats")] public float fireRate = 0.5f;
    [BoxGroup("Weapon Stats")] public float recoil = 0f;
    [BoxGroup("Weapon Stats")] public float projectileSpeed = 10f;
    [BoxGroup("Weapon Stats")] [Range(0.9f, 1f)] public float projectileRange = 1f;
    [BoxGroup("Weapon Stats")] public bool travelArc = false;
    [BoxGroup("Weapon Stats")] public float arcHeight = 2f;
    [BoxGroup("Weapon Stats")] public float arcRange = 10f;
    [BoxGroup("Weapon Stats")] public int penetration = 0;
    [BoxGroup("Weapon Stats")] public bool isAOE = false;
    [BoxGroup("Weapon Stats")] public float splashRadius = 2f;
    [BoxGroup("Weapon Stats")] public float knockback = 0f;

    [BoxGroup("State")]public int hp=10;
    [BoxGroup("State")] public float stamina=50;
    [BoxGroup("State")] public float staminaRegen;
    [BoxGroup("State")] public float currentSpeed;
    [BoxGroup("State")] public bool seeClearly = false;
    [BoxGroup("State")] private bool inHitInv;
    [BoxGroup("State")] private bool inGarden;
    [BoxGroup("State")] private bool isDead = false;

    [BoxGroup("Response")][SerializeField] float hitInvTime = 3f;
    [BoxGroup("Response")][SerializeField] GameObject dangerZone;
    [BoxGroup("Response")][SerializeField] Collider winCollider;
    [BoxGroup("Response")][SerializeField] MMF_Player damagedFeedback;
   

    [BoxGroup("Object References")]  GameObject camObj;
    [BoxGroup("Object References")]  GameObject playerObj;   

    [BoxGroup("Script References")]  GameManager gameManager;
    [BoxGroup("Script References")] Inventory inventoryScript;
    [BoxGroup("Script References")]  UIManager uiScript;
    [BoxGroup("Script References")]  PlayerController controller;
    [BoxGroup("Script References")]  PlayerAttack attackScript;
    [BoxGroup("Script References")] SUPERCharacter.SUPERCharacterAIO superController; 

    [BoxGroup("Visuals References")] Camera mainCam;
    [BoxGroup("Visuals References")][SerializeField] public Transform healthParent;
    [BoxGroup("Visuals References")][SerializeField] public GameObject fullHeartPrefab;
    [BoxGroup("Visuals References")][SerializeField] public GameObject emptyHeartPrefab;

    private List<GameObject> heartObjects = new List<GameObject>();

    [BoxGroup("Visuals References")][SerializeField] public MMProgressBar staminaBar;


    [BoxGroup("Audio References")] AudioMixer globalMixer;
    [BoxGroup("Audio References")] AudioMixer musicMixer;
    [BoxGroup("Audio References")] AudioLowPassFilter lowPass;
    [BoxGroup("Audio References")] AudioSource audioSource;


    [BoxGroup("Audio Clips")][SerializeField] AudioClip damagedClip;
    [BoxGroup("Audio Clips")][Range(0f, 1f)] public float damagedClipVolume = 1f;
    [BoxGroup("Audio Clips")][SerializeField] AudioClip healedClip;
    [BoxGroup("Audio Clips")][Range(0f, 1f)] public float healedClipVolume = 1f;

    [BoxGroup("Stats")] public int startingHP = 6;

    GameObject staminaBarObj;
    private Weapon previousWeapon;


    void Awake()
    {

        camObj = GameObject.FindGameObjectWithTag("MainCamera");
        playerObj = gameObject;

        if (camObj == null)
        {
            Debug.LogError("MainCamera not found");
            return;
        }

        mainCam = camObj.GetComponent<Camera>();
        gameManager = GameManager.Instance;
        inventoryScript = Inventory.Instance;
        controller = GetComponent<PlayerController>();
        attackScript = GetComponent<PlayerAttack>();
        superController = GetComponent<SUPERCharacter.SUPERCharacterAIO>();
        audioSource = GetComponent<AudioSource>();
        damagedFeedback = GetComponent<MMF_Player>();
        hp = maxHP;
        isDead = false;
        // initialize and sync stats to SUPER controller
        SyncStatsToController();

        // apply weapon stats and switch weapon object
        if (equippedWeapon != null)
        {
            ApplyWeaponStats(equippedWeapon);
            SwitchWeaponObject(equippedWeapon);
            if (attackScript != null) attackScript.RefreshChargeAttack();
            previousWeapon = equippedWeapon;
        }
    }
    void Start()
    {
        uiScript = GameManager.Instance.uiScript;

        if (uiScript.gameObject.transform.Find("Bars/StaminaBar").gameObject!=null) staminaBarObj = uiScript.gameObject.transform.Find("Bars/StaminaBar").gameObject;
        if (staminaBarObj != null) staminaBar = staminaBarObj.transform.Find("BarBG").GetComponent<MMProgressBar>();

        // Initialize health display
        UpdateHealthDisplay();
    }

    // sync stats from PlayerState to SUPER controller
    void SyncStatsToController()
    {
        if (superController == null) return;

        stamina = maxStamina;
        staminaRegen = baseStaminaRegen;
        currentSpeed = baseMoveSpeed;

        superController.Stamina = maxStamina;
        superController.currentStaminaLevel = stamina;
        superController.s_regenerationSpeed = staminaRegen;
        superController.walkingSpeed = currentSpeed * 55f;
        superController.sprintingSpeed = currentSpeed * 55f * 1.5f;

        if (inGarden)
        {
        superController.walkingSpeed = gardenSpeed * 55f;
        superController.sprintingSpeed = gardenSpeed * 55f * 1.5f;
        }
    }

    // call this when stats change (e.g. from upgrades)
    public void UpdateControllerStats()
    {
        if (superController == null) return;

        superController.Stamina = maxStamina;
        superController.s_regenerationSpeed = staminaRegen;
        superController.walkingSpeed = currentSpeed * 55f;
        superController.sprintingSpeed = currentSpeed * 55f * 1.5f;
    }

    public void ApplyWeaponStats(Weapon weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Weapon not assigned");
            return;
        }

        damage = weapon.baseDamage;
        meleeDamage = weapon.meleeDamage;
        uniqueMelee = weapon.uniqueMelee;
        meleeSpeed = weapon.meleeSpeed;
        uniqueRanged = weapon.uniqueRanged;
        fireRate = weapon.fireRate;      
        projectileSpeed = weapon.projectileSpeed;
        projectileRange = weapon.projectileRange;
        travelArc = weapon.travelArc;
        arcHeight = weapon.arcHeight;
        arcRange = weapon.arcRange;
        penetration = weapon.penetration;
        isAOE = weapon.isAOE;
        splashRadius = weapon.splashRadius;
        knockback = weapon.knockback;
    }

    void SwitchWeaponObject(Weapon weapon)
    {
        // Deactivate all weapon objects in the mapping list
        foreach (WeaponMapping mapping in weaponMappings)
        {
            if (mapping.weaponObject != null)
            {
                mapping.weaponObject.SetActive(false);
            }
        }

        // Activate the equipped weapon from the mapping list
        if (weapon != null)
        {
            WeaponMapping activeMapping = weaponMappings.Find(m => m.weapon == weapon);
            if (activeMapping != null && activeMapping.weaponObject != null)
            {
                activeMapping.weaponObject.SetActive(true);
                weaponObject = activeMapping.weaponObject;
                Debug.Log($"Activated weapon object: {weaponObject.name}");

                // Find and cache animator
                weaponAnimator = weaponObject.GetComponent<Animator>();
                if (weaponAnimator == null)
                {
                    weaponAnimator = weaponObject.GetComponentInChildren<Animator>();
                }
            }
            else
            {
                Debug.LogError($"No weapon mapping found for {weapon.name}. Add it to the Weapon Mappings list in PlayerState.");
            }
        }
    }

    void Update()
    {
        if (hp <= 0 && !isDead) Death();

       // #if UNITY_EDITOR
        // Debug weapon switching with number keys
        if (GameManager.Instance != null && GameManager.Instance.debugEnabled)
        {
            if (weaponMappings.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && weaponMappings.Count >= 1) equippedWeapon = weaponMappings[0].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha2) && weaponMappings.Count >= 2) equippedWeapon = weaponMappings[1].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha3) && weaponMappings.Count >= 3) equippedWeapon = weaponMappings[2].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha4) && weaponMappings.Count >= 4) equippedWeapon = weaponMappings[3].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha5) && weaponMappings.Count >= 5) equippedWeapon = weaponMappings[4].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha6) && weaponMappings.Count >= 6) equippedWeapon = weaponMappings[5].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha7) && weaponMappings.Count >= 7) equippedWeapon = weaponMappings[6].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha8) && weaponMappings.Count >= 8) equippedWeapon = weaponMappings[7].weapon;
                if (Input.GetKeyDown(KeyCode.Alpha9) && weaponMappings.Count >= 9) equippedWeapon = weaponMappings[8].weapon;
            }
        }
       // #endif

        // Check if weapon changed
        if (equippedWeapon != previousWeapon)
        {
            if (equippedWeapon != null)
            {
                ApplyWeaponStats(equippedWeapon);
                SwitchWeaponObject(equippedWeapon);
                if (attackScript != null) attackScript.RefreshChargeAttack();

                // Save equipped weapon to metaprogression
                if (Metaprogression.Instance != null)
                {
                    Metaprogression.Instance.equippedWeapon = equippedWeapon;
                    Metaprogression.Instance.SaveToFile();
                }
            }
            previousWeapon = equippedWeapon;
        }

        // sync stamina from controller to PlayerState and update UI every frame
        if (superController != null)
        {
            stamina = superController.currentStaminaLevel;
            if (uiScript != null) uiScript.UpdateStamina();
        }
    }
    

    // Player takes damage and starts invincibility coroutine (need to decide if enviornmental damage is treated differently)
    public void GetHit(int damage, bool isEnv)
    {
        if (!inHitInv)
        {
            if (damagedClip != null && audioSource != null) audioSource.PlayOneShot(damagedClip, damagedClipVolume);
            hp -= damage;
            UpdateHealthDisplay();
            if (hp <= maxHP/6) dangerZone.SetActive(true);
            StartCoroutine(HitInv());
        }
    }
    // Damage invincibility with feedback effects
    IEnumerator HitInv()
    {
        inHitInv=true;   
        if (damagedFeedback != null) damagedFeedback.PlayFeedbacks();
        yield return new WaitForSeconds(hitInvTime);
        inHitInv=false;
        // Lowpass off
        
    }
    
    // Visual indicator of damage invincibility


    void OnTriggerEnter(Collider other)
    {
        if (other == winCollider) Win();
    }

    void FixedUpdate()
    {
        /*
        // When under threshold
        if (hp < dangerzone) // red vignette
        else if // (red vignette on)  red vignette off
        */


    }

   public void UpdateHealthDisplay()
    {
        if (healthParent == null || fullHeartPrefab == null || emptyHeartPrefab == null) return;

        // Check if any heart objects are null (destroyed from scene change) and force rebuild
        bool hasNullHearts = heartObjects.Count > 0 && heartObjects.Exists(h => h == null);

        // Initialize hearts if empty, count doesn't match maxHP, or contains destroyed objects
        if (heartObjects.Count != maxHP || hasNullHearts)
        {
            // Clear existing
            foreach (GameObject heart in heartObjects)
            {
                if (heart != null) Destroy(heart);
            }
            heartObjects.Clear();

            // Create maxHP hearts (full up to current hp, rest empty)
            for (int i = 0; i < maxHP; i++)
            {
                GameObject prefabToUse = i < hp ? fullHeartPrefab : emptyHeartPrefab;
                GameObject heart = Instantiate(prefabToUse, healthParent);
                heartObjects.Add(heart);
            }
        }
        else
        {
            // Update existing hearts based on current hp
            for (int i = 0; i < maxHP; i++)
            {
                if (heartObjects[i] == null) continue;

                // Determine if this heart should be full or empty
                bool shouldBeFull = i < hp;

                // Check if current heart matches the correct prefab by comparing names
                string currentHeartPrefabName = heartObjects[i].name.Replace("(Clone)", "").Trim();
                string fullHeartPrefabName = fullHeartPrefab.name;
                string emptyHeartPrefabName = emptyHeartPrefab.name;

                bool isFull = currentHeartPrefabName == fullHeartPrefabName;

                // Replace if state doesn't match
                if (shouldBeFull != isFull)
                {
                    int siblingIndex = heartObjects[i].transform.GetSiblingIndex();
                    Destroy(heartObjects[i]);

                    GameObject prefabToUse = shouldBeFull ? fullHeartPrefab : emptyHeartPrefab;
                    GameObject newHeart = Instantiate(prefabToUse, healthParent);
                    newHeart.transform.SetSiblingIndex(siblingIndex);
                    heartObjects[i] = newHeart;
                }
            }
        }
    }

    void Death()
    {
        isDead = true;
        if (uiScript != null) uiScript.Death();
        if (inventoryScript != null) inventoryScript.CombineInventory(true);
    }
    void Win()
    {
        if (uiScript != null) uiScript.Win();
        if (inventoryScript != null) inventoryScript.CombineInventory(false);
    }

}
