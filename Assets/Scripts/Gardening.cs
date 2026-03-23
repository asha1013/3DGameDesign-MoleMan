using UnityEngine;
using TMPro;

public class Gardening : MonoBehaviour
{
    private static bool playerLookingAtAnyPlot = false;
    private static System.Collections.Generic.HashSet<string> currentlyAssignedMushrooms = new System.Collections.Generic.HashSet<string>();

    public int plotIndex;
    public Material defaultMushroomMat;
    public float interactionDistance = 3f;
    public Shroom[] availableMushrooms;

    [Header("Child References")]
    public Light beamLight;
    public MeshRenderer mushroomRenderer;
    public GameObject rewardObject;
    public TextMeshPro progressText;
    public GameObject poof;

    [Header("Audio")]
    AudioSource audioSource;
    public AudioClip assignClip;
    [Range(0f, 1f)] public float assignVolume = 1f;
    public AudioClip feedClip;
    [Range(0f, 1f)] public float feedVolume = 1f;
    public AudioClip completeClip;
    [Range(0f, 1f)] public float completeVolume = 1f;

    [Header("Feed UI")]
    public GameObject feedPanel;
    public UnityEngine.UI.Image feedIcon;
    public GameObject rewardViewPanel;

    [Header("Colliders")]
    public Collider mushroomCollider;
    public Collider rewardCollider;

    private Shroom assignedMushroom;
    private int currentProgress;
    private bool isAssigned;
    private bool isGrowing;
    private bool isCompleted;
    private GameObject player;
    private Inventory playerInventory;

     bool inRanged=false;

    void Start()
    {
        audioSource=GetComponent<AudioSource>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerInventory = Inventory.Instance;

        // Auto-assign colliders if not set
        if (mushroomCollider == null && mushroomRenderer != null)
            mushroomCollider = mushroomRenderer.gameObject.GetComponent<Collider>();
        if (rewardCollider == null && rewardObject != null)
            rewardCollider = rewardObject.GetComponentInChildren<Collider>();

        // Disable non-essential elements by default
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (rewardObject != null) rewardObject.SetActive(false);
        if (feedPanel != null) feedPanel.SetActive(false);
        if (rewardViewPanel != null) rewardViewPanel.SetActive(false);

        LoadFromMetaprogression();
    }

    void LoadFromMetaprogression()
    {
        if (Metaprogression.Instance == null) return;

        string mushroomName = Metaprogression.Instance.plotMushroomNames[plotIndex];
        int progress = Metaprogression.Instance.plotProgress[plotIndex];
        bool completed = Metaprogression.Instance.plotCompleted[plotIndex];

        if (!string.IsNullOrEmpty(mushroomName))
        {
            Shroom mushroom = GetMushroomByName(mushroomName);
            if (mushroom != null)
            {
                // Set mushroom data without audio or saving
                assignedMushroom = mushroom;
                isAssigned = true;
                currentProgress = progress;

                // Add to static tracking
                currentlyAssignedMushrooms.Add(mushroomName);

                if (beamLight != null) beamLight.color = mushroom.color;
                if (mushroomRenderer != null) mushroomRenderer.material = mushroom.growingMat;

                // Instantiate reward prefab (kept inactive for now)
                if (rewardObject != null && assignedMushroom.rewardPrefab != null)
                {
                    foreach (Transform child in rewardObject.transform)
                    {
                        Destroy(child.gameObject);
                    }
                    Instantiate(assignedMushroom.rewardPrefab, rewardObject.transform.position, rewardObject.transform.rotation, rewardObject.transform);
                }

                if (completed)
                {
                    // Complete state: show reward, hide progress, use grown material
                    isCompleted = true;
                    isGrowing = true;
                    if (mushroomRenderer != null)
                    {
                        mushroomRenderer.material = assignedMushroom.grownMat;
                        mushroomRenderer.transform.localScale = Vector3.one * 1.4f;
                    }
                    if (rewardObject != null) rewardObject.SetActive(true);
                }
                else if (progress > 0)
                {
                    // Growing state: show progress text
                    isGrowing = true;
                    if (mushroomRenderer != null)
                        mushroomRenderer.transform.localScale = Vector3.one * 1.2f;
                    if (progressText != null)
                    {
                        progressText.gameObject.SetActive(true);
                        progressText.text = $"{currentProgress}/{assignedMushroom.reqProgress}";
                    }
                }
            }
        }
    }

    Shroom GetMushroomByName(string name)
    {
        foreach (Shroom mushroom in availableMushrooms)
        {
            if (mushroom.mushroomName == name) return mushroom;
        }
        return null;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.transform.position, transform.position);
        bool inRange = distance <= interactionDistance;

        if (inRange)
        {
            // Check what the player is looking at via raycast
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                RaycastHit hit;

                // Regular raycast
                if (Physics.Raycast(ray, out hit, interactionDistance))
                {
                    // Only process if hit object has Interactable tag
                    if (hit.collider.CompareTag("Interactable"))
                    {
                        // Check if looking at mushroom collider
                        if (!isCompleted && mushroomCollider != null && hit.collider == mushroomCollider)
                        {
                            playerLookingAtAnyPlot = true;
                            UpdateFeedPanelUI();
                            if (rewardViewPanel != null) rewardViewPanel.SetActive(false);

                            // Handle interaction
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                bool canFeed = !isAssigned || isGrowing;
                                Debug.Log($"[Plot {plotIndex}] E pressed - isAssigned: {isAssigned}, isGrowing: {isGrowing}, canFeed: {canFeed}");
                                if (canFeed)
                                {
                                    FeedMushroom();
                                }
                            }
                            return;
                        }
                        // Check if looking at reward collider
                        else if (isCompleted && rewardCollider != null && hit.collider == rewardCollider)
                        {
                            playerLookingAtAnyPlot = true;
                            UpdateRewardViewPanel();
                            if (feedPanel != null) feedPanel.SetActive(false);

                            // Handle interaction
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                CollectReward();
                            }
                            return;
                        }
                    }
                }
            }
        }

        // Only hide panels if not looking at any plot
        if (!playerLookingAtAnyPlot)
        {
            if (feedPanel != null) feedPanel.SetActive(false);
            if (rewardViewPanel != null) rewardViewPanel.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // Reset the static flag at the end of the frame
        playerLookingAtAnyPlot = false;
    }

    void UpdateFeedPanelUI()
    {
        if (feedPanel == null) return;

        if (!isAssigned)
        {
            // Not started - determine which mushroom will be assigned based on player inventory
            Shroom mushroomToAssign = GetMushroomThatWillBeAssigned();

            // Only show panel if player has a valid material
            if (mushroomToAssign != null && mushroomToAssign.reagent != null)
            {
                feedPanel.SetActive(true);
                if (feedIcon != null) feedIcon.sprite = mushroomToAssign.reagent.itemIcon;
            }
            else
            {
                feedPanel.SetActive(false);
            }
        }
        else if (isAssigned && !isCompleted)
        {
            // Assigned (either not growing yet, or actively growing) - show assigned mushroom's reagent icon
            if (assignedMushroom != null && assignedMushroom.reagent != null)
            {
                feedPanel.SetActive(true);
                if (feedIcon != null) feedIcon.sprite = assignedMushroom.reagent.itemIcon;
            }
            else
            {
                feedPanel.SetActive(false);
            }
        }
        else
        {
            feedPanel.SetActive(false);
        }
    }

    void UpdateRewardViewPanel()
    {
        if (rewardViewPanel == null) return;
        rewardViewPanel.SetActive(true);
    }

    void CollectReward()
    {
        if (!isCompleted || assignedMushroom == null) return;

        int mushroomIndex = -1;
        for (int i = 0; i < availableMushrooms.Length; i++)
        {
            if (availableMushrooms[i] == assignedMushroom)
            {
                mushroomIndex = i;
                break;
            }
        }

        if (mushroomIndex == -1) return;

        // Trigger effect based on mushroom index
        switch (mushroomIndex)
        {
            case 0:
                ApplyEffect0();
                break;
            case 1:
                ApplyEffect1();
                break;
            case 2:
                ApplyEffect2();
                break;
        }
    }

    void ApplyEffect0()
    {
        // Disable fog and set seeClearly to true
        if (Metaprogression.Instance != null)
        {
            Metaprogression.Instance.seeClearly = true;
            Metaprogression.Instance.SaveToFile();

            // Apply immediately
            if (GameManager.instance != null && GameManager.instance.playerState != null)
            {
                GameManager.instance.playerState.seeClearly = true;
            }
            RenderSettings.fog = false;
        }
    }

    void ApplyEffect1()
    {
        // Toggle between weaponMappings 1 and 0
        if (GameManager.instance != null && GameManager.instance.playerState != null)
        {
            PlayerState playerState = GameManager.instance.playerState;

            if (playerState.weaponMappings.Count > 1)
            {
                // If current weapon is not weaponMappings[1], switch to it
                if (playerState.equippedWeapon != playerState.weaponMappings[1].weapon)
                {
                    playerState.equippedWeapon = playerState.weaponMappings[1].weapon;
                }
                // If it is weaponMappings[1], switch to weaponMappings[0]
                else if (playerState.weaponMappings.Count > 0)
                {
                    playerState.equippedWeapon = playerState.weaponMappings[0].weapon;
                }
            }
        }
    }

    void ApplyEffect2()
    {
        // Toggle between weaponMappings 2 and 0
        if (GameManager.instance != null && GameManager.instance.playerState != null)
        {
            PlayerState playerState = GameManager.instance.playerState;

            if (playerState.weaponMappings.Count > 2)
            {
                // If current weapon is not weaponMappings[2], switch to it
                if (playerState.equippedWeapon != playerState.weaponMappings[2].weapon)
                {
                    playerState.equippedWeapon = playerState.weaponMappings[2].weapon;
                }
                // If it is weaponMappings[2], switch to weaponMappings[0]
                else if (playerState.weaponMappings.Count > 0)
                {
                    playerState.equippedWeapon = playerState.weaponMappings[0].weapon;
                }
            }
        }
    }

    Shroom GetMushroomThatWillBeAssigned()
    {
        if (playerInventory == null) return null;

        // Return the first mushroom that player has reagent for AND isn't assigned elsewhere
        foreach (Shroom mushroom in availableMushrooms)
        {
            if (IsMushroomAssignedElsewhere(mushroom.mushroomName))
                continue;

            if (playerInventory.HasItem(mushroom.reagent, 1))
                return mushroom;
        }

        return null;
    }

    void FeedMushroom()
    {
        if (playerInventory == null) return;

        Debug.Log($"[Plot {plotIndex}] FeedMushroom - isAssigned: {isAssigned}, isGrowing: {isGrowing}, currentProgress: {currentProgress}");

        // If not assigned, try to assign based on what item they have
        if (!isAssigned)
        {
            Debug.Log($"[Plot {plotIndex}] Not assigned, calling TryAssignMushroom");
            TryAssignMushroom();
            return;
        }

        if (!isGrowing)
        {
            Debug.Log($"[Plot {plotIndex}] Assigned but not growing, starting growth");
            if (playerInventory.HasItem(assignedMushroom.reagent, 1))
            {
                playerInventory.RemoveItem(assignedMushroom.reagent, 1);
                StartGrowing();
            }
        }
        else
        {
            Debug.Log($"[Plot {plotIndex}] Already growing, incrementing progress");
            if (currentProgress >= assignedMushroom.reqProgress) return;

            if (playerInventory.HasItem(assignedMushroom.reagent, 1))
            {
                playerInventory.RemoveItem(assignedMushroom.reagent, 1);
                currentProgress++;
                UpdateVisuals();
                SaveProgress();

                // Show poof effect
                if (poof != null)
                {
                    poof.SetActive(true);
                    StartCoroutine(DisablePoofAfterDelay(3f));
                }

                if (audioSource != null && feedClip != null)
                    audioSource.PlayOneShot(feedClip, feedVolume);

                if (currentProgress >= assignedMushroom.reqProgress)
                {
                    CompleteMushroom();
                }
            }
        }
    }

    void TryAssignMushroom()
    {
        Debug.Log($"[Plot {plotIndex}] TryAssignMushroom called");
        foreach (Shroom mushroom in availableMushrooms)
        {
            // Skip if this mushroom is already assigned to another plot
            if (IsMushroomAssignedElsewhere(mushroom.mushroomName))
            {
                Debug.Log($"[Plot {plotIndex}] Skipping {mushroom.mushroomName} - assigned elsewhere");
                continue;
            }

            if (playerInventory.HasItem(mushroom.reagent, 1))
            {
                Debug.Log($"[Plot {plotIndex}] Found match: {mushroom.mushroomName}, assigning and starting growth");
                playerInventory.RemoveItem(mushroom.reagent, 1);
                AssignMushroom(mushroom);
                Debug.Log($"[Plot {plotIndex}] After AssignMushroom - isAssigned: {isAssigned}, isGrowing: {isGrowing}");
                StartGrowing();
                Debug.Log($"[Plot {plotIndex}] After StartGrowing - isAssigned: {isAssigned}, isGrowing: {isGrowing}");
                return;
            }
        }
        Debug.Log($"[Plot {plotIndex}] TryAssignMushroom - no valid mushroom found");
    }

    bool IsMushroomAssignedElsewhere(string mushroomName)
    {
        // Check if this mushroom is in the static tracking set
        // (This covers mushrooms assigned in the current session)
        if (currentlyAssignedMushrooms.Contains(mushroomName))
        {
            // If it's in the set, make sure it's not this plot's own mushroom
            if (assignedMushroom == null || assignedMushroom.mushroomName != mushroomName)
                return true;
        }

        // Also check saved metaprogression data as a fallback
        if (Metaprogression.Instance == null) return false;

        for (int i = 0; i < Metaprogression.Instance.plotMushroomNames.Length; i++)
        {
            // Skip this plot's own index
            if (i == plotIndex) continue;

            // Check if another plot has this mushroom assigned
            if (Metaprogression.Instance.plotMushroomNames[i] == mushroomName)
                return true;
        }
        return false;
    }

    void AssignMushroom(Shroom mushroom)
    {
        assignedMushroom = mushroom;
        isAssigned = true;

        // Add to static tracking to prevent duplicate assignments
        currentlyAssignedMushrooms.Add(mushroom.mushroomName);

        if (beamLight != null) beamLight.color = mushroom.color;
        if (mushroomRenderer != null) mushroomRenderer.material = mushroom.growingMat;

        if (rewardObject != null && assignedMushroom.rewardPrefab != null)
        {
            foreach (Transform child in rewardObject.transform)
            {
                Destroy(child.gameObject);
            }

            Instantiate(assignedMushroom.rewardPrefab, rewardObject.transform.position, rewardObject.transform.rotation, rewardObject.transform);
        }

        if (rewardObject != null) rewardObject.SetActive(false);

        if (audioSource != null && assignClip != null)
            audioSource.PlayOneShot(assignClip, assignVolume);

        if (Metaprogression.Instance != null)
        {
            Metaprogression.Instance.plotMushroomNames[plotIndex] = mushroom.mushroomName;
            Metaprogression.Instance.SaveToFile();
        }
    }

    void StartGrowing()
    {
        isGrowing = true;
        currentProgress = 1;
        UpdateVisuals();
        SaveProgress();

        // Scale mushroom to 1.2 when growing starts
        if (mushroomRenderer != null)
            mushroomRenderer.transform.localScale = Vector3.one * 1.2f;

        // Show poof effect
        if (poof != null)
        {
            poof.SetActive(true);
            StartCoroutine(DisablePoofAfterDelay(3f));
        }

        if (audioSource != null && feedClip != null)
            audioSource.PlayOneShot(feedClip, feedVolume);
    }

    void UpdateVisuals()
    {
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"{currentProgress}/{assignedMushroom.reqProgress}";
        }
    }

    void CompleteMushroom()
    {
        isCompleted = true;

        if (mushroomRenderer != null)
        {
            mushroomRenderer.material = assignedMushroom.grownMat;
            // Scale mushroom to 1.4 when completed
            mushroomRenderer.transform.localScale = Vector3.one * 1.4f;
        }

        if (progressText != null) progressText.gameObject.SetActive(false);
        if (rewardObject != null) rewardObject.SetActive(true);

        // Show poof effect
        if (poof != null)
        {
            poof.SetActive(true);
            StartCoroutine(DisablePoofAfterDelay(3f));
        }

        if (audioSource != null && completeClip != null)
            audioSource.PlayOneShot(completeClip, completeVolume);

        Metaprogression.Instance.plotCompleted[plotIndex] = true;
        Metaprogression.Instance.SaveToFile();
    }

    System.Collections.IEnumerator DisablePoofAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (poof != null)
            poof.SetActive(false);
    }

    void SaveProgress()
    {
        if (Metaprogression.Instance != null)
        {
            Metaprogression.Instance.plotProgress[plotIndex] = currentProgress;
            Metaprogression.Instance.SaveToFile();
        }
    }
}
