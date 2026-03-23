using UnityEngine;
using NaughtyAttributes;

public enum ConnectionDirection
{
    North,  // +Z
    South,  // -Z
    East,   // +X
    West    // -X
}

public class DoorConnector : MonoBehaviour
{
    [Tooltip("Local axis pointing OUT of the room (where hallway snaps to)")]
    public Vector3 localOutwardAxis = Vector3.forward;

    public bool isConnected = false;

    public bool isHallway = false;

    [HideInInspector] public GameObject doorObject;
    [HideInInspector] public GameObject wallObject;
    [HideInInspector] public GameObject lockedObject;

    [Foldout("Testing")]
    public bool testMode = false;
    [Foldout("Testing")]
    [ShowIf("testMode")]
    public bool testIsDoor = true;
    [Foldout("Testing")]
    [ShowIf("testMode")]
    public bool testIsLocked = false;

    void Awake()
    {
        if (!isHallway)
        {
            FindChildObjects();
        }
    }

    void Start()
    {
        if (testMode)
        {
            SetConnectionState(testIsDoor, testIsLocked);
        }
    }

    void FindChildObjects()
    {
        Transform openChild = transform.Find("Open");
        if (openChild != null) doorObject = openChild.gameObject;

        Transform closedChild = transform.Find("Closed");
        if (closedChild != null) wallObject = closedChild.gameObject;

        Transform lockedChild = transform.Find("Locked");
        if (lockedChild != null) lockedObject = lockedChild.gameObject;
    }

    void OnValidate()
    {
        if (testMode && Application.isPlaying)
        {
            SetConnectionState(testIsDoor, testIsLocked);
        }
    }

    // set the active child object based on connection state
    public void SetConnectionState(bool isDoor, bool isLocked = false)
    {
        if (isHallway) return;

        // ensure child objects are found (for editor mode)
        if (doorObject == null && wallObject == null && lockedObject == null)
        {
            FindChildObjects();
        }

        DeactivateAll();

        if (isLocked && lockedObject != null)
        {
            lockedObject.SetActive(true);
        }
        else if (isDoor && doorObject != null)
        {
            doorObject.SetActive(true);
        }
        else if (!isDoor && wallObject != null)
        {
            wallObject.SetActive(true);
        }
    }

    void DeactivateAll()
    {
        if (doorObject != null) doorObject.SetActive(false);
        if (wallObject != null) wallObject.SetActive(false);
        if (lockedObject != null) lockedObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        Vector3 start = transform.position;
        Vector3 worldOutward = transform.TransformDirection(localOutwardAxis.normalized);
        Vector3 end = start + worldOutward;

        Gizmos.color = isHallway ? Color.cyan : Color.red;
        Gizmos.DrawLine(start, end);

        // draw arrow head to show outward direction
        Vector3 arrowRight = Quaternion.Euler(0, 30, 0) * -worldOutward * 0.3f;
        Vector3 arrowLeft = Quaternion.Euler(0, -30, 0) * -worldOutward * 0.3f;
        Gizmos.DrawLine(end, end + arrowRight);
        Gizmos.DrawLine(end, end + arrowLeft);
    }

    // get world direction based on outward-facing axis
    public ConnectionDirection GetWorldDirection()
    {
        Vector3 worldOutward = transform.TransformDirection(localOutwardAxis.normalized);

        // determine which cardinal direction the outward vector points
        float absX = Mathf.Abs(worldOutward.x);
        float absZ = Mathf.Abs(worldOutward.z);

        if (absX > absZ)
        {
            // pointing more along X axis
            return worldOutward.x > 0 ? ConnectionDirection.East : ConnectionDirection.West;
        }
        else
        {
            // pointing more along Z axis
            return worldOutward.z > 0 ? ConnectionDirection.North : ConnectionDirection.South;
        }
    }
}
