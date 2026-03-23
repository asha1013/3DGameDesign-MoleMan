using UnityEngine;
using System.Collections;
using NaughtyAttributes;

public class Boss : MonoBehaviour
{
    [BoxGroup("Stats")] public string bossName;
    [BoxGroup("Stats")] public int maxHP;

    [BoxGroup("State")] [ReadOnly] public int currentHP;
    [BoxGroup("State")] [ReadOnly] public bool isAttacking;
    [BoxGroup("State")] [ReadOnly] public bool canAttack;
    [BoxGroup("State")] [ReadOnly] public bool isDead;
    [BoxGroup("State")] [ReadOnly] private bool dying = false;
    [BoxGroup("State")] [ReadOnly] public bool inWindup;
    [BoxGroup("State")] [ReadOnly] private int lastAttackIndex = 0;

    private Coroutine attackCoroutine;

    [Foldout("Visual State References")] [ReadOnly] [SerializeField] GameObject idleChild;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] GameObject windupSlamChild;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] GameObject windupShootChild;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] GameObject attackSlamChild;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] GameObject attackShootChild;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] Material idleMaterial;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] Material windupSlamMaterial;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] Material windupShootMaterial;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] Material attackSlamMaterial;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] Material attackShootMaterial;
    [Foldout("Visual State References")] [ReadOnly] [SerializeField] MeshRenderer flashRenderer;

    [BoxGroup("Drop")] [SerializeField] GameObject bossDrop;

    // Attack 1: Player Tracking Rapid Fire
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneWindupTime = 1f;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneAttackTime = 3f;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneCooldownTime = 2f;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneFireRate = 0.2f;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneProjectileSpeed = 10f;
    [BoxGroup("Attack 1 - Rapid Fire")] public GameObject attackOneProjectilePrefab;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneFlashIntensity = 20f;
    [BoxGroup("Attack 1 - Rapid Fire")] public float attackOneTrackingSpeed = 5f;
    [BoxGroup("Attack 1 - Rapid Fire")] [ReadOnly] private float lastAttackOneTime = -999f;

    // Attack 2: Bullet Hell
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoWindupTime = 2f;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoAttackTime = 5f;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoCooldownTime = 3f;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoFireRate = 0.5f;
    [BoxGroup("Attack 2 - Bullet Hell")] public int attackTwoDensity = 12;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoRadius = 3f;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoWaveHeight = 1f;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoProjectileSpeed = 8f;
    [BoxGroup("Attack 2 - Bullet Hell")] public GameObject attackTwoProjectilePrefab;
    [BoxGroup("Attack 2 - Bullet Hell")] public float attackTwoFlashIntensity = 20f;
    [BoxGroup("Attack 2 - Bullet Hell")] [ReadOnly] private float lastAttackTwoTime = -999f;

    // Attack 3: Homing Crystal
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeWindupTime = 1.5f;
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeAttackTime = 2f;
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeCooldownTime = 4f;
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeHomingSpeed = 3f;
    [BoxGroup("Attack 3 - Homing Crystal")] public int attackThreeCrystalHP = 10;
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeProjectileSpeed = 5f;
    [BoxGroup("Attack 3 - Homing Crystal")] public GameObject attackThreeProjectilePrefab;
    [BoxGroup("Attack 3 - Homing Crystal")] public Sprite homingCrystalWhole;
    [BoxGroup("Attack 3 - Homing Crystal")] public Sprite homingCrystalCracked;
    [BoxGroup("Attack 3 - Homing Crystal")] public float attackThreeFlashIntensity = 20f;
    [BoxGroup("Attack 3 - Homing Crystal")] [ReadOnly] private float lastAttackThreeTime = -999f;

    // Attack 4: Crystal Wave
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourWindupTime = 1f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourAttackTime = 8f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourCooldownTime = 3f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourFireRate = 2f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourRadius = 2f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourRadiusDist = 0.1f;
    [BoxGroup("Attack 4 - Crystal Wave")] public int attackFourDensity = 16;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourWaveStep = 1f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourWaveSpeed = 0.3f;
    [BoxGroup("Attack 4 - Crystal Wave")] public GameObject[] attackFourProjectilePrefabs = new GameObject[4];
    [BoxGroup("Attack 4 - Crystal Wave")] public AudioClip attackFourWaveClip;
    [BoxGroup("Attack 4 - Crystal Wave")] [Range(0f, 1f)] public float attackFourWaveClipVolume = 1f;
    [BoxGroup("Attack 4 - Crystal Wave")] public float attackFourFlashIntensity = 20f;
    [BoxGroup("Attack 4 - Crystal Wave")] [ReadOnly] private float lastAttackFourTime = -999f;

    [BoxGroup("Shared")] [SerializeField] GameObject projectileOrigin;

    [BoxGroup("Audio Clips")] [SerializeField] AudioClip windupClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float windupClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip attackClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float attackClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip dieClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float dieClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip damagedClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float damagedClipVolume = 1f;

    private Billboard billboard;
    private AudioSource audioSource;
    private GameObject player;
    private Color originalEmissionColor;
    private float originalBillboardTurnSpeed;

    public enum BossVisualState { Idle, WindupSlam, WindupShoot, AttackSlam, AttackShoot }

    void Awake()
    {
        billboard = GetComponentInChildren<Billboard>();
        if (GetComponent<AudioSource>() != null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError($"{bossName}: Player not found");
            return;
        }

        if (billboard != null) originalBillboardTurnSpeed = billboard.turnSpeed;

        // Create material instances to prevent shared material modifications
        if (idleMaterial != null) idleMaterial = new Material(idleMaterial);
        if (windupSlamMaterial != null) windupSlamMaterial = new Material(windupSlamMaterial);
        if (windupShootMaterial != null) windupShootMaterial = new Material(windupShootMaterial);
        if (attackSlamMaterial != null) attackSlamMaterial = new Material(attackSlamMaterial);
        if (attackShootMaterial != null) attackShootMaterial = new Material(attackShootMaterial);

        if (flashRenderer != null)
        {
            flashRenderer.material = new Material(flashRenderer.material);
            originalEmissionColor = flashRenderer.material.GetColor("_EmissionColor");
        }

        SetVisualState(BossVisualState.Idle);

        currentHP = maxHP;
        canAttack = true;

        enabled = false;
    }

    void Update()
    {
        if (currentHP <= 0 && !dying) StartCoroutine(Die());

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Triggering Debug attack");
            StartAttackSequence();
        }
        #endif

        if (player == null || isAttacking || !canAttack) return;

        float playerDistance = Vector3.Distance(transform.position, player.transform.position);

        // Attack selection logic
        if (canAttack && !isAttacking)
        {
            int chosenAttack = ChooseNextAttack(playerDistance);
            if (chosenAttack > 0)
            {
                StartAttackSequence(chosenAttack);
            }
        }
    }

    int ChooseNextAttack(float playerDistance)
    {
        // Build list of valid attacks (cooldown ready)
        bool attack1Valid = Time.time - lastAttackOneTime >= attackOneCooldownTime;
        bool attack2Valid = Time.time - lastAttackTwoTime >= attackTwoCooldownTime;
        bool attack3Valid = Time.time - lastAttackThreeTime >= attackThreeCooldownTime;
        bool attack4Valid = Time.time - lastAttackFourTime >= attackFourCooldownTime;

        // Distance-based filtering (example logic, adjust as needed)
        // Attack 1 (rapid fire): Good for any distance
        // Attack 2 (bullet hell): Medium to far distance
        // Attack 3 (homing): Far distance
        // Attack 4 (crystal wave): Close to medium distance

        // Simple cyclic selection with cooldown checks
        int[] attackOrder = { 1, 2, 3, 4 };
        int startIndex = lastAttackIndex % attackOrder.Length;

        for (int i = 0; i < attackOrder.Length; i++)
        {
            int index = (startIndex + i) % attackOrder.Length;
            int attackNum = attackOrder[index];

            bool isValid = false;
            switch (attackNum)
            {
                case 1: isValid = attack1Valid; break;
                case 2: isValid = attack2Valid; break;
                case 3: isValid = attack3Valid; break;
                case 4: isValid = attack4Valid; break;
            }

            if (isValid)
            {
                return attackNum;
            }
        }

        return 0; // No valid attack
    }

    void StartAttackSequence(int attackIndex = 1)
    {
        if (attackCoroutine != null) return;

        lastAttackIndex = attackIndex;

        switch (attackIndex)
        {
            case 1:
                attackCoroutine = StartCoroutine(AttackOne_RapidFire());
                break;
            case 2:
                attackCoroutine = StartCoroutine(AttackTwo_BulletHell());
                break;
            case 3:
                attackCoroutine = StartCoroutine(AttackThree_HomingCrystal());
                break;
            case 4:
                attackCoroutine = StartCoroutine(AttackFour_CrystalWave());
                break;
        }
    }

    public void SetVisualState(BossVisualState state)
    {
        // Deactivate all children
        if (idleChild != null) idleChild.SetActive(false);
        if (windupSlamChild != null) windupSlamChild.SetActive(false);
        if (windupShootChild != null) windupShootChild.SetActive(false);
        if (attackSlamChild != null) attackSlamChild.SetActive(false);
        if (attackShootChild != null) attackShootChild.SetActive(false);

        // Activate appropriate state
        GameObject targetChild = null;
        Material targetMaterial = null;

        switch (state)
        {
            case BossVisualState.Idle:
                targetChild = idleChild;
                targetMaterial = idleMaterial;
                break;
            case BossVisualState.WindupSlam:
                targetChild = windupSlamChild;
                targetMaterial = windupSlamMaterial;
                break;
            case BossVisualState.WindupShoot:
                targetChild = windupShootChild;
                targetMaterial = windupShootMaterial;
                break;
            case BossVisualState.AttackSlam:
                targetChild = attackSlamChild;
                targetMaterial = attackSlamMaterial;
                break;
            case BossVisualState.AttackShoot:
                targetChild = attackShootChild;
                targetMaterial = attackShootMaterial;
                break;
        }

        if (targetChild != null)
        {
            targetChild.SetActive(true);
        }
        else if (idleChild != null && targetMaterial != null)
        {
            idleChild.SetActive(true);
            MeshRenderer renderer = idleChild.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.material = targetMaterial;
        }
    }

    IEnumerator AttackOne_RapidFire()
    {
        isAttacking = true;
        canAttack = false;
        lastAttackOneTime = Time.time;

        // Windup
        SetVisualState(BossVisualState.WindupShoot);
        inWindup = true;
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission(attackOneFlashIntensity, attackOneWindupTime));
        yield return new WaitForSeconds(attackOneWindupTime);

        // Attack - Temporarily increase billboard turn speed for tracking
        SetVisualState(BossVisualState.AttackShoot);
        inWindup = false;
        if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

        if (billboard != null) billboard.turnSpeed = attackOneTrackingSpeed;

        float elapsed = 0f;
        while (elapsed < attackOneAttackTime)
        {
            if (attackOneProjectilePrefab != null && projectileOrigin != null && player != null)
            {
                Vector3 direction = (player.transform.position - projectileOrigin.transform.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                GameObject projectile = Instantiate(attackOneProjectilePrefab, projectileOrigin.transform.position, rotation);

                EnemyProjectile projScript = projectile.GetComponent<EnemyProjectile>();
                if (projScript != null)
                {
                    projScript.projectileSpeed = attackOneProjectileSpeed;
                }
            }

            yield return new WaitForSeconds(attackOneFireRate);
            elapsed += attackOneFireRate;
        }

        // Restore original billboard speed
        if (billboard != null) billboard.turnSpeed = originalBillboardTurnSpeed;

        SetVisualState(BossVisualState.Idle);
        isAttacking = false;

        yield return new WaitForSeconds(attackOneCooldownTime);
        canAttack = true;
        attackCoroutine = null;
    }

    IEnumerator AttackTwo_BulletHell()
    {
        isAttacking = true;
        canAttack = false;
        lastAttackTwoTime = Time.time;

        // Two-phase windup: windupShoot for 0.35s, then idle for remainder
        SetVisualState(BossVisualState.WindupShoot);
        inWindup = true;
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        yield return new WaitForSeconds(0.35f);

        SetVisualState(BossVisualState.Idle);
        StartCoroutine(FlashEmission(attackTwoFlashIntensity, attackTwoWindupTime - 0.35f));
        yield return new WaitForSeconds(attackTwoWindupTime - 0.35f);

        // Attack - Alternate between idle and windupShoot, spawning waves
        inWindup = false;
        bool alternateOffset = false;
        float elapsed = 0f;
        bool isWindupShootActive = false;

        while (elapsed < attackTwoAttackTime)
        {
            isWindupShootActive = !isWindupShootActive;

            if (isWindupShootActive)
            {
                SetVisualState(BossVisualState.WindupShoot);
                if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

                // Spawn circle of projectiles
                float offset = alternateOffset ? (180f / attackTwoDensity) : 0f;
                for (int i = 0; i < attackTwoDensity; i++)
                {
                    float angle = (360f / attackTwoDensity) * i + offset;
                    Quaternion rotation = Quaternion.Euler(0, angle, 0);
                    Vector3 direction = rotation * Vector3.forward;
                    Vector3 spawnPos = transform.position + direction * attackTwoRadius;
                    spawnPos.y = transform.position.y + attackTwoWaveHeight;

                    GameObject projectile = Instantiate(attackTwoProjectilePrefab, spawnPos, rotation);
                    EnemyProjectile projScript = projectile.GetComponent<EnemyProjectile>();
                    if (projScript != null)
                    {
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.linearVelocity = direction * attackTwoProjectileSpeed;
                        }
                    }
                }

                alternateOffset = !alternateOffset;
            }
            else
            {
                SetVisualState(BossVisualState.Idle);
            }

            yield return new WaitForSeconds(attackTwoFireRate);
            elapsed += attackTwoFireRate;
        }

        SetVisualState(BossVisualState.Idle);
        isAttacking = false;

        yield return new WaitForSeconds(attackTwoCooldownTime);
        canAttack = true;
        attackCoroutine = null;
    }

    IEnumerator AttackThree_HomingCrystal()
    {
        isAttacking = true;
        canAttack = false;
        lastAttackThreeTime = Time.time;

        // Windup
        SetVisualState(BossVisualState.WindupShoot);
        inWindup = true;
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission(attackThreeFlashIntensity, attackThreeWindupTime));
        yield return new WaitForSeconds(attackThreeWindupTime);

        // Attack - Fire homing crystal
        SetVisualState(BossVisualState.AttackShoot);
        inWindup = false;
        if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

        if (attackThreeProjectilePrefab != null && projectileOrigin != null && player != null)
        {
            Vector3 direction = (player.transform.position - projectileOrigin.transform.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject projectile = Instantiate(attackThreeProjectilePrefab, projectileOrigin.transform.position, rotation);

            HomingCrystalProjectile homingScript = projectile.GetComponent<HomingCrystalProjectile>();
            if (homingScript != null)
            {
                homingScript.Initialize(attackThreeHomingSpeed, attackThreeCrystalHP, attackThreeProjectileSpeed, homingCrystalWhole, homingCrystalCracked);
            }
        }

        yield return new WaitForSeconds(attackThreeAttackTime);

        SetVisualState(BossVisualState.Idle);
        isAttacking = false;

        yield return new WaitForSeconds(attackThreeCooldownTime);
        canAttack = true;
        attackCoroutine = null;
    }

    IEnumerator AttackFour_CrystalWave()
    {
        isAttacking = true;
        canAttack = false;
        lastAttackFourTime = Time.time;

        // Windup
        SetVisualState(BossVisualState.WindupSlam);
        inWindup = true;
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission(attackFourFlashIntensity, attackFourWindupTime));
        yield return new WaitForSeconds(attackFourWindupTime);

        // Attack - Alternate between attackSlam and windupSlam, spawning waves
        inWindup = false;
        float elapsed = 0f;
        bool isAttackSlamActive = false;

        while (elapsed < attackFourAttackTime)
        {
            isAttackSlamActive = !isAttackSlamActive;

            if (isAttackSlamActive)
            {
                SetVisualState(BossVisualState.AttackSlam);
                if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

                // Spawn wave manager
                GameObject waveManager = new GameObject("CrystalWaveManager");
                waveManager.transform.position = transform.position;
                CrystalWaveManager waveScript = waveManager.AddComponent<CrystalWaveManager>();
                waveScript.Initialize(
                    attackFourProjectilePrefabs,
                    attackFourRadius,
                    attackFourRadiusDist,
                    attackFourDensity,
                    attackFourWaveStep,
                    attackFourWaveSpeed,
                    attackFourWaveClip,
                    attackFourWaveClipVolume
                );
            }
            else
            {
                SetVisualState(BossVisualState.WindupSlam);
            }

            yield return new WaitForSeconds(attackFourFireRate);
            elapsed += attackFourFireRate;
        }

        SetVisualState(BossVisualState.Idle);
        isAttacking = false;

        yield return new WaitForSeconds(attackFourCooldownTime);
        canAttack = true;
        attackCoroutine = null;
    }

    IEnumerator FlashEmission(float flashIntensity, float duration)
    {
        if (flashRenderer == null) yield break;

        float elapsed = 0f;
        float halfDuration = duration / 2f;
        float baseIntensity = originalEmissionColor.maxColorComponent;
        Color normalizedColor = baseIntensity > 0 ? originalEmissionColor / baseIntensity : Color.white;

        // Flash up
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float intensity = Mathf.SmoothStep(baseIntensity, flashIntensity, t);
            flashRenderer.material.SetColor("_EmissionColor", normalizedColor * intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Flash down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            float intensity = Mathf.SmoothStep(flashIntensity, baseIntensity, t);
            flashRenderer.material.SetColor("_EmissionColor", normalizedColor * intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }

        flashRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
    }

    IEnumerator Die()
    {
        inWindup = false;
        dying = true;
        isDead = true;

        if (dieClip != null && audioSource != null) audioSource.PlayOneShot(dieClip, dieClipVolume);

        RoomStarter room = GetComponentInParent<RoomStarter>();
        if (room != null) room.CheckForRemainingEnemies();

        yield return new WaitForSeconds(0.5f);
        if (bossDrop != null) Instantiate(bossDrop, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        inWindup = false;
        if (!Application.isPlaying) return;

        // Clean up material instances
        if (flashRenderer != null && flashRenderer.material != null && flashRenderer.material.name.Contains("(Instance)"))
            Destroy(flashRenderer.material);
        if (idleMaterial != null && idleMaterial.name.Contains("(Instance)"))
            Destroy(idleMaterial);
        if (windupSlamMaterial != null && windupSlamMaterial.name.Contains("(Instance)"))
            Destroy(windupSlamMaterial);
        if (windupShootMaterial != null && windupShootMaterial.name.Contains("(Instance)"))
            Destroy(windupShootMaterial);
        if (attackSlamMaterial != null && attackSlamMaterial.name.Contains("(Instance)"))
            Destroy(attackSlamMaterial);
        if (attackShootMaterial != null && attackShootMaterial.name.Contains("(Instance)"))
            Destroy(attackShootMaterial);
    }

    public void GetHit(int damage, float knockback, GameObject projectile)
    {
        currentHP -= damage;

        if (damagedClip != null && audioSource != null && !dying)
            audioSource.PlayOneShot(damagedClip, damagedClipVolume);

        StartCoroutine(DamageFlash());
    }

    IEnumerator DamageFlash()
    {
        Color idleOriginal = idleMaterial != null ? idleMaterial.color : Color.white;
        Color windupSlamOriginal = windupSlamMaterial != null ? windupSlamMaterial.color : Color.white;
        Color windupShootOriginal = windupShootMaterial != null ? windupShootMaterial.color : Color.white;
        Color attackSlamOriginal = attackSlamMaterial != null ? attackSlamMaterial.color : Color.white;
        Color attackShootOriginal = attackShootMaterial != null ? attackShootMaterial.color : Color.white;

        Color flashColor = new Color(0.5f, 0.25f, 0.25f, 1.0f);

        if (idleMaterial != null) idleMaterial.color = flashColor;
        if (windupSlamMaterial != null) windupSlamMaterial.color = flashColor;
        if (windupShootMaterial != null) windupShootMaterial.color = flashColor;
        if (attackSlamMaterial != null) attackSlamMaterial.color = flashColor;
        if (attackShootMaterial != null) attackShootMaterial.color = flashColor;

        yield return new WaitForSeconds(0.15f);

        if (idleMaterial != null) idleMaterial.color = idleOriginal;
        if (windupSlamMaterial != null) windupSlamMaterial.color = windupSlamOriginal;
        if (windupShootMaterial != null) windupShootMaterial.color = windupShootOriginal;
        if (attackSlamMaterial != null) attackSlamMaterial.color = attackSlamOriginal;
        if (attackShootMaterial != null) attackShootMaterial.color = attackShootOriginal;
    }
}
