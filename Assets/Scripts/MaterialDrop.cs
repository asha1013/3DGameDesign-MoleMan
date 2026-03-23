using UnityEngine;

public class MaterialDrop : MonoBehaviour
{
    public Item item;
    private Inventory inventory;
    private Transform playerTransform;

    void Start()
    {
        inventory = Inventory.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Play pickup sound
            var sfx = GetComponent<PickupSFX>();
            if (sfx != null)
                sfx.PlayPickupSound();

            // Add to inventory
            if (inventory != null && item != null) inventory.AddItem(item, 1);

            // Destroy pickup
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (item == null) return;

        // Move down until hitting ground
        if (item.isMaterial && !Physics.Raycast(transform.position, Vector3.down, 0.1f))
        {
            transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.down * 0.5f, Time.deltaTime * 5f);
        }

        // Move towards player if within range and a material
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 10f && item.isMaterial)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, 10 * Time.deltaTime);
        }
    }
}
