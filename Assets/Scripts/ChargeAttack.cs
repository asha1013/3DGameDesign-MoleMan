using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class ChargeAttack : MonoBehaviour
{
    [BoxGroup("References")] public Transform projectileOrigin;
    [BoxGroup("References")] public Animator animator;
    [BoxGroup("References")] public AudioSource weaponAudioSource;
    [BoxGroup("References")] public Renderer weaponRenderer;

    [BoxGroup("Timing")] public float startupTime = 0.2f;
    [BoxGroup("Timing")] public float fireTime = 0.15f;

    [BoxGroup("Projectile Scaling")] public float projectileStartScale = 0.2f;
    [BoxGroup("Projectile Scaling")] public float projectileMaxScale = 3f;
    [BoxGroup("Projectile Scaling")] public float fullEmit;
    [BoxGroup("Projectile Scaling")] public float flashEmit;

    [BoxGroup("State")] [SerializeField] [ReadOnly] private bool isCharging = false;
    [BoxGroup("State")] [SerializeField] [ReadOnly] private bool isCharged = false;
    [BoxGroup("State")] [SerializeField] [ReadOnly] private bool inStartup = false;
    [BoxGroup("State")] [SerializeField] [ReadOnly] private bool earlyRelease = false;
    [BoxGroup("State")] [SerializeField] [ReadOnly] private bool inputBuffered = false;
    [BoxGroup("State")] [SerializeField] [ReadOnly] private float currentChargeTime = 0f;

    public bool IsCharging => isCharging;
    public bool IsCharged => isCharged;
    public bool HasFired { get; private set; }

    private PlayerState playerState;
    private Color cachedBaseEmission;
    private Coroutine chargeCoroutine;
    private Coroutine emissionCoroutine;
    private Coroutine flashCoroutine;
    private GameObject chargingProjectile;
    private PlayerProjectile chargingProjectileScript;
    private float currentEmissionLerp = 0f;

    void Start()
    {
        // Get player state first
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
        }

        Debug.Log($"ChargeAttack: Start - weaponRenderer={(weaponRenderer != null ? weaponRenderer.name : "null")}");

        // Cache emission colors from assigned materials
        CacheEmissionColors();
    }

    void CacheEmissionColors()
    {
        // Get weapon renderer from playerState.weaponObject
        if (playerState != null && playerState.weaponObject != null)
        {
            weaponRenderer = playerState.weaponObject.GetComponentInChildren<Renderer>();
            if (weaponRenderer != null && weaponRenderer.material != null)
            {
                cachedBaseEmission = weaponRenderer.material.GetColor("_EmissionColor");
                Debug.Log($"ChargeAttack: Cached base emission - Color: {cachedBaseEmission}, Intensity: {cachedBaseEmission.maxColorComponent}");
            }
        }
    }

    void Update()
    {
        // Make charging projectile follow projectile origin
        if (chargingProjectile != null && projectileOrigin != null)
        {
            chargingProjectile.transform.position = projectileOrigin.position;
            chargingProjectile.transform.rotation = projectileOrigin.rotation;
        }

        // Apply emission lerp to current renderer material
        if (weaponRenderer != null && weaponRenderer.material != null)
        {
            Weapon weapon = GetWeapon();
            if (weapon != null)
            {
                float baseIntensity = cachedBaseEmission.maxColorComponent;
                Color normalized = baseIntensity > 0 ? cachedBaseEmission / baseIntensity : Color.white;
                Color targetColor = normalized * Mathf.Lerp(baseIntensity, weapon.chargeEmit, currentEmissionLerp);
                weaponRenderer.material.SetColor("_EmissionColor", targetColor);
            }
        }
    }

    Weapon GetWeapon()
    {
        if (playerState == null || playerState.equippedWeapon == null)
        {
            Debug.LogWarning("ChargeAttack: No weapon equipped");
            return null;
        }
        return playerState.equippedWeapon;
    }

    public void StartCharge()
    {
        if (isCharging)
        {
            // Buffer the input if already charging
            inputBuffered = true;
            return;
        }

        Weapon weapon = GetWeapon();
        if (weapon == null) return;

        Debug.Log($"ChargeAttack: Starting charge - chargeTime={weapon.chargeTime}, chargeEmit={weapon.chargeEmit}, chargedFlash={weapon.chargedFlash}");

        isCharging = true;
        isCharged = false;
        inStartup = true;
        earlyRelease = false;
        inputBuffered = false;
        currentChargeTime = 0f;
        HasFired = false;

        // Start charging animation
        if (animator != null)
        {
            animator.SetTrigger("charging");
        }

        // Start charge coroutine
        if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
        chargeCoroutine = StartCoroutine(ChargeWeapon());
    }

    public void ReleaseCharge()
    {
        if (!isCharging) return;

        // If released during startup, cancel the attack entirely
        if (inStartup)
        {
            // Stop all coroutines
            if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
            if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);

            // Destroy charging projectile
            if (chargingProjectile != null)
            {
                Destroy(chargingProjectile);
                chargingProjectile = null;
                chargingProjectileScript = null;
            }

            // Stop audio
            if (weaponAudioSource != null)
            {
                weaponAudioSource.Stop();
            }

            // Trigger release animation
            if (animator != null)
            {
                animator.SetTrigger("release");
            }

            // Restore emission colors
            if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
            emissionCoroutine = StartCoroutine(LerpEmissionDown());

            // Reset state
            isCharging = false;
            isCharged = false;
            inStartup = false;
            earlyRelease = false;
            return;
        }

        // Stop all charging coroutines
        if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
        if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        // Stop audio
        if (weaponAudioSource != null)
        {
            weaponAudioSource.Stop();
        }

        // Trigger release animation
        if (animator != null)
        {
            animator.SetTrigger("release");
        }

        // Start fire sequence
        StartCoroutine(FireSequence());
    }

    IEnumerator ChargeWeapon()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null) yield break;

        float totalChargeTime = startupTime + weapon.chargeTime;
        float totalElapsed = 0f;

        // Instantiate projectile at start scale with script disabled (right when charging starts)
        if (weapon.projectilePrefab != null && projectileOrigin != null)
        {
            chargingProjectile = Instantiate(weapon.projectilePrefab, projectileOrigin.position, projectileOrigin.rotation);
            chargingProjectile.transform.localScale = Vector3.one * projectileStartScale;
            chargingProjectileScript = chargingProjectile.GetComponent<PlayerProjectile>();
            if (chargingProjectileScript != null)
            {
                chargingProjectileScript.enabled = false;
                // Disable collider during charging to prevent premature hits
                Collider projCollider = chargingProjectile.GetComponent<Collider>();
                if (projCollider != null) projCollider.enabled = false;
            }
        }

        // Startup phase - emission starts charging immediately
        float startupElapsed = 0f;
        while (startupElapsed < startupTime)
        {
            if (earlyRelease)
            {
                // Released during startup, fire immediately after startup completes
                startupElapsed += Time.deltaTime;
                totalElapsed += Time.deltaTime;

                // Update emission during early release
                currentChargeTime = Mathf.Clamp(totalElapsed, 0f, weapon.chargeTime);
                float chargePercent = totalElapsed / totalChargeTime;
                UpdateEmission(weapon, chargePercent);

                if (startupElapsed >= startupTime)
                {
                    inStartup = false;
                    StartCoroutine(FireSequence());
                    yield break;
                }
            }
            else
            {
                startupElapsed += Time.deltaTime;
                totalElapsed += Time.deltaTime;

                // Update emission during startup
                currentChargeTime = Mathf.Clamp(totalElapsed, 0f, weapon.chargeTime);
                float chargePercent = totalElapsed / totalChargeTime;
                UpdateEmission(weapon, chargePercent);
            }
            yield return null;
        }

        inStartup = false;

        // Check if released during startup
        if (earlyRelease)
        {
            StartCoroutine(FireSequence());
            yield break;
        }

        Debug.Log($"ChargeAttack: Entering charging phase - totalElapsed={totalElapsed:F2}s");

        // Play charging audio after startup
        if (weaponAudioSource != null && weapon.chargingClip != null)
        {
            weaponAudioSource.PlayOneShot(weapon.chargingClip, weapon.chargingClipVolume);
        }

        // Charging phase - continue emission charging
        while (totalElapsed < totalChargeTime)
        {
            totalElapsed += Time.deltaTime;
            currentChargeTime = Mathf.Clamp(totalElapsed, 0f, weapon.chargeTime);
            float chargePercent = totalElapsed / totalChargeTime;

            UpdateEmission(weapon, chargePercent);

            // Lerp projectile scale
            if (chargingProjectile != null)
            {
                float scale = Mathf.Lerp(projectileStartScale, projectileMaxScale, chargePercent);
                chargingProjectile.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Fully charged
        isCharged = true;

        Debug.Log($"ChargeAttack: Fully charged! Starting flash emission");

        // Trigger charged animation
        if (animator != null)
        {
            animator.SetTrigger("charged");
        }

        // Stop charging clip and start charged loop
        if (weaponAudioSource != null)
        {
            weaponAudioSource.Stop();
            if (weapon.chargedLoop != null)
            {
                weaponAudioSource.clip = weapon.chargedLoop;
                weaponAudioSource.volume = weapon.chargedLoopVolume;
                weaponAudioSource.loop = true;
                weaponAudioSource.Play();
            }
        }

        // Start flash coroutine
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashEmission());
    }

    void UpdateEmission(Weapon weapon, float chargePercent)
    {
        currentEmissionLerp = chargePercent;
    }

    IEnumerator FireSequence()
    {
        // Wait for fire delay
        yield return new WaitForSeconds(fireTime);

        // Fire projectile
        FireProjectile();

        // Lerp emission down
        if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
        emissionCoroutine = StartCoroutine(LerpEmissionDown());

        isCharging = false;
        isCharged = false;

        // Check for buffered input and start new charge
        if (inputBuffered)
        {
            inputBuffered = false;
            StartCharge();
        }
    }

    IEnumerator FlashEmission()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null) yield break;

        float chargeValue = 1f;
        float flashValue = weapon.chargedFlash / weapon.chargeEmit;

        while (isCharged && isCharging)
        {
            // Ping pong from chargeEmit to chargedFlash
            float elapsed = 0f;
            float duration = 0.05f;

            // Up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                currentEmissionLerp = Mathf.Lerp(chargeValue, flashValue, t);
                yield return null;
            }

            // Down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                currentEmissionLerp = Mathf.Lerp(flashValue, chargeValue, t);
                yield return null;
            }
        }

        // Reset to base when flash ends
        currentEmissionLerp = 0f;
    }

    IEnumerator LerpEmissionDown()
    {
        float elapsed = 0f;
        float duration = 0.1f;
        float startLerp = currentEmissionLerp;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            currentEmissionLerp = Mathf.Lerp(startLerp, 0f, t);
            yield return null;
        }

        // Ensure fully restored to base
        currentEmissionLerp = 0f;
    }

    void FireProjectile()
    {
        Weapon weapon = GetWeapon();
        if (weapon == null || weapon.projectilePrefab == null || projectileOrigin == null)
        {
            Debug.LogWarning("ChargeAttack: Projectile prefab or origin not assigned");
            return;
        }

        HasFired = true;

        // Calculate charge percent
        float chargePercent = currentChargeTime / weapon.chargeTime;

        Debug.Log($"ChargeAttack: Firing projectile - chargeTime={currentChargeTime:F2}s, chargePercent={chargePercent:F2}");

        // Lerp stats
        float damage = Mathf.Lerp(weapon.damageRange.x, weapon.damageRange.y, chargePercent);
        float range = Mathf.Lerp(weapon.rangeRange.x, weapon.rangeRange.y, chargePercent);
        float speed = Mathf.Lerp(weapon.speedRange.x, weapon.speedRange.y, chargePercent);
        float scale = Mathf.Lerp(1f, 3f, chargePercent);

        // Use the charging projectile and enable its script
        if (chargingProjectile != null && chargingProjectileScript != null)
        {
            // Initialize projectile with playerState (this applies weapon stats)
            chargingProjectileScript.Initialize(playerState);

            // Enable the script and collider so it can move and hit
            chargingProjectileScript.enabled = true;
            Collider projCollider = chargingProjectile.GetComponent<Collider>();
            if (projCollider != null) projCollider.enabled = true;

            // Override stats based on charge level
            chargingProjectileScript.damage = Mathf.RoundToInt(damage);
            chargingProjectileScript.projectileSpeed = speed;
            chargingProjectileScript.dropOff = (1f - range) * 20f;

            // Modulate audio volume based on charge (40%-100%)
            float volumeMod = Mathf.Lerp(0.4f, 1f, chargePercent);
            chargingProjectileScript.volumeModifier = volumeMod;

            // Scale trail renderers on projectileObject children
            if (chargingProjectileScript.projectileObject != null)
            {
                TrailRenderer[] trails = chargingProjectileScript.projectileObject.GetComponentsInChildren<TrailRenderer>();
                foreach (TrailRenderer trail in trails)
                {
                    trail.widthMultiplier *= scale;
                }
            }

            // Clear references
            chargingProjectile = null;
            chargingProjectileScript = null;
        }

        /* OLD INSTANTIATION CODE (commented out for potential revert)
        // Instantiate projectile
        GameObject projectile = Instantiate(weapon.projectilePrefab, projectileOrigin.position, projectileOrigin.rotation);
        projectile.transform.localScale = Vector3.one * scale;

        // Apply stats to projectile
        PlayerProjectile projScript = projectile.GetComponent<PlayerProjectile>();
        if (projScript != null)
        {
            projScript.damage = Mathf.RoundToInt(damage);
            projScript.projectileSpeed = speed;
            projScript.dropOff = (1f - range) * 20f;

            // Modulate audio volume based on charge (40%-100%)
            float volumeMod = Mathf.Lerp(0.4f, 1f, chargePercent);
            projScript.volumeModifier = volumeMod;

            // Scale trail renderers on projectileObject children
            if (projScript.projectileObject != null)
            {
                TrailRenderer[] trails = projScript.projectileObject.GetComponentsInChildren<TrailRenderer>();
                foreach (TrailRenderer trail in trails)
                {
                    trail.widthMultiplier *= scale;
                }
            }
        }
        */
    }

    void OnDisable()
    {
        // Stop all coroutines when disabled
        if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
        if (emissionCoroutine != null) StopCoroutine(emissionCoroutine);
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        // Destroy charging projectile if it exists
        if (chargingProjectile != null)
        {
            Destroy(chargingProjectile);
            chargingProjectile = null;
            chargingProjectileScript = null;
        }

        // Restore original emission colors
        RestoreEmissionColors();

        isCharging = false;
        isCharged = false;
    }

    void OnDestroy()
    {
        // Destroy charging projectile if it exists
        if (chargingProjectile != null)
        {
            Destroy(chargingProjectile);
            chargingProjectile = null;
            chargingProjectileScript = null;
        }

        // Restore original emission colors
        RestoreEmissionColors();
    }

    void RestoreEmissionColors()
    {
        currentEmissionLerp = 0f;
    }
}
