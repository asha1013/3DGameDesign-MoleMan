using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FloorChecker : MonoBehaviour
{
    private RoomStarter roomStarter;
    private bool hasTriggered = false;

    void Start()
    {
        roomStarter = transform.parent.parent.GetComponent<RoomStarter>();
        if (roomStarter == null)
        {
            Debug.LogError($"FloorChecker on {gameObject.name}: RoomStarter not found on parent's parent! Hierarchy: {transform.parent?.parent?.name}");
        }

        // Find trigger collider specifically (in case there are multiple colliders)
        Collider[] colliders = GetComponents<Collider>();
        Collider triggerCol = null;

        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCol = col;
                break;
            }
        }

        if (triggerCol == null)
        {
            Debug.LogError($"FloorChecker on {gameObject.name}: No trigger Collider found! Found {colliders.Length} colliders total.");
        }
        else
        {
            // Only check if player is inside on the very first frame (for start room)
            // Use a small delay to avoid false positives during dungeon generation
            StartCoroutine(CheckPlayerInside(triggerCol));
        }
    }

    IEnumerator CheckPlayerInside(Collider triggerCol)
    {
        // Wait for dungeon generation to finish
        yield return new WaitForSeconds(2f);

        // Check if player is already inside the trigger
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && triggerCol != null && triggerCol.bounds.Contains(player.transform.position))
        {
            Debug.Log($"FloorChecker on {gameObject.name}: Player already inside trigger after delay, manually triggering");
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider != null)
            {
                OnTriggerEnter(playerCollider);
            }
            else
            {
                Debug.LogError($"FloorChecker on {gameObject.name}: Player has no Collider component");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        Debug.Log($"FloorChecker on {gameObject.name}: OnTriggerEnter with {other.gameObject.name} (tag: {other.tag})");

        if (other.CompareTag("Player") && roomStarter != null)
        {
            hasTriggered = true;
            Debug.Log($"FloorChecker on {gameObject.name}: Player entered, activating room");
            roomStarter.ActivateRoom();
            roomStarter.OnPlayerEnterFloor();

            // update minimap
            MinimapManager minimap = Object.FindFirstObjectByType<MinimapManager>();
            if (minimap != null)
            {
                minimap.SetCurrentRoom(roomStarter.gridPosition);
            }
        }
    }
}
