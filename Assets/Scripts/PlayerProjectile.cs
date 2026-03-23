using System.Collections;
using NaughtyAttributes;
using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    // Movement
    public float projectileSpeed;
    public bool travelArc;
    public float arcHeight;
    public float dropOff;

    // Damage
    public int damage;
    public float knockback;
    public bool isAOE;
    public float splashRadius;
    public int attackPenetration = 0;

    // Audio
    public AudioClip shootClip;
    [UnityEngine.Range(0f, 1f)] public float shootClipVolume = 1f;
    public AudioClip travelLoopClip;
    [UnityEngine.Range(0f, 1f)] public float travelLoopClipVolume = 1f;
    public AudioClip hitClip;
    [UnityEngine.Range(0f, 1f)] public float hitClipVolume = 1f;
    [HideInInspector] public float volumeModifier = 1f;

    // State tracking
    bool hasHit;
    int hits;

    // Visuals
    public Transform projectileObject;

    // Cached components
    ParticleSystem travelParticleSystem;
    ParticleSystem hitParticleSystem;
    Vector3 direction;
    Vector3 targetPos;
    Rigidbody rb;
    AudioSource projectileAS;
    Collider projectileCollider;
    Collider aoeCollider;
    Renderer projRenderer;
    Light projectileLight;
    Transform hitChild;
    Transform spriteChild;
    PlayerAttack playerAttack;
    bool initialized = false;

    public void Initialize(PlayerState state)
    {
        if (state == null)
        {
            Debug.LogWarning("PlayerState not assigned");
            return;
        }

        // apply all weapon stats from PlayerState
        damage = state.damage;
        projectileSpeed = state.projectileSpeed;
        dropOff = (1f - state.projectileRange) * 20f;
        travelArc = state.travelArc;
        arcHeight = state.arcHeight;
        attackPenetration = state.penetration;
        isAOE = state.isAOE;
        splashRadius = state.splashRadius;
        knockback = state.knockback;

        initialized = true;
    }

    void Start()
    {
        if (!initialized)
        {
            Debug.LogWarning("PlayerProjectile not initialized - stats may be incorrect");
        }

        // Cache components
        hitChild = transform.Find("Hit");
        spriteChild = transform.Find("Sprite");
        if (GetComponent<ParticleSystem>() != null) travelParticleSystem = GetComponent<ParticleSystem>();
        if (hitChild != null) hitParticleSystem = hitChild.GetComponent<ParticleSystem>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("PlayerProjectile: Rigidbody not found");
        if (GetComponent<Light>() != null) projectileLight = GetComponent<Light>();
        if (GetComponent<AudioSource>() != null) projectileAS = GetComponent<AudioSource>();
        projectileCollider = GetComponent<Collider>();
        if (hitChild != null) aoeCollider = hitChild.GetComponent<Collider>();
        if (GetComponent<Renderer>() != null) projRenderer = GetComponent<Renderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerAttack = player.GetComponent<PlayerAttack>();

        // Set target and direction
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null) direction = camera.transform.forward;
        else direction = Vector3.forward;
        if (hitChild != null) hitChild.gameObject.SetActive(false);

        // Ensure projectile visual is active
        if (projectileObject != null)
        {
            projectileObject.gameObject.SetActive(true);
        }

        if (shootClip != null && projectileAS != null) projectileAS.PlayOneShot(shootClip, shootClipVolume * volumeModifier);

        // Setup
        Destroy(gameObject, 10f); // destroy after 10s
        if (spriteChild != null)
        {
            Billboard billboard = spriteChild.GetComponent<Billboard>();
            if (billboard == null) billboard = spriteChild.gameObject.AddComponent<Billboard>();
            billboard.turnSpeed = 30f;
        }

        // Travel audio
        if (travelLoopClip != null && projectileAS != null)
        {
            projectileAS.loop = true;
            projectileAS.clip = travelLoopClip;
            projectileAS.volume = travelLoopClipVolume;
            projectileAS.Play();
        }

        // Launch projectile
        if (travelArc)
        {
            BallisticTrajectory();
        }
        else
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Apply gravity/dropoff
        if (travelArc)
        {
            rb.AddForce(Vector3.down * dropOff, ForceMode.Acceleration);
        }
        else if (dropOff > 0)
        {
            rb.linearVelocity += Vector3.down * dropOff * Time.fixedDeltaTime;
        }

        // Rotate projectile visual to face velocity direction
        if (!hasHit && rb.linearVelocity.sqrMagnitude > 0.01f && projectileObject != null)
        {
            projectileObject.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check for barrier
        Barrier barrier = other.GetComponent<Barrier>();
        if (barrier != null)
        {
            barrier.OnProjectileHit(transform.position);
            if (projectileObject != null) projectileObject.gameObject.SetActive(false);
            if (hitChild != null) hitChild.gameObject.SetActive(true);
            projectileCollider.enabled = false;
            Destroy(rb);
            StartCoroutine(ProjectileCleaner());
            return;
        }

        bool isValidTarget = other.CompareTag("Enemy") || other.gameObject.layer == 7 || other.gameObject.layer == 8 || other.gameObject.layer == 10;
        if (!isValidTarget || hasHit) return;

        hasHit = true;

        // hit audio
        if (hitClip != null)
        {
            projectileAS.loop = false;
            projectileAS.Stop();
            projectileAS.PlayOneShot(hitClip, hitClipVolume);
        }

        // AOE
        if (isAOE)
        {
            if (hitChild != null)
            {
                hitChild.gameObject.SetActive(true);
                Collider[] hits = Physics.OverlapSphere(transform.position, splashRadius);
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        Enemy enemy = hit.GetComponentInParent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.GetHit(damage, knockback, gameObject);
                            if (playerAttack != null) playerAttack.HitConfirm();
                        }
                    }
                }
            }
            else Debug.Log("No hitchild detected on AOE projectile");
        }
        // Direct hit
        else if (other.CompareTag("Enemy"))
        {
            hits++;
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.GetHit(damage, knockback, gameObject);
                if (playerAttack != null) playerAttack.HitConfirm();
            }

            // Check for penetration
            if (hits > attackPenetration)
            {
                if (projectileObject != null) projectileObject.gameObject.SetActive(false);
                if (hitChild != null) hitChild.gameObject.SetActive(true);
                projectileCollider.enabled = false;
                if (rb != null) Destroy(rb);
                StartCoroutine(ProjectileCleaner());
                return;
            }
            else
            {
                hasHit = false; // Allow next hit
                return;
            }
        }

        // Hide and cleanup
        Destroy(rb);
        if (projectileObject != null) projectileObject.gameObject.SetActive(false);
        if (hitChild != null) hitChild.gameObject.SetActive(true);
        projectileCollider.enabled = false;
        StartCoroutine(ProjectileCleaner());
    }

    IEnumerator ProjectileCleaner()
    {
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }

    void BallisticTrajectory()
    {
        Vector3 startPos = transform.position;
        float horizontalDist = Vector3.Distance(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(targetPos.x, 0, targetPos.z)
        );
        float gravity = 20f;

        // Calculate velocity needed to reach arcHeight above the higher point
        float peakHeight = Mathf.Max(startPos.y, targetPos.y) + arcHeight;
        float heightToReach = peakHeight - startPos.y;

        // Vertical velocity to reach peak
        float verticalVel = Mathf.Sqrt(2 * gravity * heightToReach);

        // Time to reach peak
        float timeToPeak = verticalVel / gravity;

        // Time falling from peak to target
        float fallHeight = peakHeight - targetPos.y;
        float timeFromPeak = Mathf.Sqrt(2 * fallHeight / gravity);

        float totalTime = timeToPeak + timeFromPeak;

        // Horizontal velocity to cover distance in that time
        float horizontalSpeed = horizontalDist / totalTime;

        rb.linearVelocity = new Vector3(
            direction.x * horizontalSpeed,
            verticalVel,
            direction.z * horizontalSpeed
        );

        dropOff = gravity;
    }
}
