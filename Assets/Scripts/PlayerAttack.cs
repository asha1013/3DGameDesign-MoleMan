using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using NaughtyAttributes;

public class PlayerAttack : MonoBehaviour
{
    [BoxGroup("References")] public Animator leftHandAnimator;
    [BoxGroup("References")] [SerializeField] Collider punchColider;
    [BoxGroup("References")] [SerializeField] Transform projectileOrigin;

    [BoxGroup("Hit Feedback")] public AudioClip hitConfirmClip;
    [BoxGroup("Hit Feedback")] [UnityEngine.Range(0f, 1f)] public float hitConfirmClipVolume = 1f;
    [BoxGroup("Hit Feedback")] public GameObject hitCrosshair;

    [BoxGroup("Feedback")] public MMF_Player shootFeedback;

    private AudioSource audioSource;
    private AttackHitbox hitboxScript;
    private PlayerState playerState;
    private ChargeAttack chargeAttack;
    private GroundTargetAttack groundTargetAttack;
    private Color cachedBaseEmission;
    private Renderer weaponRenderer;
    private Coroutine crosshairCoroutine;
    private Coroutine currentAttackCoroutine;
    private enum AttackType { None, Melee, Ranged }
    private AttackType currentAttack = AttackType.None;
    private float currentAttackElapsed;
    private float currentAttackDuration;
    private float lastMeleeTime = -999f;
    private float lastRangedTime = -999f;
    private int attackStateHash;
    private float currentEmissionLerp = 0f;

    void Start()
    {
        punchColider.enabled = false;
        hitboxScript = punchColider.GetComponent<AttackHitbox>();
        audioSource = GetComponent<AudioSource>();
        playerState = GetComponent<PlayerState>();
        RefreshChargeAttack();
        CacheFireEmissions();
    }

    void CacheFireEmissions()
    {
        if (playerState != null && playerState.equippedWeapon != null)
        {
            weaponRenderer = playerState.weaponObject.GetComponentInChildren<Renderer>();
            if (weaponRenderer != null && weaponRenderer.material != null)
            {
                cachedBaseEmission = weaponRenderer.material.GetColor("_EmissionColor");
            }
        }
    }

    public void RefreshChargeAttack()
    {
        if (playerState == null || playerState.weaponObject == null) return;

        // Look for unique attack components on weapon
        if (playerState.weaponObject.GetComponent<ChargeAttack>()!=null) chargeAttack = playerState.weaponObject.GetComponent<ChargeAttack>();
        if (playerState.weaponObject.GetComponent<GroundTargetAttack>()!=null)  groundTargetAttack = playerState.weaponObject.GetComponent<GroundTargetAttack>();

        // Re-cache emissions when weapon changes
        CacheFireEmissions();

        // Update projectile origin position if weapon overrides it
        UpdateProjectileOrigin();

        if (playerState != null && playerState.equippedWeapon != null)
        {
            Debug.Log($"RefreshChargeAttack: hasChargeAttack={playerState.equippedWeapon.hasChargeAttack}, chargeAttack component found={chargeAttack != null}");
        }
    }

    void UpdateProjectileOrigin()
    {
        if (playerState == null || playerState.equippedWeapon == null || projectileOrigin == null) return;

        if (playerState.equippedWeapon.overrideProjectileOrigin)
        {
            projectileOrigin.localPosition = playerState.equippedWeapon.projectileOriginLocalPosition;
        }
    }

    void LateUpdate()
    {
        // Apply emission lerp to current renderer material
        if (weaponRenderer != null && weaponRenderer.material != null && playerState != null && playerState.equippedWeapon != null)
        {
            float baseIntensity = cachedBaseEmission.maxColorComponent;
            Color normalized = baseIntensity > 0 ? cachedBaseEmission / baseIntensity : Color.white;
            Color targetColor = normalized * Mathf.Lerp(baseIntensity, playerState.equippedWeapon.emitIntensity, currentEmissionLerp);
            weaponRenderer.material.SetColor("_EmissionColor", targetColor);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float meleeSpeed = playerState != null ? playerState.meleeSpeed : 0.64f;
        float fireRate = playerState != null ? playerState.fireRate : 0.57f;

        // Check for unique attack types
        bool hasChargeAttack = playerState != null && playerState.equippedWeapon != null && playerState.equippedWeapon.hasChargeAttack;
        bool hasAimAttack = playerState != null && playerState.equippedWeapon != null && playerState.equippedWeapon.hasAimAttack;

        if (hasChargeAttack && chargeAttack != null)
        {
            // Check release separately - always check regardless of cooldown/animator state
            if (Input.GetMouseButtonUp(0) && chargeAttack.IsCharging)
            {
                chargeAttack.ReleaseCharge();
                currentAttack = AttackType.None;
            }

            // Charge attack input - only check after cooldown
            if (Time.time - lastRangedTime >= fireRate)
            {
                if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
                {
                    // Check if weapon animator is in idle state
                    bool isIdle = playerState.weaponAnimator == null || playerState.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle");

                    if (isIdle)
                    {
                        // check if currently in melee attack's no-cancel window (70%)
                        if (currentAttack == AttackType.Melee && currentAttackElapsed < currentAttackDuration * 0.7f)
                        {
                            // Don't start charge, but continue to check for release below
                        }
                        else
                        {
                            chargeAttack.StartCharge();
                            currentAttack = AttackType.Ranged;
                            lastRangedTime = Time.time;
                        }
                    }
                }
            }
        }
        else if (hasAimAttack && groundTargetAttack != null)
        {
            // Aim attack input
            if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
            {
                // Check if weapon animator is not in idle state
                if (playerState.weaponAnimator != null && !playerState.weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) return;

                // check if currently in melee attack's no-cancel window (70%)
                if (currentAttack == AttackType.Melee && currentAttackElapsed < currentAttackDuration * 0.7f) return;

                groundTargetAttack.StartAim();
                currentAttack = AttackType.Ranged;
                lastRangedTime = Time.time;
            }
            else if (Input.GetMouseButtonUp(0) && (groundTargetAttack.IsAiming || groundTargetAttack.IsInStartup))
            {
                groundTargetAttack.ReleaseAim();
                currentAttack = AttackType.None;
            }
        }
        else
        {
            // Normal ranged attack input
            if (Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            {
                // check if ranged is on cooldown
                if (Time.time - lastRangedTime < fireRate) return;

                // check if currently in melee attack's no-cancel window (70%)
                if (currentAttack == AttackType.Melee && currentAttackElapsed < currentAttackDuration * 0.7f) return;

                // cancel current attack if in cancel window
                if (currentAttack != AttackType.None && currentAttackCoroutine != null)
                {
                    StopCoroutine(currentAttackCoroutine);
                    if (currentAttack == AttackType.Melee) punchColider.enabled = false;
                }

                currentAttackCoroutine = StartCoroutine(PlayerRanged());
                lastRangedTime = Time.time;
            }
        }

        // melee attack input (always available)
        if (Input.GetMouseButton(1))
        {
            // check if melee is on cooldown
            if (Time.time - lastMeleeTime < meleeSpeed) return;

            // check if charge/aim attack is active and hasn't fired yet
            if (hasChargeAttack && chargeAttack != null && chargeAttack.IsCharging && !chargeAttack.HasFired) return;
            if (hasAimAttack && groundTargetAttack != null && (groundTargetAttack.IsAiming || groundTargetAttack.IsInStartup) && !groundTargetAttack.HasFired) return;

            // check if currently in ranged attack's no-cancel window (70%)
            if (currentAttack == AttackType.Ranged && currentAttackElapsed < currentAttackDuration * 0.7f) return;

            // cancel current attack if in cancel window
            if (currentAttack != AttackType.None && currentAttackCoroutine != null)
            {
                StopCoroutine(currentAttackCoroutine);
            }

            // Cancel unique attacks
            if (hasChargeAttack && chargeAttack != null && chargeAttack.IsCharging)
            {
                chargeAttack.ReleaseCharge();
            }
            if (hasAimAttack && groundTargetAttack != null && (groundTargetAttack.IsAiming || groundTargetAttack.IsInStartup))
            {
                groundTargetAttack.CancelAim();
            }

            leftHandAnimator.SetTrigger("Attack");
            currentAttackCoroutine = StartCoroutine(PlayerMelee());
            lastMeleeTime = Time.time;
        }
    }

    IEnumerator PlayerMelee()
    {
        currentAttack = AttackType.Melee;
        currentAttackElapsed = 0f;

        // Get base animation timings from weapon
        float baseStartup = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.baseMeleeStartup : 0.27f;
        float baseActive = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.baseMeleeActive : 0.13f;
        float baseMeleeTime = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.baseMeleeTime : 0.64f;

        currentAttackDuration = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.meleeSpeed : baseMeleeTime;

        float animSpeed = 1f;
        float startup = baseStartup;
        float active = baseActive;

        // if not unique melee, scale animation speed directly with meleeSpeed
        if (playerState != null && !playerState.uniqueMelee)
        {
            animSpeed = baseMeleeTime / currentAttackDuration;
            leftHandAnimator.speed = animSpeed;
            startup = baseStartup / animSpeed;
            active = baseActive / animSpeed;
        }

        // startup
        while (currentAttackElapsed < startup)
        {
            currentAttackElapsed += Time.deltaTime;
            yield return null;
        }

        // active hitbox
        hitboxScript.hasHit = false;
        punchColider.enabled = true;
        float targetElapsed = startup + active;
        while (currentAttackElapsed < targetElapsed)
        {
            currentAttackElapsed += Time.deltaTime;
            yield return null;
        }

        // recovery
        punchColider.enabled = false;
        while (currentAttackElapsed < currentAttackDuration)
        {
            currentAttackElapsed += Time.deltaTime;
            yield return null;
        }

        if (playerState != null && !playerState.uniqueMelee)
        {
            leftHandAnimator.speed = 1f;
        }
        currentAttack = AttackType.None;
        currentAttackCoroutine = null;
    }
   IEnumerator PlayerRanged()
    {
        currentAttack = AttackType.Ranged;
        currentAttackElapsed = 0f;

        // Get base animation timings from weapon
        float baseStartup = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.baseRangedStartup : 0.12f;
        float baseRangedTime = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.baseRangedTime : 0.57f;

        currentAttackDuration = playerState != null && playerState.equippedWeapon != null
            ? playerState.equippedWeapon.fireRate : baseRangedTime;

        float animSpeed = 1f;
        float startup = baseStartup;

        // Set animation speed and trigger attack
        if (playerState != null && playerState.weaponAnimator != null)
        {playerState.weaponAnimator.Play("Attack");
            // if not unique ranged, scale animation speed directly with fireRate
            if (!playerState.uniqueRanged)
            {
                animSpeed = baseRangedTime / currentAttackDuration;
                playerState.weaponAnimator.speed = animSpeed;
                startup = baseStartup / animSpeed;
            }

        }

        // Play shoot sound from weapon
        if (playerState.equippedWeapon != null && playerState.equippedWeapon.hasShootSFX && playerState.equippedWeapon.shootClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(playerState.equippedWeapon.shootClip, playerState.equippedWeapon.shootClipVolume);
        }

        // Check if fire emission is enabled
        bool useFireEmit = playerState != null && playerState.equippedWeapon != null && playerState.equippedWeapon.fireEmit;

        // startup with emission lerp
        float startupElapsed = 0f;
        while (currentAttackElapsed < startup)
        {
            currentAttackElapsed += Time.deltaTime;
            startupElapsed += Time.deltaTime;

            // Lerp emission up during startup
            if (useFireEmit)
            {
                currentEmissionLerp = startupElapsed / startup;
            }

            yield return null;
        }
       // playerState.weaponAnimator.SetTrigger("Attack");
     //  playerState.weaponAnimator.Play("Attack");

        if (shootFeedback != null) shootFeedback.PlayFeedbacks();

        // get projectile from equipped weapon
        if (playerState != null && playerState.equippedWeapon != null && playerState.equippedWeapon.projectilePrefab != null)
        {
            GameObject projectile = Instantiate(playerState.equippedWeapon.projectilePrefab, projectileOrigin.position, Quaternion.identity);
            PlayerProjectile projScript = projectile.GetComponent<PlayerProjectile>();
            if (projScript != null)
            {               
                projScript.Initialize(playerState);
            }
        }
        else
        {
            Debug.LogWarning("Weapon or projectile prefab not assigned");
        }

        // recovery with emission lerp down
        float recoveryStart = currentAttackElapsed;
        float recoveryDuration = currentAttackDuration - startup;
        while (currentAttackElapsed < currentAttackDuration)
        {
            currentAttackElapsed += Time.deltaTime;

            // Lerp emission down during recovery
            if (useFireEmit)
            {
                float recoveryElapsed = currentAttackElapsed - recoveryStart;
                float t = recoveryElapsed / recoveryDuration;
                currentEmissionLerp = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        // Ensure emission is fully restored
        if (useFireEmit)
        {
            currentEmissionLerp = 0f;
        }

        if (playerState != null && !playerState.uniqueRanged)
        {
            if (playerState.weaponAnimator != null)
            {
                playerState.weaponAnimator.speed = 1f;
            }
        }
        currentAttack = AttackType.None;
        currentAttackCoroutine = null;
    }

    public void HitConfirm()
    {
        if (hitConfirmClip != null && audioSource != null) audioSource.PlayOneShot(hitConfirmClip, hitConfirmClipVolume);
        if (hitCrosshair != null)
        {
            if (crosshairCoroutine != null) StopCoroutine(crosshairCoroutine);
            crosshairCoroutine = StartCoroutine(CrosshairFlash());
        }
    }

    IEnumerator CrosshairFlash()
    {
        hitCrosshair.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        hitCrosshair.SetActive(false);
    }

    IEnumerator DelayedCheck()
    {        
        yield return new WaitForSeconds(.1f);
        chargeAttack = playerState.weaponObject.GetComponent<ChargeAttack>();
    }
}
