using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using NaughtyAttributes;
using MoreMountains.Feedbacks;

// Interface for custom attack implementations
public interface IUniqueAttack
{
    IEnumerator ExecuteAttack(EnemyAttack attack, Enemy enemy);
}

#if UNITY_EDITOR
[RequireComponent(typeof(EnemyVariableSetter))]
#endif
[RequireComponent(typeof(Strafer))]
public class Enemy : MonoBehaviour
{
    [BoxGroup("Stats")] public string enemyName;
    [BoxGroup("Stats")] public int maxHP;
    [BoxGroup("Stats")] public float moveSpeed = 3f;
    private float turnSpeed = 15f;
    [BoxGroup("Stats")] public float moveAnimRate = .35f;
    [BoxGroup("Stats")] public EnemyAttack[] attacks;
    private float knockbackDecay = 10f;
    [BoxGroup("Stats")][SerializeField] float flashIntensity = 20f;
    [BoxGroup("Stats")] public bool darkMode = false;
    [BoxGroup("Drop")] [SerializeField] GameObject enemyDrop;

    [BoxGroup("Attack Stats")] public int damage;
    [BoxGroup("Attack Stats")] public float range;
    [BoxGroup("Attack Stats")] public float delay;
    [BoxGroup("Attack Stats")] public float duration;
    [BoxGroup("Attack Stats")] public float cooldown;
    [BoxGroup("Attack Stats")] public bool moveAttack;

    [BoxGroup("Projectile")] [ShowIf("IsProjectileAttack")] [SerializeField] GameObject projectilePrefab;
    [BoxGroup("Projectile")] [ShowIf("IsProjectileAttack")] [SerializeField] GameObject projectileOrigin;


    [BoxGroup("Lunging")] [ShowIf("IsLungingAttack")] public float jumpHeight;
    [BoxGroup("Lunging")] [ShowIf("IsLungingAttack")] public float lungeDistance;
    [BoxGroup("Lunging")] [ShowIf("IsLungingAttack")] public float bounceBack;

    [BoxGroup("State")] [ReadOnly] public int currentHP = 10;
    [BoxGroup("State")] [ReadOnly] public bool isMoving;
    [BoxGroup("State")] [ReadOnly] public bool isAgroed;
    [BoxGroup("State")] [ReadOnly] public bool isAttacking;
    [BoxGroup("State")] [ReadOnly] public bool isDead;
    [BoxGroup("State")] [ReadOnly] public bool canAttack;
    [BoxGroup("State")] [ReadOnly] private bool lungeHitPlayer = false;
    [BoxGroup("State")] [ReadOnly] private bool isSightline;
    [BoxGroup("State")] [ReadOnly] private bool dying = false;
    [BoxGroup("State")] [ReadOnly] Vector3 knockbackVelocity;
    [BoxGroup("State")] [ReadOnly] private float playerDistance;
    [BoxGroup("State")] [ReadOnly] public bool inWindup;

    private Coroutine moveAnimCoroutine;
    private Coroutine attackCoroutine;

    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] GameObject idleChild;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] GameObject moveChild;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] Material idleMaterial;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] Material moveMaterial;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] MeshRenderer flashRenderer;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] GameObject windupChild;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] Material windupMaterial;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] GameObject attackChild;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] Material attackMaterial;
    [Foldout("Auto-Set References")] [ShowIf("IsMeleeOrLungingAttack")] [ReadOnly] [SerializeField] GameObject hitboxObject;
    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] Strafer strafer;

    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] MonsterDeathSFX monsterDeathSFX;

    [Foldout("Auto-Set References")] [ReadOnly] [SerializeField] MonsterHitSFX monsterHitSFX;
    [Foldout("Auto-Set References")] [SerializeField] MMF_Player damageFeedback;

    [BoxGroup("Audio Clips")] [SerializeField] AudioClip agroClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float agroClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip dieClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float dieClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip moveClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float moveClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip damagedClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float damagedClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip windupClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float windupClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] AudioClip attackClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float attackClipVolume = 1f;
    [BoxGroup("Audio Clips")] [ShowIf("IsMeleeOrLungingAttack")] [SerializeField] AudioClip attackHitClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float attackHitClipVolume = 1f;




    private Collider hitboxCollider;
    private AttackHitbox hitboxScript;

    private MeshRenderer idleRenderer;
    private Billboard billBoard;
    private NavMeshAgent navMeshAgent;
    private AudioSource audioSource;
    private Animator animator;
    private Vector3 pathTarget;
    GameObject player;
    Transform projectileOriginTransform;
    private Color originalEmissionColor;

    // Conditional visibility methods for NaughtyAttributes
    private bool IsProjectileAttack()
    {
        return attacks != null && attacks.Length > 0 && attacks[0].attackType == EnemyAttack.AttackType.Projectile;
    }

    private bool IsLungingAttack()
    {
        return attacks != null && attacks.Length > 0 && attacks[0].attackType == EnemyAttack.AttackType.Lunging;
    }

    private bool IsMeleeOrLungingAttack()
    {
        return attacks != null && attacks.Length > 0 && (attacks[0].attackType == EnemyAttack.AttackType.Melee || attacks[0].attackType == EnemyAttack.AttackType.Lunging);
    }



    void Awake()
    {
        if (idleChild != null) idleRenderer = idleChild.GetComponent<MeshRenderer>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        billBoard = GetComponentInChildren<Billboard>();
        if (GetComponent<AudioSource>() != null) audioSource = GetComponent<AudioSource>();
        if (GetComponent<Animator>() != null) animator = GetComponent<Animator>();
        if (GetComponent<MonsterDeathSFX>() != null) monsterDeathSFX = GetComponent<MonsterDeathSFX>();
        if (GetComponent<MonsterHitSFX>() != null) monsterHitSFX = GetComponent<MonsterHitSFX>();

        if (GetComponent<Strafer>() != null) strafer = GetComponent<Strafer>();
        else strafer = gameObject.AddComponent<Strafer>();
    }

    void Start()
    {
        if (billBoard != null) billBoard.turnSpeed = turnSpeed;
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError($"{enemyName}: Player not found");
            return;
        }

        // Snap to nearest navmesh point
        if (navMeshAgent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"{enemyName}: navmesh not found");
            }
        }

        // Create material instances for all states to prevent shared material modifications
        if (idleMaterial != null) idleMaterial = new Material(idleMaterial);
        if (moveMaterial != null) moveMaterial = new Material(moveMaterial);
        if (windupMaterial != null) windupMaterial = new Material(windupMaterial);
        if (attackMaterial != null) attackMaterial = new Material(attackMaterial);

        if (flashRenderer != null)
        {
            flashRenderer.material = new Material(flashRenderer.material);
            originalEmissionColor = flashRenderer.material.GetColor("_EmissionColor");
        }

        SetVisualState(idleMaterial, idleChild);

        if (hitboxObject != null)
        {
            hitboxCollider = hitboxObject.GetComponent<Collider>();
            hitboxScript = hitboxObject.GetComponent<AttackHitbox>();
            if (hitboxScript != null) hitboxScript.enemyOwner = this;
        }

        currentHP = maxHP;
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.stoppingDistance = 0f;
        isAgroed = true;
        canAttack = true;

        enabled = false;
    }


    void Update()
    {
        if (currentHP <= 0 && !dying) StartCoroutine(Die());

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Debug attack trigger
        {
            Debug.Log("Triggering Debug attack");
            AttackOne();
        }
        #endif

        if (player == null) return;

        playerDistance = Vector3.Distance(transform.position, player.transform.position);
        Vector3 playerDirection = (player.transform.position - transform.position).normalized;

        transform.position += knockbackVelocity * Time.deltaTime;
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);

        // Movement
        if  (!isDead && isAgroed && !isAttacking && navMeshAgent != null)
        {
            strafer.CalculatePathTarget(player.transform.position, playerDistance);
            if (strafer.shouldMove)
            {
                navMeshAgent.destination = strafer.pathTarget;
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }
        }
        else if (!isAttacking)
        {
            isMoving = false;
        }

        // Attack if in range with LOS or very close
        if (playerDistance <= range && canAttack && !isAttacking)
        {
            Vector3 rayStart = transform.position + Vector3.up;
            Vector3 rayDirection = (player.transform.position - rayStart).normalized;
            float rayDistance = Vector3.Distance(rayStart, player.transform.position);

            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayDistance))
            {
                if (hit.collider.CompareTag("Player")) isSightline = true;
                else isSightline = false;
            }
            else isSightline = false;

            if (isSightline || playerDistance < 1f) AttackOne();
        }

        if (isMoving && !isAttacking && moveAnimCoroutine == null)
        {
            moveAnimCoroutine = StartCoroutine(MoveAnim());
        }
    }
    
    void AttackOne()
    {
        if (attackCoroutine != null) return;
        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogWarning($"{enemyName}: No attacks configured");
            return;
        }

        if (!isAttacking && canAttack && attacks[0].attackType == EnemyAttack.AttackType.Melee)
            attackCoroutine = StartCoroutine(MeleeAttack(attacks[0]));
        else if (!isAttacking && canAttack && attacks[0].attackType == EnemyAttack.AttackType.Projectile)
            attackCoroutine = StartCoroutine(RangedAttack(attacks[0]));
        else if (!isAttacking && canAttack && attacks[0].attackType == EnemyAttack.AttackType.Lunging)
            attackCoroutine = StartCoroutine(LungingAttack(attacks[0]));
        else if (!isAttacking && canAttack && attacks[0].attackType == EnemyAttack.AttackType.Unique)
            attackCoroutine = StartCoroutine(UniqueAttack(attacks[0]));
    }

    void VisionCheck()
    {
        isSightline = true;
    }

    // Visual state system - swaps between child objects or materials
    public void SetVisualState(Material mat, GameObject child)
    {
        if (idleChild != null) idleChild.SetActive(false);
        if (moveChild != null) moveChild.SetActive(false);
        if (windupChild != null) windupChild.SetActive(false);
        if (attackChild != null) attackChild.SetActive(false);

        if (child != null)
        {
            child.SetActive(true);
        }
        else // No child for state, use idle child and swap material
        {
            if (idleChild != null)
            {
                idleChild.SetActive(true);
                if (mat != null)
                {
                    MeshRenderer renderer = idleChild.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.material = mat;
                }
            }
        }
    }

    public IEnumerator MoveAnim()
    {
        while (isMoving && !isAttacking)
        {
            inWindup=false;
            if (moveChild != null)
                SetVisualState(null, moveChild);
            else
                SetVisualState(moveMaterial, null);

            // This might play too much, here might be a good place to put like a 1% chance of playing
            if (moveClip != null && audioSource != null) audioSource.PlayOneShot(moveClip, moveClipVolume);
            yield return new WaitForSeconds(moveAnimRate);

            if (moveChild != null)
                SetVisualState(null, idleChild);
            else
                SetVisualState(idleMaterial, null);

            yield return new WaitForSeconds(moveAnimRate);
        }

        if (moveChild != null && !isAttacking)
            SetVisualState(null, idleChild);
        else if (!isAttacking)
            SetVisualState(idleMaterial, null);

        moveAnimCoroutine = null;
    }

#region Attacks
    public IEnumerator MeleeAttack(EnemyAttack attack)
    {
        if (navMeshAgent != null && !moveAttack) navMeshAgent.enabled = false;
        isAttacking = true;
        canAttack = false;

        if (moveAnimCoroutine != null)
        {
            StopCoroutine(moveAnimCoroutine);
            moveAnimCoroutine = null;
        }

        // Windup
        SetVisualState(windupMaterial, windupChild);
        inWindup=true;
        UIManager.Instance.WarningIndicator(transform.position);
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission());
        yield return new WaitForSeconds(delay);
        

        // Attack
        SetVisualState(attackMaterial, attackChild);
        inWindup=false;
        if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

        if (hitboxScript != null)
        {
            hitboxScript.damage = damage;
            hitboxScript.hasHit = false;
            hitboxCollider.enabled = true;
        }

        yield return new WaitForSeconds(duration);

        if (hitboxCollider != null) hitboxCollider.enabled = false;
        if (attackHitClip != null && audioSource != null && hitboxScript != null && hitboxScript.hasHit)
            audioSource.PlayOneShot(attackHitClip, attackHitClipVolume);

        SetVisualState(idleMaterial, idleChild);
        isAttacking = false;

        if (navMeshAgent != null) navMeshAgent.enabled = true;

        yield return new WaitForSeconds(cooldown);
        if (strafer != null) strafer.ResetStrafeDirection();
        canAttack = true;
        attackCoroutine = null;
    }

    public IEnumerator RangedAttack(EnemyAttack attack)
    {
        if (navMeshAgent != null && !moveAttack) navMeshAgent.enabled = false;
        isAttacking = true;
        canAttack = false;

        if (moveAnimCoroutine != null)
        {
            StopCoroutine(moveAnimCoroutine);
            moveAnimCoroutine = null;
        }

        // Windup
        SetVisualState(windupMaterial, windupChild);
        inWindup=true;
        UIManager.Instance.WarningIndicator(transform.position);
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission());
        yield return new WaitForSeconds(delay);

        // Shoot
        SetVisualState(attackMaterial, attackChild);
        inWindup=false;
        if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);

        if (projectilePrefab != null && projectileOrigin != null && player != null)
        {
            Transform originTransform = projectileOrigin.transform;
            if (originTransform != null)
            {
                Vector3 direction = (player.transform.position - originTransform.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                Instantiate(projectilePrefab, originTransform.position, rotation);
            }
        }

        yield return new WaitForSeconds(duration);

        SetVisualState(idleMaterial, idleChild);
        isAttacking = false;

        if (navMeshAgent != null) navMeshAgent.enabled = true;

        yield return new WaitForSeconds(cooldown);
        if (strafer != null) strafer.ResetStrafeDirection();
        canAttack = true;
        attackCoroutine = null;
    }

    public IEnumerator UniqueAttack(EnemyAttack attack)
    {
        isAttacking = true;
        canAttack = false;

        if (flashRenderer != null) StartCoroutine(FlashEmission());        
        inWindup=true;
        UIManager.Instance.WarningIndicator(transform.position);

        // Call custom attack implementation
        IUniqueAttack uniqueAttack = GetComponent<IUniqueAttack>();
        if (uniqueAttack != null)
        {
            SetVisualState(windupMaterial, windupChild);
            inWindup=false;
            yield return StartCoroutine(uniqueAttack.ExecuteAttack(attack, this));
            
        }

        isAttacking = false;
        yield return new WaitForSeconds(cooldown);
        if (strafer != null) strafer.ResetStrafeDirection();
        canAttack = true;
        attackCoroutine = null;
    }

    public void OnLungeHit()
    {
        lungeHitPlayer = true;
    }

    public IEnumerator LungingAttack(EnemyAttack attack)
    {
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        isAttacking = true;
        canAttack = false;
        float elapsed = 0f;
        lungeHitPlayer = false;

        if (moveAnimCoroutine != null)
        {
            StopCoroutine(moveAnimCoroutine);
            moveAnimCoroutine = null;
        }

        // Windup
        SetVisualState(windupMaterial, windupChild);
        inWindup=true;
        UIManager.Instance.WarningIndicator(transform.position);
        if (windupClip != null && audioSource != null) audioSource.PlayOneShot(windupClip, windupClipVolume);
        StartCoroutine(FlashEmission());
        yield return new WaitForSeconds(delay);

        // Lunge
        SetVisualState(attackMaterial, attackChild);
        if (attackClip != null && audioSource != null) audioSource.PlayOneShot(attackClip, attackClipVolume);


        if (hitboxScript != null)
        {
            hitboxScript.damage = damage;
            hitboxScript.hasHit = false;
            hitboxCollider.enabled = true;
        }

        // Scale lunge based on distance to player
        float actualLungeDistance = lungeDistance;
        float actualJumpHeight = jumpHeight;
        if (playerDistance < range - 1)
        {
            actualLungeDistance = lungeDistance * .6f;
            actualJumpHeight = jumpHeight * .7f;
        }

        // Physics setup for jumping lunge
        float distanceTraveled = 0f;
        Rigidbody rb = null;
        if (jumpHeight > 0f && attackChild != null)
        {
            rb = attackChild.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.up * actualJumpHeight;
            }
        }

        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0;

        // Lunge forward until distance traveled or hit player
        while (distanceTraveled < actualLungeDistance && elapsed < duration && player != null)
        {
            float step = (actualLungeDistance / duration) * Time.deltaTime;
            transform.position += direction * step;
            distanceTraveled += step;
            transform.position += knockbackVelocity * Time.deltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);

            // Ground check for jump
            if (actualJumpHeight > 0f && rb != null && rb.linearVelocity.y <= 0 &&
                Physics.Raycast(rb.transform.position, Vector3.down, 0.05f, 1 << 7))
            {
                break;
            }

            // Bounce back on hit
            if (lungeHitPlayer)
            {
                float bounceElapsed = 0f;
                float bounceDuration = 0.4f;
                float bounceDistance = 0f;

                if (actualJumpHeight > 0f && attackChild != null)
                {
                    rb = attackChild.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = Vector3.up * (actualJumpHeight / 2);
                    }
                }

                while (bounceDistance < bounceBack && bounceElapsed < bounceDuration)
                {
                    float bounceStep = (bounceBack / bounceDuration) * Time.deltaTime;
                    transform.position += -direction * bounceStep;
                    bounceDistance += bounceStep;
                    bounceElapsed += Time.deltaTime;
                    yield return null;
                }
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (hitboxCollider != null) hitboxCollider.enabled = false;
        if (attackHitClip != null && audioSource != null && hitboxScript != null && hitboxScript.hasHit)
            audioSource.PlayOneShot(attackHitClip, attackHitClipVolume);

        SetVisualState(idleMaterial, idleChild);

        // Reset physics
        if (actualJumpHeight > 0f && attackChild != null)
        {
            rb = attackChild.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            attackChild.transform.localPosition = Vector3.zero;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.Warp(transform.position);
            navMeshAgent.enabled = true;
        }

        isAttacking = false;
        yield return new WaitForSeconds(cooldown);
        if (strafer != null) strafer.ResetStrafeDirection();
        canAttack = true;
        attackCoroutine = null;
    }
#endregion

    IEnumerator FlashEmission()
    {
        float elapsed = 0f;
        float baseIntensity = originalEmissionColor.maxColorComponent;
        Color normalizedColor = originalEmissionColor / baseIntensity;

        while (elapsed < (delay/2))
        {
            float t = elapsed / (delay/2);
            float intensity = Mathf.SmoothStep(baseIntensity, flashIntensity, t);
            flashRenderer.material.SetColor("_EmissionColor", normalizedColor * intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < (delay/2))
        {
            float t = elapsed / (delay/2);
            float intensity = Mathf.SmoothStep(flashIntensity, baseIntensity, t);
            flashRenderer.material.SetColor("_EmissionColor", normalizedColor * intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }
        flashRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
    }
    IEnumerator Die()
    {
        inWindup=false;
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        dying = true;
        isDead = true;

        if (dieClip != null && audioSource != null) audioSource.PlayOneShot(dieClip, dieClipVolume);
        
        if (animator != null) animator.SetTrigger("Die");

        RoomStarter room = GetComponentInParent<RoomStarter>();
        if (room != null) room.CheckForRemainingEnemies();

        yield return new WaitForSeconds(.5f);
        if (enemyDrop != null) Instantiate(enemyDrop, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        inWindup=false;
        // Clean up material instances to prevent memory leak (only in play mode)
        if (!Application.isPlaying) return;

        if (flashRenderer != null && flashRenderer.material != null)
        {
            // Only destroy if it's a runtime instance (name ends with "(Instance)")
            if (flashRenderer.material.name.Contains("(Instance)"))
                Destroy(flashRenderer.material);
        }

        if (idleMaterial != null && idleMaterial.name.Contains("(Instance)"))
            Destroy(idleMaterial);
        if (moveMaterial != null && moveMaterial.name.Contains("(Instance)"))
            Destroy(moveMaterial);
        if (windupMaterial != null && windupMaterial.name.Contains("(Instance)"))
            Destroy(windupMaterial);
        if (attackMaterial != null && attackMaterial.name.Contains("(Instance)"))
            Destroy(attackMaterial);
    }

    public void GetHit(int damage, float Knockback, GameObject projectile)
    {
        if (Knockback > 0 && projectile !=null) // Knockback enemy
        {
            Vector3 direction = (transform.position - projectile.transform.position).normalized;
            direction.y = 0;
            knockbackVelocity = direction * Knockback;
        }
        if (Knockback > 0 && projectile==null) // Knockback enemy
        {
            Vector3 direction = (transform.position - player.transform.position).normalized;
            direction.y = 0;
            knockbackVelocity = direction * Knockback;
        }
        currentHP -= damage;
        
        if (damagedClip != null && audioSource != null && !dying) audioSource.PlayOneShot(damagedClip, damagedClipVolume);
        
        if (damageFeedback != null) damageFeedback.PlayFeedbacks();
        StartCoroutine(DamageFlash());
    }

    IEnumerator DamageFlash()
    {
        Color idleOriginal;
        Color moveOriginal;
        Color windupOriginal;
        Color attackOriginal;

        if (!darkMode)
        {
        idleOriginal = idleMaterial != null ? idleMaterial.color : Color.white;
        moveOriginal = moveMaterial != null ? moveMaterial.color : Color.white;
        windupOriginal = windupMaterial != null ? windupMaterial.color : Color.white;
        attackOriginal = attackMaterial != null ? attackMaterial.color : Color.white;
        }
        else
        {
        idleOriginal = idleMaterial != null ? idleMaterial.color : new Color(0.5f, 0.5f, 0.5f, 1.000f);
        moveOriginal = moveMaterial != null ? moveMaterial.color : new Color(0.5f, 0.5f, 0.5f, 1.000f);
        windupOriginal = windupMaterial != null ? windupMaterial.color :  new Color(0.5f, 0.5f, 0.5f, 1.000f);
        attackOriginal = attackMaterial != null ? attackMaterial.color : new Color(0.5f, 0.5f, 0.5f, 1.000f);
        }

        
        if (idleMaterial != null) idleMaterial.color = new Color(.5f, 0.25f, 0.25f, 1.000f);
        if (moveMaterial != null) moveMaterial.color =  new Color(.5f, 0.25f, 0.25f, 1.000f);
        if (windupMaterial != null) windupMaterial.color = new Color(.5f, 0.25f, 0.25f, 1.000f);
        if (attackMaterial != null) attackMaterial.color =  new Color(.5f, 0.25f, 0.25f, 1.000f);


        yield return new WaitForSeconds(0.15f);

        // Restore original colors
        if (idleMaterial != null) idleMaterial.color = idleOriginal;
        if (moveMaterial != null) moveMaterial.color = moveOriginal;
        if (windupMaterial != null) windupMaterial.color = windupOriginal;
        if (attackMaterial != null) attackMaterial.color = attackOriginal;
    }

}
