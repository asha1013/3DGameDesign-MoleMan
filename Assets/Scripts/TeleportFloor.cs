using UnityEngine;
using UnityEngine.AI;

public class TeleportFloor : MonoBehaviour
{
    [SerializeField] float sampleDistance = 10f;
    [SerializeField] float heightOffset = 0.5f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // find nearest navmesh point
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
        {
            // teleport player to navmesh point + height offset
            Vector3 teleportPos = hit.position + Vector3.up * heightOffset;

            // try CharacterController first
            CharacterController controller = other.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                other.transform.position = teleportPos;
                controller.enabled = true;
            }
            // try Rigidbody for SUPER Character Controller
            else
            {
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    other.transform.position = teleportPos;
                    rb.isKinematic = false;
                }
                else
                {
                    other.transform.position = teleportPos;
                }
            }
        }
        else
        {
            Debug.LogWarning("No NavMesh found near teleport position");
        }
    }
}
