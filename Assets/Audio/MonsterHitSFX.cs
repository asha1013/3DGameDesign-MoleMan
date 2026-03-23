using UnityEngine;

public class MonsterHitSFX : MonoBehaviour
{
    [Header("Random Hit Sounds")]
    public AudioClip[] hitClips;       // 拖入多条受击音效
    [Range(0f, 1f)]
    public float volume = 1.0f;        // 音量
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.05f; // 音调随机波动（让多次命中不太机械）

    [Header("Cooldown")]
    public float minInterval = 0.05f;  // 两次受击音之间的最小间隔（防止一帧连响）
    private float lastPlayTime = -999f;

    /// <summary>
    /// Call this when the monster takes damage.
    /// </summary>
    public void PlayHitSound()
    {
        if (hitClips == null || hitClips.Length == 0) return;

        // 冷却，避免一瞬间连续被多次命中发出一堆声
        if (Time.time - lastPlayTime < minInterval) return;
        lastPlayTime = Time.time;

        int index = Random.Range(0, hitClips.Length);
        AudioClip clip = hitClips[index];
        if (clip == null) return;

        // 随机一个 pitch
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        // 用一个临时 AudioSource 来做随机音高（比 PlayClipAtPoint 更灵活）
        GameObject temp = new GameObject("MonsterHitSFX_Temp");
        temp.transform.position = transform.position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = 1f; // 3D 声音
        src.Play();

        Destroy(temp, clip.length / Mathf.Max(pitch, 0.01f));
    }
}
