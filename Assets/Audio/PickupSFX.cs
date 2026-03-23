using UnityEngine;

public class PickupSFX : MonoBehaviour
{
    [Header("Pickup Sounds")]
    public AudioClip[] pickupClips;     // 拾取音效
    [Range(0f, 1f)]
    public float volume = 1f;           // 音量

    [Range(0f, 0.5f)]
    public float pitchVariation = 0.05f; // 音调随机

    /// <summary>
    /// Call this when the item is picked up.
    /// </summary>
    public void PlayPickupSound()
    {
        if (pickupClips == null || pickupClips.Length == 0) return;

        int index = Random.Range(0, pickupClips.Length);
        AudioClip clip = pickupClips[index];
        if (clip == null) return;

        // 随机音调
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        // 用临时 AudioSource 播放
        GameObject temp = new GameObject("PickupSFX_Temp");
        temp.transform.position = transform.position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 1f;  // 3D 声音，听起来有空间感
        src.Play();

        Destroy(temp, clip.length / Mathf.Max(pitch, 0.01f));
    }
}
