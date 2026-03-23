using UnityEngine;
using System.Collections;
using NaughtyAttributes;

public class GroundTargetAttack : MonoBehaviour
{
    [BoxGroup("References")] public Transform projectileOrigin;
    [BoxGroup("References")] public Animator animator;
    [BoxGroup("References")] public AudioSource weaponAudioSource;

    [BoxGroup("State")] [ReadOnly] private bool isInStartup = false;
    [BoxGroup("State")] [ReadOnly] private bool isAiming = false;
    [BoxGroup("State")] [ReadOnly] private float currentAimTime = 0f;
    [BoxGroup("State")] [ReadOnly] private Vector3 targetPosition;

    public bool IsInStartup => isInStartup;
    public bool IsAiming => isAiming;
    public bool HasFired { get; private set; }

    private PlayerState playerState;
    private SUPERCharacter.SUPERCharacterAIO superController;
    private float cachedrotationWeight;
    private Transform aimObjectInstance;
    private Material tempMaterial;
    private Renderer weaponRenderer;
    private Coroutine startupCoroutine;
    private Coroutine aimCoroutine;
    private Coroutine emissionCoroutine;

    void Start()
    {
        // Get renderer for material manipulation
        weaponRenderer = GetComponent<Renderer>();
        if (weaponRenderer == null)
        {
            weaponRenderer = GetComponentInChildren<Renderer>();
        }

        // Get player state and controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
            superController = player.GetComponent<SUPERCharacter.SUPERCharacterAIO>();
        }

        // Instantiate aim object if weapon has one
        Weapon weapon = GetWeapon();
        if (weapon != null && weapon.aimObject != null)
        {
            GameObject aimObj = Instantiate(weapon.aimObject);
            aimObjectInstance = aimObj.transform;
            aimObjectInstance.gameObject.SetActive(false);
        }
    }

    Weapon GetWeapon()
    {
        if (playerState == null || playerState.equippedWeapon == null)
        {
            Debug.LogWarning("GroundTargetAttack: No weapon equipped");
            return null;
        }
        return playerState.equippedWeapon;
    }

    public void StartAim()
    {
        if (isInStartup || isAiming) return;

        Weapon weapon = GetWeapon();
        if (weapon == null) return;

        isInStartup = true;
        HasFired = false;

        // Trigger startup animation
        if (animator != null)
        {
            animator.SetTrigger("startup");
        }

        // Play aim audio
        if (weaponAudioSource != null && weapon.weaponAimClip != null)
        {
            weaponAudioSource.PlayOneShot(weapon.weaponAimClip, weapon.weaponAimClipVolume);
        }

        if (startupCoroutine != null) StopCoroutine(startupCoroutine);
        startupCoroutine = StartCoroutine(StartupSequence());
    }

    IEnumerator StartupSequence()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null) yield break;

        // Wait for startup animation
        yield return new WaitForSeconds(weapon.startTime);

        isInStartup = false;
        isAiming = true;
        currentAimTime = 0f;

        // Cache and adjust camera weight
        if (superController != null)
        {
            cachedrotationWeight = superController.rotationWeight;
            superController.rotationWeight = weapon.aimWeight;
        }

        // Create temp material instance
        if (weaponRenderer != null && weapon.aimMat != null)
        {
            tempMaterial = new Material(weapon.aimMat);
            weaponRenderer.material = tempMaterial;
        }

        // Show aim object
        if (aimObjectInstance != null)
        {
            aimObjectInstance.gameObject.SetActive(true);
        }

        // Start aim update loop
        if (aimCoroutine != null) StopCoroutine(aimCoroutine);
        aimCoroutine = StartCoroutine(AimUpdate());
    }

    IEnumerator AimUpdate()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null) yield break;

        while (isAiming)
        {
            currentAimTime += Time.deltaTime;
            float aimPercent = Mathf.Clamp01(currentAimTime / weapon.powerRampTime);

            // Lerp power from min to max
            float currentPower = Mathf.Lerp(weapon.minPower, weapon.maxPower, aimPercent);

            // Update emission based on power
            if (tempMaterial != null)
            {
                float emissionValue = Mathf.Lerp(0f, weapon.aimEmit, aimPercent);
                tempMaterial.SetColor("_EmissionColor", Color.white * emissionValue);
            }

            // Calculate target position using current power
            CalculateTargetPosition(currentPower);

            // Update aim object position
            if (aimObjectInstance != null)
            {
                aimObjectInstance.position = targetPosition;
            }

            yield return null;
        }
    }

    void CalculateTargetPosition(float power)
    {
        Transform camera = GameObject.FindGameObjectWithTag("MainCamera").transform;

        // Raycast from camera forward to find direct hits
        if (Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, power))
        {
            // Direct hit on terrain/object within range
            targetPosition = hit.point;
        }
        else
        {
            // No direct hit - calculate ground point at range
            Vector3 endPoint = camera.position + camera.forward * power;
            if (Physics.Raycast(endPoint, Vector3.down, out RaycastHit groundHit, 100f))
            {
                targetPosition = groundHit.point;
            }
            else
            {
                // Fallback if no ground found
                targetPosition = endPoint + Vector3.down * 5f;
            }
        }
    }

    public void ReleaseAim()
    {
        if (!isAiming) return;

        // Stop aim coroutine
        if (aimCoroutine != null) StopCoroutine(aimCoroutine);

        // Fire projectile
        FireProjectile();

        // Hide aim object
        if (aimObjectInstance != null)
        {
            aimObjectInstance.gameObject.SetActive(false);
        }

        // Restore camera weight
        if (superController != null)
        {
            superController.rotationWeight = cachedrotationWeight;
        }

        // Lerp emission down
        if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
        emissionCoroutine = StartCoroutine(LerpEmissionDown());

        isAiming = false;
    }

    public void CancelAim()
    {
        // Cancel startup
        if (isInStartup)
        {
            if (startupCoroutine != null) StopCoroutine(startupCoroutine);
            isInStartup = false;
        }

        // Cancel aiming
        if (isAiming)
        {
            if (aimCoroutine != null) StopCoroutine(aimCoroutine);

            if (aimObjectInstance != null)
            {
                aimObjectInstance.gameObject.SetActive(false);
            }

            if (superController != null)
            {
                superController.rotationWeight = cachedrotationWeight;
            }

            if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
            emissionCoroutine = StartCoroutine(LerpEmissionDown());

            isAiming = false;
        }
    }

    IEnumerator LerpEmissionDown()
    {
        Weapon weapon = GetWeapon();
        float startEmission = weapon != null ? weapon.aimEmit : 5f;
        if (tempMaterial != null)
        {
            Color currentColor = tempMaterial.GetColor("_EmissionColor");
            startEmission = currentColor.r;
        }

        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float emissionValue = Mathf.Lerp(startEmission, 0f, t);

            if (tempMaterial != null)
            {
                tempMaterial.SetColor("_EmissionColor", Color.white * emissionValue);
            }

            yield return null;
        }

        // Clean up temp material
        if (tempMaterial != null)
        {
            Destroy(tempMaterial);
            tempMaterial = null;
        }
    }

    void FireProjectile()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null || weapon.projectilePrefab == null || projectileOrigin == null)
        {
            Debug.LogWarning("GroundTargetAttack: Projectile prefab or origin not assigned");
            return;
        }

        HasFired = true;

        // Instantiate projectile
        GameObject projectile = Instantiate(weapon.projectilePrefab, projectileOrigin.position, projectileOrigin.rotation);

        // Calculate ballistic trajectory to target
        Vector3 startPos = projectileOrigin.position;
        Vector3 endPos = targetPosition;

        float horizontalDist = Vector3.Distance(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(endPos.x, 0, endPos.z)
        );

        // Use weapon's arc height
        float arcHeight = weapon.arcHeight;
        float gravity = 20f;

        float peakHeight = Mathf.Max(startPos.y, endPos.y) + arcHeight;
        float heightToReach = peakHeight - startPos.y;

        float verticalVel = Mathf.Sqrt(2 * gravity * heightToReach);
        float timeToPeak = verticalVel / gravity;
        float fallHeight = peakHeight - endPos.y;
        float timeFromPeak = Mathf.Sqrt(2 * fallHeight / gravity);
        float totalTime = timeToPeak + timeFromPeak;

        float horizontalSpeed = horizontalDist / totalTime;
        Vector3 direction = (new Vector3(endPos.x, startPos.y, endPos.z) - new Vector3(startPos.x, startPos.y, startPos.z)).normalized;

        // Apply to projectile
        PlayerProjectile projScript = projectile.GetComponent<PlayerProjectile>();
        if (projScript != null)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(
                    direction.x * horizontalSpeed,
                    verticalVel,
                    direction.z * horizontalSpeed
                );
            }

            // Set projectile properties
            projScript.damage = weapon.baseDamage;
            projScript.travelArc = true;
            projScript.dropOff = gravity;
        }
    }

    void OnDestroy()
    {
        // Clean up temp material
        if (tempMaterial != null)
        {
            Destroy(tempMaterial);
        }

        // Clean up aim object
        if (aimObjectInstance != null)
        {
            Destroy(aimObjectInstance.gameObject);
        }
    }
}
