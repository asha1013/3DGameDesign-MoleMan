using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class RoomStarter : MonoBehaviour
{
    public AudioClip doorOpenClip;
    [UnityEngine.Range(0f, 1f)] public float doorOpenClipVolume = 1f;
    public AudioClip doorCloseClip;
    [UnityEngine.Range(0f, 1f)] public float doorCloseClipVolume = 1f;

    public bool hasActivated = false;
    private Dictionary<GameObject, bool> doorStates = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> wallStates = new Dictionary<GameObject, bool>();
    private Dictionary<Transform, AudioSource> doorAudioSources = new Dictionary<Transform, AudioSource>();
    private PlayerFootsteps playerFootsteps;
    public bool roomDiscovered = false;
    [HideInInspector] public Vector2Int gridPosition;
   public void RoomStart()
    {
        Debug.Log($"{gameObject.name}: RoomStart called, adding FloorChecker");
        // Search recursively for Floor and add FloorChecker
        Transform floorTransform = FindChildRecursive(transform, "Floor");
        if (floorTransform != null)
        {
            FloorChecker checker = floorTransform.GetComponent<FloorChecker>();
            if (checker == null)
            {
                checker = floorTransform.gameObject.AddComponent<FloorChecker>();
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: Floor not found");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerFootsteps = player.GetComponent<PlayerFootsteps>();
        }

        StartCoroutine(EnableRoomObjects());
        StartCoroutine(FindEnemiesDelayed());
    }

    IEnumerator EnableRoomObjects()
    {
        yield return new WaitForSeconds(0.5f);

        // Enable ceiling
        Transform ceilingTransform = FindChildRecursive(transform, "Ceiling");
        if (ceilingTransform != null)
        {
            ceilingTransform.gameObject.SetActive(true);
        }

        // Enable all enemy GameObjects
        Enemy[] enemies = GetComponentsInChildren<Enemy>(true);
        Debug.Log($"{gameObject.name}: EnableRoomObjects - Found {enemies.Length} enemies, activating GameObjects");
        foreach (Enemy enemy in enemies)
        {
            Debug.Log($"{gameObject.name}: Activating enemy GameObject {enemy.gameObject.name} (was {enemy.gameObject.activeSelf})");
            enemy.gameObject.SetActive(true);
        }
    }

    IEnumerator FindEnemiesDelayed()
    {
        yield return new WaitForSeconds(1f);

        // Don't disable enemies if room is already activated
        if (hasActivated) yield break;

        // Find all NavMeshAgents in this room and disable Enemy scripts
        UnityEngine.AI.NavMeshAgent[] agents = GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true);
        foreach (var agent in agents)
        {
            Enemy enemy = agent.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.enabled = false;
            }
        }
    }

    public void ActivateRoom()
    {
        if (hasActivated) return;

        StartCoroutine(CheckTunnelAndActivate());
    }

    IEnumerator CheckTunnelAndActivate()
    {
        yield return new WaitForSeconds(1f);

        if (playerFootsteps != null && playerFootsteps.inTunnel)
        {
            Debug.Log($"{gameObject.name}: Player in tunnel, aborting activation");
            yield break;
        }

        hasActivated = true;
        Debug.Log($"{gameObject.name}: Room activated");

        // Enable all Enemy scripts in this room
        Enemy[] enemies = GetComponentsInChildren<Enemy>(true);
        Debug.Log($"{gameObject.name}: Found {enemies.Length} enemies to activate");

        foreach (Enemy enemy in enemies)
        {
            enemy.enabled = true;
        }

        // Only close doors if there are enemies in the room
        if (enemies.Length > 0)
        {
            Debug.Log($"{gameObject.name}: Closing doors because room has {enemies.Length} enemies");
            CloseDoors();
        }
        else
        {
            Debug.Log($"{gameObject.name}: No enemies, skipping door close");
        }
    }

    void CloseDoors()
    {
        DoorConnector[] doorConnectors = GetComponentsInChildren<DoorConnector>();
        Debug.Log($"{gameObject.name}: CloseDoors - Found {doorConnectors.Length} DoorConnectors");

        foreach (DoorConnector connector in doorConnectors)
        {
            Debug.Log($"{gameObject.name}: Connector {connector.gameObject.name} - isConnected={connector.isConnected}, isHallway={connector.isHallway}");

            if (!connector.isConnected || connector.isHallway) continue;

            // Check if this is a locked door
            if (connector.lockedObject != null && connector.lockedObject.activeSelf)
            {
                Debug.Log($"{gameObject.name}: Connector {connector.gameObject.name} is locked door");
                Transform lockedDoorTransform = connector.lockedObject.transform.Find("LockedDoor");
                if (lockedDoorTransform != null)
                {
                    LockedDoor lockedDoor = lockedDoorTransform.GetComponent<LockedDoor>();
                    if (lockedDoor != null)
                    {
                        lockedDoor.doorOpenable = false;
                    }
                }
                continue;
            }

            Debug.Log($"{gameObject.name}: Closing connector {connector.gameObject.name}");

            // Get or add AudioSource to door
            AudioSource doorAudio = GetOrAddAudioSource(connector.transform);
            if (doorCloseClip != null && doorAudio != null)
            {
                doorAudio.PlayOneShot(doorCloseClip, doorCloseClipVolume);
            }

            // Store current state before closing
            if (connector.doorObject != null)
            {
                doorStates[connector.doorObject] = connector.doorObject.activeSelf;
                Debug.Log($"{gameObject.name}: Storing door state - was {connector.doorObject.activeSelf}");
            }

            if (connector.wallObject != null)
            {
                wallStates[connector.wallObject] = connector.wallObject.activeSelf;
                Debug.Log($"{gameObject.name}: Storing wall state - was {connector.wallObject.activeSelf}");
            }

            // Close the door (activate Closed, deactivate Open)
            if (connector.doorObject != null) connector.doorObject.SetActive(false);
            if (connector.wallObject != null) connector.wallObject.SetActive(true);
        }
    }

    public void CheckForRemainingEnemies()
    {
        if (!hasActivated) return;

        Enemy[] enemies = GetComponentsInChildren<Enemy>(true);
        Debug.Log($"{gameObject.name}: CheckForRemainingEnemies - Found {enemies.Length} enemies");

        foreach (Enemy enemy in enemies)
        {
            Debug.Log($"{gameObject.name}: Enemy {enemy.gameObject.name} isDead={enemy.isDead}");
            if (!enemy.isDead) return;
        }

        Debug.Log($"{gameObject.name}: All enemies dead, opening doors");
        OpenDoors();
    }

    public void OnPlayerEnterFloor()
    {
        CheckForRemainingEnemies();
    }

    void OpenDoors()
    {
        DoorConnector[] doorConnectors = GetComponentsInChildren<DoorConnector>();
        foreach (DoorConnector connector in doorConnectors)
        {
            if (!connector.isConnected || connector.isHallway) continue;

            // Check if this is a locked door
            if (connector.lockedObject != null && connector.lockedObject.activeSelf)
            {
                Transform lockedDoorTransform = connector.lockedObject.transform.Find("LockedDoor");
                if (lockedDoorTransform != null)
                {
                    LockedDoor lockedDoor = lockedDoorTransform.GetComponent<LockedDoor>();
                    if (lockedDoor != null)
                    {
                        lockedDoor.doorOpenable = true;
                    }
                }
                continue;
            }

            AudioSource doorAudio = GetOrAddAudioSource(connector.transform);
            if (doorOpenClip != null && doorAudio != null)
            {
                doorAudio.PlayOneShot(doorOpenClip, doorOpenClipVolume);
            }
        }

        // Restore previous door/wall states
        foreach (var doorEntry in doorStates)
        {
            doorEntry.Key.SetActive(doorEntry.Value);
        }

        foreach (var wallEntry in wallStates)
        {
            wallEntry.Key.SetActive(wallEntry.Value);
        }
    }

    AudioSource GetOrAddAudioSource(Transform doorSnap)
    {
        if (!doorAudioSources.ContainsKey(doorSnap))
        {
            AudioSource source = doorSnap.GetComponent<AudioSource>();
            if (source == null) source = doorSnap.gameObject.AddComponent<AudioSource>();
            doorAudioSources[doorSnap] = source;
        }
        return doorAudioSources[doorSnap];
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
