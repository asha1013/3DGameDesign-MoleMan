using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerHitSFX : MonoBehaviour
{
    [Header("Player Hit Sounds")]
    public AudioClip[] hitClips;          // put several hurt sounds here
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("Random Pitch")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.05f;  // small variation so it doesn't sound the same every time

    [Header("Cooldown")]
    public float minInterval = 0.1f;      // min time between hit sounds

    private AudioSource source;
    private float lastPlayTime = -999f;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        // player hurt is usually best as 2D so it's always clear
        source.spatialBlend = 0f; // 0 = 2D, 1 = 3D
    }

    public void PlayHitSound()
    {
        if (hitClips == null || hitClips.Length == 0) return;

        // prevent spam if multiple hits in one frame
        if (Time.time - lastPlayTime < minInterval) return;
        lastPlayTime = Time.time;

        int index = Random.Range(0, hitClips.Length);
        AudioClip clip = hitClips[index];
        if (clip == null) return;

        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.pitch = pitch;
        source.volume = volume;

        source.PlayOneShot(clip);
    }
}
