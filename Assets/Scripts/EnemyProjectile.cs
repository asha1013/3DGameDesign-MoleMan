using System.Collections;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [BoxGroup("Projectile Stats")] public float projectileSpeed;
    [BoxGroup("Projectile Stats")] public bool travelArc;
    [BoxGroup("Projectile Stats")] public float arcHeight;
    [BoxGroup("Projectile Stats")] public int damage;
    [BoxGroup("Projectile Stats")] public bool isAOE;
    [BoxGroup("Projectile Stats")] public float splashRadius;
    [BoxGroup("Projectile Stats")] public float dropOff;

    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip travelLoopClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float travelLoopClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip hitClip;
    [BoxGroup("Audio Clips")] [UnityEngine.Range(0f, 1f)] public float hitClipVolume = 1f;

    [BoxGroup("Visuals")] public Material spriteDown;
    [BoxGroup("Visuals")] [SerializeField] Material spriteUp;

    [BoxGroup("State")] [ReadOnly] public float targetLow = .5f;
    [BoxGroup("State")] [ReadOnly] public bool hasHit;

    private ParticleSystem travelParticleSystem;
    private ParticleSystem hitParticleSystem;
    private Vector3 direction;
    private Vector3 targetPos;
    private Rigidbody rb;
    private AudioSource projectileAS;
    private Collider projectileCollider;
    private Collider aoeCollider;
    private Renderer projRenderer;
    private Transform hitChild;
    private Transform spriteChild;
    private MeshRenderer spriteRenderer;
    private bool hasSwitchedToDown = false;
    private PlayerState playerState;

    void Start()
    {
        // Assigns
        hitChild = transform.Find("Hit");
        spriteChild = transform.Find("Sprite");
        if (spriteChild != null)
        {
            spriteRenderer = spriteChild.GetComponent<MeshRenderer>();
            if (spriteRenderer != null) spriteUp = spriteRenderer.material;
        }
        if (GetComponent<ParticleSystem>() != null) travelParticleSystem = GetComponent<ParticleSystem>();
        if (hitChild != null) hitParticleSystem = hitChild.GetComponent<ParticleSystem>();
        if (GetComponent<Rigidbody>() != null) rb = GetComponent<Rigidbody>();
        if (GetComponent<AudioSource>() != null) projectileAS = GetComponent<AudioSource>();
        projectileCollider = GetComponent<Collider>();
        if (hitChild != null) aoeCollider = hitChild.GetComponent<Collider>();
        if (GetComponent<Renderer>() != null) projRenderer = GetComponent<Renderer>();

        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera == null)
        {
            Debug.LogError("EnemyProjectile: MainCamera not found");
            Destroy(gameObject);
            return;
        }
        targetPos = mainCamera.transform.position - new Vector3(0, targetLow, 0);
        direction = (targetPos - transform.position).normalized;

        if (hitChild != null) hitChild.gameObject.SetActive(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("EnemyProjectile: Player not found");
            Destroy(gameObject);
            return;
        }
        playerState = player.GetComponent<PlayerState>();

        Destroy(gameObject, 10f); // destroy after 10s
        gameObject.AddComponent<Billboard>().turnSpeed = 30f;
        if (travelLoopClip != null && projectileAS != null)
        {
            projectileAS.loop = true;
            projectileAS.clip = travelLoopClip;
            projectileAS.volume=travelLoopClipVolume;
            projectileAS.Play();
        }

      if (travelArc)
        {
            BallisticTrajectory(); // Math :(
        }
        else
        {
            rb.linearVelocity = direction*projectileSpeed;
        }        
        UIManager.Instance.StartCoroutine(UIManager.Instance.ProjectileWarningIndicator(gameObject));

    }
    void FixedUpdate()
    {
        if (travelArc)
        {
            if(rb!=null)rb.AddForce(Vector3.down * dropOff, ForceMode.Acceleration);

            // Switch to down sprite when falling
            if (!hasSwitchedToDown && spriteDown != null && spriteRenderer != null && rb != null && rb.linearVelocity.y < -.1f)
            {
                spriteRenderer.material = spriteDown;
                hasSwitchedToDown = true;
            }
        }
        else if (dropOff > 0)
        {
            if(rb!=null)rb.linearVelocity += Vector3.down * dropOff * Time.fixedDeltaTime; // Projectile dropoff based on stat
        }
    }

    void OnTriggerEnter(Collider other)
    {
        bool isValidTarget = other.CompareTag("Player") || other.gameObject.layer == 7 || other.gameObject.layer == 8 || other.gameObject.layer == 9 || other.gameObject.layer == 10;
        if (!isValidTarget) return;

        Debug.Log($"Projectile hit: {other.gameObject.name}, Layer: {other.gameObject.layer}, Tag: {other.gameObject.tag}, ProjectileY: {transform.position.y}, ColliderBoundsY: {other.bounds.max.y}");

        if (other.CompareTag("Player") && !hasHit)
        {
            hasHit = true;
            if (hitClip != null)
            {
                projectileAS.loop = false;
                projectileAS.Stop();
                projectileAS.PlayOneShot(hitClip, hitClipVolume);
            }
            if (isAOE)
            {
                hitChild.gameObject.SetActive(true); // enables to activate any play on start particle effect                           
            }
            playerState.GetHit(damage, false);
            if (projRenderer != null) projRenderer.enabled = false;
            projectileCollider.enabled = false;
            StartCoroutine(ProjectileCleaner());
        }
        else if ((!other.gameObject.CompareTag("Enemy")|| other.gameObject.layer == 9 ) && !hasHit ) // hits something that isnt another enemy
        {
            hasHit = true;
            if (hitClip != null)
            {
                projectileAS.Stop();
                projectileAS.PlayOneShot(hitClip, hitClipVolume);
            }
           if (isAOE)
            {
                hitChild.gameObject.SetActive(true); // enables to activate any play on start particle effect
                Collider[] hits = Physics.OverlapSphere(transform.position, splashRadius); //check sphere around object for player
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        playerState.GetHit(damage, false);
                    }
                }           
            }
            if (projRenderer != null) projRenderer.enabled = false;
            projectileCollider.enabled = false;
            Destroy(rb);            
            if (spriteChild != null) spriteChild.gameObject.SetActive(false);
            StartCoroutine(ProjectileCleaner());
        }
    }
    IEnumerator ProjectileCleaner() // deletes projectile after its done doing whatever it needs to do
    {
        yield return new WaitForSeconds(3);
        Destroy (gameObject);
    }
    void BallisticTrajectory() // I didn't do the math >:)
    {
        {
            Vector3 startPos = transform.position;
            float horizontalDist = Vector3.Distance(
                new Vector3(startPos.x, 0, startPos.z),
                new Vector3(targetPos.x, 0, targetPos.z)
            );
            float heightDiff = targetPos.y - startPos.y;
            float gravity = 20f; // Use fixed gravity value
            
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
}
