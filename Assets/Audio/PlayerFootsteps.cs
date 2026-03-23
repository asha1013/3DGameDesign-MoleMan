using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StepByStepFootsteps : MonoBehaviour
{
    public AudioClip[] footstepSounds;     // 拖入多个脚步音效
    public float stepInterval = 0.5f;      // 每步时间间隔（走路）
    public float runInterval = 0.3f;       // 跑步时每步间隔
    public float speedThreshold = 0.2f;    // 超过此速度才算在走动
    public float volume = 0.8f;            // 音量
    public float pitchVariation = 0.05f;   // 音调随机范围（0.05=±5%）
    public bool grounded = true;           // 跳跃时可以从你的Role脚本更新

    private AudioSource source;
    private Vector3 lastPos;
    private float stepTimer;

    void Start()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;  // 3D 声音
        source.volume = volume;

        lastPos = transform.position;
    }

    void Update()
    {
        // 计算水平移动速度
        Vector3 delta = transform.position - lastPos;
        delta.y = 0;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = transform.position;

        bool isMoving = grounded && speed > speedThreshold;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        if (isMoving)
        {
            stepTimer += Time.deltaTime;
            float interval = isRunning ? runInterval : stepInterval;

            if (stepTimer >= interval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;

        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];
        source.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        source.PlayOneShot(clip, volume);
    }
}
