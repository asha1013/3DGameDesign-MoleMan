using System.Collections;
using NUnit.Framework.Constraints;
using TMPro;
using UnityEngine;
using MoreMountains.Tools;
using Unity.Mathematics;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance => instance;

    public GameObject deadScreen;
    public GameObject winScreen;
    public GameObject uiObj;
    public TextMeshProUGUI hpText;

    public MMProgressBar staminaBar;
    PlayerState playerState;

    [Header("Scene-Specific UI Elements")]
    public GameObject healthParentObj;
    public GameObject staminaBarObj;
    public GameObject minimapObj;
    public GameObject projectileIndicator;

    [Header("Directional Warning Indicators")]
    public UnityEngine.UI.Image leftIndicator;
    public UnityEngine.UI.Image rightIndicator;
    public UnityEngine.UI.Image rearIndicator;

    private Coroutine leftCoroutine;
    private Coroutine rightCoroutine;
    private Coroutine rearCoroutine;

    [Header("Screen Fade")]
    public UnityEngine.UI.Image fadeImage;
    public bool fading;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RefreshReferences();
        UpdateUIVisibility();
        UpdateLivesBG();
    }

    void Start()
    {
        RefreshReferences();
        UpdateUIVisibility();
        if(fadeImage.color.a>0) StartCoroutine(ScreenFade(false));
    }

    void RefreshReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
        }

        if (GameManager.instance != null && GameManager.instance.playerState != null)
        {
            if (GameManager.instance.playerState.staminaBar != null) staminaBar = GameManager.instance.playerState.staminaBar;
        }

        // Update HP text if available
        if (playerState != null && hpText != null)
        {
            hpText.text = playerState.hp.ToString();
        }
    }

    void UpdateUIVisibility()
    {
        bool inDungeon = GameManager.Instance != null && GameManager.Instance.inDungeon;


        if (healthParentObj != null) healthParentObj.SetActive(inDungeon);
        if (staminaBarObj != null) staminaBarObj.SetActive(inDungeon);
        if (minimapObj != null) minimapObj.SetActive(inDungeon);
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    public void Death()
    {
       //if (deadScreen != null) deadScreen.SetActive(true);
        SceneTransitionManager.Instance.LoadGardenFromDungeon();        
    }

    public void Win()
    {
        if (winScreen != null) winScreen.SetActive(true);
    }

    public void UpdateLivesBG()
    {
        if (playerState == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerState != null)
            {
                playerState = GameManager.instance.playerState;
            }
            else
            {
                return;
            }
        }

        playerState.UpdateHealthDisplay();

        if (healthParentObj == null)
        {
            Debug.LogError("UIManager.UpdateLivesBG: healthParentObj not assigned");
            return;
        }

        RectTransform hpBgTransform = healthParentObj.GetComponent<RectTransform>();
        if (hpBgTransform == null)
        {
            Debug.LogError("UIManager.UpdateLivesBG: healthParentObj missing RectTransform component");
            return;
        }

        if (playerState.maxHP>6) hpBgTransform.sizeDelta = new Vector2(1110+(160*(playerState.maxHP-6)),246);
    }

    public IEnumerator Healed(int heal)
    {
        if (hpText != null && playerState != null)
        {
            hpText.text = playerState.hp.ToString();
        }
        yield return null;
    }

    public void UpdateStamina()
    {
        if (staminaBar != null && playerState != null)
        {
            staminaBar.UpdateBar(playerState.stamina, 0f, playerState.maxStamina);
            staminaBar.SetBar(playerState.stamina, 0f, playerState.maxStamina); // force immediate update
        }
    }
    public IEnumerator ScreenFade(bool fadeOut)
    {
        if (!fadeImage.gameObject.activeSelf)fadeImage.gameObject.SetActive(true);
        if (fadeImage == null) yield break;
        

        float timer = 0f;
        float startAlpha = fadeOut ? 0f : 1f;
        float endAlpha = fadeOut ? 1f : 0f;

        Color color = fadeImage.color;

        while (timer < GameManager.instance.sceneManager.fadeTime)
        {
            if (!fading) fading=true;
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, timer / GameManager.instance.sceneManager.fadeTime);
            fadeImage.color = color;
            yield return null;
        }
        fading=false;
        fadeImage.gameObject.SetActive(fadeOut);

        color.a = endAlpha;
        fadeImage.color = color;
    }

    public IEnumerator ProjectileWarningIndicator(GameObject worldObject)
    {
        if (worldObject == null || GameManager.instance == null || GameManager.instance.camObj == null) yield break;

        Camera cam = GameManager.instance.camObj.GetComponent<Camera>();
        if (cam == null) yield break;

        // Check if object is in FOV, return if it is
        Vector3 viewportPos = cam.WorldToViewportPoint(worldObject.transform.position);
        if (viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f && viewportPos.z > 0f)
        {
            yield break;
        }

        if (projectileIndicator == null) yield break;

        GameObject indicator = Instantiate(projectileIndicator, uiObj.transform);
        RectTransform rectTransform = indicator.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Destroy(indicator);
            yield break;
        }

        Transform playerTransform = GameManager.instance.camObj.transform;

        EnemyProjectile enemyProjectileScript = worldObject.GetComponent<EnemyProjectile>();
        if (enemyProjectileScript == null)
        {
            Destroy(indicator);
            yield break;
        }

        // Track while active
        while (worldObject != null)
        {
            if (enemyProjectileScript == null || enemyProjectileScript.hasHit) break;

            // Calculate direction from player to target
            Vector3 directionToTarget = (worldObject.transform.position - playerTransform.position).normalized;

            // Convert to camera-relative direction
            Vector3 localDir = playerTransform.InverseTransformDirection(directionToTarget);

            // Calculate angle from forward (in degrees, -180 to 180)
            float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

            // Calculate edge position (anchored to canvas center)
            Vector2 anchoredPos = Vector2.zero;
            float edgeMargin = 100f;
            float halfWidth = 960f;
            float halfHeight = 540f;

            // Determine which edge and calculate position
            float absAngle = Mathf.Abs(angle);

            if (absAngle <= 45f) // Top edge
            {
                anchoredPos = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, (angle + 45f) / 90f), halfHeight - edgeMargin);
            }
            else if (absAngle >= 135f) // Bottom edge
            {
                anchoredPos = new Vector2(Mathf.Lerp(halfWidth, -halfWidth, (angle + 180f) / 90f), -halfHeight + edgeMargin);
            }
            else if (angle > 0f) // Right edge
            {
                float t = (angle - 45f) / 90f;
                anchoredPos = new Vector2(halfWidth - edgeMargin, Mathf.Lerp(halfHeight - edgeMargin, -halfHeight + edgeMargin, t));
            }
            else // Left edge
            {
                float t = (angle + 45f) / 90f;
                anchoredPos = new Vector2(-halfWidth + edgeMargin, Mathf.Lerp(halfHeight - edgeMargin, -halfHeight + edgeMargin, 1f - t));
            }

            rectTransform.anchoredPosition = anchoredPos;

            // Rotate projectile indicators to point toward center
            Vector2 directionToCenter = Vector2.zero - anchoredPos;
            float angleToCenter = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0f, 0f, angleToCenter - 90f);

            yield return null;
        }

        // Cleanup
        if (indicator != null) Destroy(indicator);
    }

    public void WarningIndicator(Vector3 enemyPosition)
    {
        if (GameManager.instance == null || GameManager.instance.camObj == null) return;

        Transform playerTransform = GameManager.instance.camObj.transform;

        // Calculate direction from player to enemy
        Vector3 directionToEnemy = (enemyPosition - playerTransform.position).normalized;

        // Convert to local space relative to player's Y rotation
        Vector3 localDir = playerTransform.InverseTransformDirection(directionToEnemy);

        // Calculate angle on horizontal plane (-180 to 180)
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // Determine which zone: Left (-180 to -60), Rear (-60 to 60), Right (60 to 180)
        UnityEngine.UI.Image targetIndicator = null;
        Coroutine targetCoroutine = null;

        if (angle < -60f) // Left zone
        {
            targetIndicator = leftIndicator;
            targetCoroutine = leftCoroutine;
        }
        else if (angle > 60f) // Right zone
        {
            targetIndicator = rightIndicator;
            targetCoroutine = rightCoroutine;
        }
        else // Rear zone (-60 to 60)
        {
            targetIndicator = rearIndicator;
            targetCoroutine = rearCoroutine;
        }

        if (targetIndicator == null) return;

        // Cancel existing coroutine if running
        if (targetCoroutine != null)
        {
            StopCoroutine(targetCoroutine);
        }

        // Start new flash coroutine
        if (targetIndicator == leftIndicator)
        {
            leftCoroutine = StartCoroutine(FlashIndicator(leftIndicator));
        }
        else if (targetIndicator == rightIndicator)
        {
            rightCoroutine = StartCoroutine(FlashIndicator(rightIndicator));
        }
        else if (targetIndicator == rearIndicator)
        {
            rearCoroutine = StartCoroutine(FlashIndicator(rearIndicator));
        }
    }

    IEnumerator FlashIndicator(UnityEngine.UI.Image indicator)
    {
        float targetAlpha = 0.8f;
        float flashDuration = 0.2f;

        Color color = indicator.color;
        float startAlpha = color.a;

        // Lerp up to target alpha
        float timeToTarget = startAlpha < targetAlpha ? flashDuration * ((targetAlpha - startAlpha) / targetAlpha) : 0f;
        float timer = 0f;

        while (timer < timeToTarget)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, timer / timeToTarget);
            indicator.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        indicator.color = color;

        // Lerp down to 0
        timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(targetAlpha, 0f, timer / flashDuration);
            indicator.color = color;
            yield return null;
        }

        color.a = 0f;
        indicator.color = color;
    }

}
