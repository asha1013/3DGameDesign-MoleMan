using UnityEngine;

public class MonsterDeathSFX : MonoBehaviour
{
    [Header("Random Death Sounds")]
    public AudioClip[] deathClips;      // 拖进来几条不同的死亡叫声
    [Range(0f, 1f)]
    public float volume = 1.0f;         // 音量

    [Header("Auto Play Settings")]
    public bool playOnDestroy = true;   // 勾上：Destroy 时自动播
    public bool onlyInPlayMode = true;  // 只在运行游戏时播，避免切场景/关编辑器乱叫

    private bool hasPlayed = false;     // 确保只播一次

    public void PlayDeathSound()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        if (deathClips == null || deathClips.Length == 0) return;

        int index = Random.Range(0, deathClips.Length);
        AudioClip clip = deathClips[index];
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }

    private void OnDestroy()
    {
        if (!playOnDestroy) return;
        if (onlyInPlayMode && !Application.isPlaying) return;

        PlayDeathSound();
    }
}
