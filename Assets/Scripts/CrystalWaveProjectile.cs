using UnityEngine;
using NaughtyAttributes;

public class CrystalWaveProjectile : MonoBehaviour
{
    [BoxGroup("State")] [ReadOnly] public bool hasHit;

    private CrystalWaveManager manager;
    private Transform hitChild;
    private Collider projectileCollider;
    private Renderer projRenderer;
    private PlayerState playerState;

    [BoxGroup("Audio Clips")] [SerializeField] public AudioClip hitClip;
    [BoxGroup("Audio Clips")] [Range(0f, 1f)] public float hitClipVolume = 1f;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerState = player.GetComponent<PlayerState>();

        hitChild = transform.Find("Hit");
        projectileCollider = GetComponent<Collider>();
        if (GetComponent<Renderer>() != null) projRenderer = GetComponent<Renderer>();

        if (hitChild != null) hitChild.gameObject.SetActive(false);

        gameObject.AddComponent<Billboard>().turnSpeed = 30f;
    }

    public void SetManager(CrystalWaveManager waveManager)
    {
        manager = waveManager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Check for player collision
        if (other.CompareTag("Player"))
        {
            hasHit = true;
            if (playerState != null) playerState.GetHit(1, false);
            ActivateHitChild();
            return;
        }

        // Check for layer 9 (wall) collision - report to manager
        if (other.gameObject.layer == 9)
        {
            if (manager != null) manager.OnWallHit();
        }
    }

    void ActivateHitChild()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (hitClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitClip, hitClipVolume);
        }

        if (hitChild != null) hitChild.gameObject.SetActive(true);
        if (projRenderer != null) projRenderer.enabled = false;
        if (projectileCollider != null) projectileCollider.enabled = false;
    }
}
