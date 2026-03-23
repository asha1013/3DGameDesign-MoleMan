using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MeleeAttackSFX : MonoBehaviour
{
    [Header("Attack Sounds")]
    public AudioClip[] attackClips;        // 拖入多条近战挥砍音效
    [Range(0f, 1f)]
    public float volume = 1.0f;            // 音量
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.05f;   // 音调随机波动（让每次攻击听起来不一样）

    [Header("Input")]
    public bool useMouseButton = true;     // 默认：用左键触发
    public int mouseButtonIndex = 0;       // 0 = 左键

    [Header("Cooldown")]
    public float minInterval = 0.1f;       // 两次攻击音之间的最小间隔（秒）

    private AudioSource source;
    private float lastPlayTime = -999f;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f; // 3D 声音，如果想 2D 可以改成 0
    }

    void Update()
    {
        if (!useMouseButton) return;

        if (Input.GetMouseButtonDown(mouseButtonIndex))
        {
            TryPlayAttackSound();
        }
    }

    public void TryPlayAttackSound()
    {
        if (attackClips == null || attackClips.Length == 0) return;

        // 冷却检查，避免一瞬间连放
        if (Time.time - lastPlayTime < minInterval) return;
        lastPlayTime = Time.time;

        int index = Random.Range(0, attackClips.Length);
        AudioClip clip = attackClips[index];
        if (clip == null) return;

        // 随机一个 pitch，让声音更自然
        source.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        source.volume = volume;
        source.PlayOneShot(clip);
    }
}

