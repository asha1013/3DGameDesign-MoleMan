using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    GameObject player;
    Animator animator;
    Inventory inventory;
    public Item key;
    [SerializeField] AudioClip openClip;
    [UnityEngine.Range(0f, 1f)] public float openClipVolume = 1f;
    public bool doorOpen = false;
    public bool doorOpenable = false;
    AudioSource audioSource;
    
    void Start()
    {
        inventory = Inventory.Instance;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

   
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (key == null)
            {
                Debug.LogError("LockedDoor: key not assigned");
                return;
            }

            if (doorOpenable && inventory.dungeonInventory.ContainsKey(key) && !doorOpen)
            {
                if (inventory.dungeonInventory[key] > 0)
                {
                    if (openClip!=null && audioSource !=null) audioSource.PlayOneShot(openClip, openClipVolume);
                    doorOpen=true;
                    animator.SetTrigger("Open");
                    inventory.dungeonInventory[key] -= 1;
                }
            }
        }
    }    
}
