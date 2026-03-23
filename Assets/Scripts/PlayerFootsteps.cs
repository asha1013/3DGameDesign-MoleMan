using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    public AudioSource playerAudio;
    public LayerMask groundLayer;
    public AudioClip[] footstepSounds;
    [Range(0f, 1f)] public float volume = 0.8f;
    public float pitchVariation = 0.05f;

    public bool inTunnel;
    public float groundDistance = 1f;
    public float sphereRadius = .3f;

    void Update()
    {
        // tunnel detection via spherecast
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out hit, groundDistance, groundLayer))
        {
            if (hit.collider != null && hit.collider.CompareTag("Tunnel"))
            {
                inTunnel = true;
            }
            
            else if (hit.collider != null)
            {
                inTunnel = false;
            }
        }
    }

    // called by SUPER Character Controller when footstep should trigger
    public void OnFootstep()
    {
        if (playerAudio == null)
        {
            Debug.LogWarning("PlayerFootsteps: playerAudio not assigned");
            return;
        }

        if (footstepSounds.Length == 0)
        {
            Debug.LogWarning("PlayerFootsteps: footstepSounds array is empty");
            return;
        }

        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];
        playerAudio.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        playerAudio.PlayOneShot(clip, volume);
    }
}
