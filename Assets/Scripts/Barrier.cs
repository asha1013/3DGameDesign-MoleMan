using UnityEngine;

public class Barrier : MonoBehaviour
{
    [SerializeField] private GameObject hitRipple;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
        }
    }

    public void OnProjectileHit(Vector3 hitPosition)
    {
        if (hitRipple != null)
        {
            GameObject ripple = Instantiate(hitRipple, hitPosition, transform.rotation);
            Destroy(ripple, 0.5f);
        }
    }
}
