using UnityEngine;
using System.Collections;
using NaughtyAttributes;

public class HomingCrystalProjectile : MonoBehaviour
{
    [BoxGroup("State")] [ReadOnly] public int currentHP;
    [BoxGroup("State")] [ReadOnly] public int maxHP;
    [BoxGroup("State")] [ReadOnly] public bool hasHit;
    [BoxGroup("State")] [ReadOnly] private bool hasSwitchedToCracked = false;

    private float homingSpeed;
    private float projectileSpeed;
    private Sprite spriteWhole;
    private Sprite spriteCracked;
    private GameObject player;
    private Transform hitChild;
    private MeshRenderer spriteRenderer;
    private Collider projectileCollider;
    private Renderer projRenderer;
    private PlayerState playerState;
    private AudioSource audioSource;

    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip travelLoopClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float travelLoopClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip hitClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float hitClipVolume = 1f;
    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip breakClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float breakClipVolume = 1f;

    public void Initialize(float homingSpd, int hp, float projSpd, Sprite whole, Sprite cracked)
    {
        homingSpeed = homingSpd;
        maxHP = hp;
        currentHP = hp;
        projectileSpeed = projSpd;
        spriteWhole = whole;
        spriteCracked = cracked;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerState = player.GetComponent<PlayerState>();
        hitChild = transform.Find("Hit");
        Transform spriteChild = transform.Find("Sprite");

        if (spriteChild != null)
        {
            spriteRenderer = spriteChild.GetComponent<MeshRenderer>();
        }

        projectileCollider = GetComponent<Collider>();
        if (GetComponent<Renderer>() != null) projRenderer = GetComponent<Renderer>();
        if (GetComponent<AudioSource>() != null) audioSource = GetComponent<AudioSource>();

        if (hitChild != null) hitChild.gameObject.SetActive(false);

        Destroy(gameObject, 20f); // Destroy after 20s

        gameObject.AddComponent<Billboard>().turnSpeed = 30f;

        if (travelLoopClip != null && audioSource != null)
        {
            audioSource.loop = true;
            audioSource.clip = travelLoopClip;
            audioSource.volume = travelLoopClipVolume;
            audioSource.Play();
        }

        // Boss projectile - no warning indicator
    }

    void Update()
    {
        if (player == null || hasHit) return;

        // Constantly rotate toward player
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, homingSpeed * 100f * Time.deltaTime);

        // Move forward
        transform.position += transform.forward * projectileSpeed * Time.deltaTime;

        // Check for sprite swap at 50% HP
        if (!hasSwitchedToCracked && currentHP <= maxHP / 2 && spriteCracked != null && spriteRenderer != null)
        {
            spriteRenderer.material.mainTexture = spriteCracked.texture;
            hasSwitchedToCracked = true;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            // Crystal destroyed by player
            hasHit = true;
            ActivateHitChild();
            StartCoroutine(ProjectileCleaner());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        bool isValidTarget = other.CompareTag("Player") || other.gameObject.layer == 7 || other.gameObject.layer == 8 || other.gameObject.layer == 10;
        if (!isValidTarget) return;

        if (other.CompareTag("Player"))
        {
            hasHit = true;
            playerState.GetHit(1, false);
            ActivateHitChild();
            StartCoroutine(ProjectileCleaner());
        }
        else if (!other.CompareTag("Enemy")) // Hit wall or obstacle
        {
            hasHit = true;
            ActivateHitChild();
            StartCoroutine(ProjectileCleaner());
        }
    }

    void ActivateHitChild()
    {
        if (hitClip != null && audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
            audioSource.PlayOneShot(hitClip, hitClipVolume);
        }

        if (breakClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakClip, breakClipVolume);
        }

        if (hitChild != null) hitChild.gameObject.SetActive(true);
        if (projRenderer != null) projRenderer.enabled = false;
        if (projectileCollider != null) projectileCollider.enabled = false;
    }

    IEnumerator ProjectileCleaner()
    {
        yield return new WaitForSeconds(3);
        Destroy(gameObject);
    }
}
