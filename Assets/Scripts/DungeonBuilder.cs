using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonBuilder : MonoBehaviour
{
    [BoxGroup("Room Types")]
    public RoomType normalRoom;
    [BoxGroup("Room Types")]
    public RoomType startRoom;
    [BoxGroup("Room Types")]
    public RoomType keyRoom;
    [BoxGroup("Room Types")]
    public RoomType bossRoom;
    [BoxGroup("Room Types")]
    public List<RoomType> additionalRoomTypes = new List<RoomType>();

    [BoxGroup("Generation Settings")]
    public GameObject hallwayPrefab;

    [BoxGroup("Generation Settings")]
    [Range(2, 16)]
    public int pathLength = 8;

    [BoxGroup("Generation Settings")]
    [Range(3, 8)]
    public int numberOfAddRooms = 4;

    [BoxGroup("Generation Settings")]
    [Range(0, 8)]
    public int numberOfExtRooms = 2;

    [BoxGroup("Generation Settings")]
    [Range(0, 8)]
    public int numberOfExtConnections = 2;

    [BoxGroup("Runtime Settings")]
    public bool generateOnStart = true;

    [BoxGroup("Minimap")]
    public MinimapManager minimapManager;
    [BoxGroup("Minimap")]
    public bool revealAllRooms = false;

    [BoxGroup("Debug")]
    public bool showDebugGizmos = true;
    [BoxGroup("Debug")]
    public float gizmoSize = 1f;

    void Start()
    {
        if (generateOnStart && floorPlan.Count == 0)
        {
            GenerateDungeon();
        }
        else if (floorPlan.Count > 0)
        {
            // dungeon already exists (generated in editor), setup minimap for play mode
            SetupMinimap();
        }
        minimapManager=GameManager.Instance.minimapManager;
    }

    // internal floor plan data
    public class GridNode
    {
        public Vector2Int position;
        public RoomType roomType;
        public bool[] connections = new bool[4]; // N, S, E, W
        public GameObject spawnedRoom;
    }

    private Dictionary<Vector2Int, GridNode> floorPlan = new Dictionary<Vector2Int, GridNode>();
    private GridNode startNode;
    private GridNode bossNode;
    private List<GridNode> pathNodes = new List<GridNode>();
    private List<GridNode> addNodes = new List<GridNode>();
    private List<GridNode> extNodes = new List<GridNode>();

    private const int MAX_GENERATION_ATTEMPTS = 10;

    [Button("Generate Complete Dungeon")]
    public void GenerateDungeon()
    {
        ClearDungeon();

        for (int attempt = 0; attempt < MAX_GENERATION_ATTEMPTS; attempt++)
        {
            Debug.Log($"Dungeon generation attempt {attempt + 1}/{MAX_GENERATION_ATTEMPTS}");

            if (GenerateFloorPlan())
            {
                Debug.Log("Floor plan generated successfully");

                if (BuildPhysicalRooms())
                {
                    Debug.Log("Physical rooms built successfully");
                    SetupDoorStates();
                    PopulateRooms();
                    SetupMinimap();
                    SpawnPlayer();
                    return;
                }

                Debug.Log("Failed to build physical rooms, retrying generation");
            }

            ClearFloorPlan();
        }

        Debug.LogError("Failed to generate dungeon after max attempts");
    }

    void ClearDungeon()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (minimapManager != null)
        {
            minimapManager.ClearMinimap();
        }

        ClearFloorPlan();
    }

    void ClearFloorPlan()
    {
        floorPlan.Clear();
        pathNodes.Clear();
        addNodes.Clear();
        extNodes.Clear();
        startNode = null;
        bossNode = null;
    }

    bool GenerateFloorPlan()
    {
        if (!CreateMainPath()) return false;
        if (!AddBranchRooms()) return false;
        if (!AddExtensionRooms()) return false;
        if (!ReplaceRoomTypes()) return false;
        if (!AddExtraConnections()) return false;

        return true;
    }

    bool BuildPhysicalRooms()
    {
        Queue<GridNode> toProcess = new Queue<GridNode>();
        HashSet<Vector2Int> processedRooms = new HashSet<Vector2Int>();
        HashSet<(Vector2Int, int)> processedConnections = new HashSet<(Vector2Int, int)>();

        // place start room at origin
        GameObject startRoomObj = TryPlaceRoom(startNode, transform.position);
        if (startRoomObj == null)
        {
            Debug.LogError($"Failed to place start room");
            return false;
        }

        startNode.spawnedRoom = startRoomObj;
        SetupRoomComponents(startRoomObj, startNode);

        toProcess.Enqueue(startNode);
        processedRooms.Add(startNode.position);

        // process rooms breadth-first to place all rooms
        while (toProcess.Count > 0)
        {
            GridNode currentNode = toProcess.Dequeue();

            for (int dir = 0; dir < 4; dir++)
            {
                if (!currentNode.connections[dir]) continue;

                Vector2Int neighborPos = currentNode.position + DirectionToVector(dir);

                // skip if room already placed
                if (processedRooms.Contains(neighborPos)) continue;

                if (!floorPlan.ContainsKey(neighborPos))
                {
                    Debug.LogWarning($"Neighbor {neighborPos} not in floor plan");
                    continue;
                }

                GridNode neighborNode = floorPlan[neighborPos];

                // spawn hallway
                GameObject hallway = SpawnHallway(currentNode, dir);
                if (hallway == null)
                {
                    Debug.LogError($"Failed to spawn hallway from {currentNode.position} dir {dir}");
                    return false;
                }
                processedConnections.Add((currentNode.position, dir));

                // find hallway exit connector
                DoorConnector hallwayExit = GetHallwayExitConnector(hallway);
                if (hallwayExit == null)
                {
                    Debug.LogError($"Hallway has no exit connector");
                    return false;
                }

                // place room at hallway exit
                int oppositeDir = OppositeDirection(dir);
                GameObject neighborRoom = TryPlaceRoomAtConnector(neighborNode, hallwayExit, oppositeDir);
                if (neighborRoom == null)
                {
                    Debug.LogError($"Failed to place room at {neighborPos}");
                    return false;
                }

                neighborNode.spawnedRoom = neighborRoom;
                SetupRoomComponents(neighborRoom, neighborNode);

                toProcess.Enqueue(neighborNode);
                processedRooms.Add(neighborPos);
            }
        }

        // spawn remaining hallways for connections not covered by BFS tree
        foreach (var kvp in floorPlan)
        {
            GridNode node = kvp.Value;

            for (int dir = 0; dir < 4; dir++)
            {
                if (!node.connections[dir]) continue;

                // skip if hallway already spawned from this position/direction
                if (processedConnections.Contains((node.position, dir))) continue;

                // check if hallway already exists spatially
                string expectedHallwayName = $"Hallway_{node.position}_{DirectionToVector(dir)}";
                if (HallwayExists(expectedHallwayName))
                {
                    continue;
                }

                GameObject hallway = SpawnHallway(node, dir);
                if (hallway == null)
                {
                    Debug.LogError($"Failed to spawn extra hallway from {node.position} dir {dir}");
                    return false;
                }
            }
        }

        return true;
    }

    void SetupRoomComponents(GameObject room, GridNode node)
    {
        RoomStarter starter = room.GetComponent<RoomStarter>();
        if (starter != null)
        {
            starter.gridPosition = node.position;
        }

        // Note: PopulateRoom is called later in PopulateRooms() after all physical construction is complete
    }

    GameObject TryPlaceRoom(GridNode node, Vector3 worldPos)
    {
        if (node.roomType == null || node.roomType.roomPrefabs == null || node.roomType.roomPrefabs.Count == 0)
        {
            Debug.LogError($"Node at {node.position} has no valid room prefabs");
            return null;
        }

        List<GameObject> availableRooms = new List<GameObject>(node.roomType.roomPrefabs);
        Shuffle(availableRooms);

        foreach (GameObject prefab in availableRooms)
        {
            // try 4 rotations
            for (int rotStep = 0; rotStep < 4; rotStep++)
            {
                Quaternion rotation = Quaternion.Euler(0, rotStep * 90f, 0);
                GameObject room = SpawnRoom(prefab, worldPos, rotation);

                if (RoomMatchesConnections(room, node) && !RoomOverlaps(room))
                {
                    return room;
                }

                DestroyRoom(room);
            }
        }

        Debug.LogError($"No room prefab fits at {node.position}");
        return null;
    }

    GameObject TryPlaceRoomAtConnector(GridNode node, DoorConnector hallwayExit, int requiredDir)
    {
        if (node.roomType == null || node.roomType.roomPrefabs == null || node.roomType.roomPrefabs.Count == 0)
            return null;

        List<GameObject> prefabs = new List<GameObject>(node.roomType.roomPrefabs);
        Shuffle(prefabs);

        Vector3 hallwayExitForward = hallwayExit.transform.forward;

        foreach (GameObject prefab in prefabs)
        {
            // try all 4 rotations
            for (int rotStep = 0; rotStep < 4; rotStep++)
            {
                float yAngle = rotStep * 90f;
                Quaternion roomRotation = Quaternion.Euler(0, yAngle, 0);

                // spawn room at origin with test rotation
                GameObject room = SpawnRoom(prefab, Vector3.zero, roomRotation);
                Physics.SyncTransforms();

                // check if room matches floor plan requirements at this rotation
                if (!RoomMatchesConnections(room, node))
                {
                    DestroyRoom(room);
                    continue;
                }

                // find connector facing required direction
                DoorConnector matchingConnector = null;
                foreach (var connector in room.GetComponentsInChildren<DoorConnector>())
                {
                    if (!connector.isHallway && (int)connector.GetWorldDirection() == requiredDir)
                    {
                        matchingConnector = connector;
                        break;
                    }
                }

                if (matchingConnector == null)
                {
                    DestroyRoom(room);
                    continue;
                }

                // check if connector's forward aligns with hallway (opposite directions)
                Vector3 connectorForward = matchingConnector.transform.forward;
                float alignment = Vector3.Dot(connectorForward, -hallwayExitForward);

                if (alignment < 0.9f) // not aligned enough
                {
                    DestroyRoom(room);
                    continue;
                }

                // snap position so connector aligns with hallway exit
                Vector3 offset = matchingConnector.transform.position - room.transform.position;
                room.transform.position = hallwayExit.transform.position - offset;
                Physics.SyncTransforms();

                // final overlap check
                if (!RoomOverlaps(room))
                {
                    return room;
                }

                DestroyRoom(room);
            }
        }

        return null;
    }

    bool RoomMatchesConnections(GameObject room, GridNode node)
    {
        DoorConnector[] connectors = room.GetComponentsInChildren<DoorConnector>();

        // count connectors in each direction
        int[] foundConnections = new int[4];
        int[] requiredConnections = new int[4];

        foreach (var connector in connectors)
        {
            if (connector.isHallway) continue;
            int dir = (int)connector.GetWorldDirection();
            foundConnections[dir]++;
        }

        for (int dir = 0; dir < 4; dir++)
        {
            if (node.connections[dir])
                requiredConnections[dir] = 1;
        }

        // verify required connections are present
        for (int dir = 0; dir < 4; dir++)
        {
            if (requiredConnections[dir] > 0 && foundConnections[dir] == 0)
                return false;
        }

        return true;
    }

    bool RoomOverlaps(GameObject room)
    {
        Transform floorTransform = room.transform.Find("CeilingFloor/Floor");
        if (floorTransform == null)
        {
            Debug.LogWarning($"No Floor found in {room.name}");
            return false;
        }

        Collider floorCollider = null;
        foreach (Collider col in floorTransform.GetComponents<Collider>())
        {
            if (!col.isTrigger)
            {
                floorCollider = col;
                break;
            }
        }

        if (floorCollider == null) return false;

        // check for overlaps with other floor colliders
        Collider[] overlaps = Physics.OverlapBox(
            floorCollider.bounds.center,
            floorCollider.bounds.extents,
            room.transform.rotation
        );

        foreach (var overlap in overlaps)
        {
            if (!overlap.CompareTag("Floor") || overlap.isTrigger) continue;
            if (overlap == floorCollider) continue;
            if (overlap.gameObject == floorCollider.gameObject) continue;

            // check if overlap belongs to this room
            Transform parent = overlap.transform;
            while (parent != null)
            {
                if (parent == room.transform) break;
                parent = parent.parent;
            }
            if (parent == room.transform) continue;

            return true;
        }

        return false;
    }

    GameObject SpawnHallway(GridNode node, int direction)
    {
        if (hallwayPrefab == null) return null;

        // find room's connector in this direction
        DoorConnector roomConnector = null;
        foreach (var connector in node.spawnedRoom.GetComponentsInChildren<DoorConnector>())
        {
            if (!connector.isHallway && (int)connector.GetWorldDirection() == direction)
            {
                roomConnector = connector;
                break;
            }
        }

        if (roomConnector == null)
        {
            Debug.LogWarning($"No connector found in direction {direction}");
            return null;
        }

        // spawn hallway and find its start connector
        GameObject tempHallway = Instantiate(hallwayPrefab);
        DoorConnector hallwayStart = null;
        foreach (var conn in tempHallway.GetComponentsInChildren<DoorConnector>())
        {
            if (conn.isHallway)
            {
                hallwayStart = conn;
                break;
            }
        }

        if (hallwayStart == null)
        {
            DestroyImmediate(tempHallway);
            return null;
        }

        // calculate snap position/rotation
        Vector3 connectorOffset = hallwayStart.transform.position - tempHallway.transform.position;
        Quaternion connectorRotOffset = Quaternion.Inverse(tempHallway.transform.rotation) * hallwayStart.transform.rotation;
        DestroyImmediate(tempHallway);

        Quaternion targetRot = roomConnector.transform.rotation * Quaternion.Inverse(connectorRotOffset) * Quaternion.Euler(0, 180, 0);
        Vector3 targetPos = roomConnector.transform.position - (targetRot * connectorOffset);

        GameObject hallway = SpawnRoom(hallwayPrefab, targetPos, targetRot);
        hallway.name = $"Hallway_{node.position}_{DirectionToVector(direction)}";
        return hallway;
    }

    DoorConnector GetHallwayExitConnector(GameObject hallway)
    {
        DoorConnector[] connectors = hallway.GetComponentsInChildren<DoorConnector>();

        // find non-hallway connector (exit)
        foreach (var conn in connectors)
        {
            if (!conn.isHallway)
                return conn;
        }

        // fallback to second connector
        if (connectors.Length > 1)
            return connectors[1];

        return null;
    }

    bool HallwayExists(string hallwayName)
    {
        foreach (Transform child in transform)
        {
            if (child.name == hallwayName)
                return true;
        }
        return false;
    }

    GameObject SpawnRoom(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject room;
        if (Application.isPlaying)
        {
            room = Instantiate(prefab, position, rotation, transform);
        }
        else
        {
#if UNITY_EDITOR
            room = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            room.transform.SetPositionAndRotation(position, rotation);
#else
            room = Instantiate(prefab, position, rotation, transform);
#endif
        }
        return room;
    }

    void DestroyRoom(GameObject room)
    {
        if (Application.isPlaying)
            Destroy(room);
        else
            DestroyImmediate(room);
    }

    bool CreateMainPath()
    {
        startNode = new GridNode { position = Vector2Int.zero, roomType = startRoom };
        floorPlan[Vector2Int.zero] = startNode;
        pathNodes.Add(startNode);

        Vector2Int currentPos = Vector2Int.zero;

        for (int i = 0; i < pathLength; i++)
        {
            List<int> directions = new List<int> { 0, 1, 2, 3 };
            Shuffle(directions);

            bool placed = false;
            foreach (int dir in directions)
            {
                Vector2Int nextPos = currentPos + DirectionToVector(dir);

                if (!floorPlan.ContainsKey(nextPos))
                {
                    GridNode currentNode = floorPlan[currentPos];
                    currentNode.connections[dir] = true;

                    GridNode newNode = new GridNode { position = nextPos, roomType = normalRoom };
                    newNode.connections[OppositeDirection(dir)] = true;
                    floorPlan[nextPos] = newNode;
                    pathNodes.Add(newNode);

                    currentPos = nextPos;
                    placed = true;
                    break;
                }
            }

            if (!placed) return false;
        }

        // place boss room
        List<int> bossDirections = new List<int> { 0, 1, 2, 3 };
        Shuffle(bossDirections);

        foreach (int dir in bossDirections)
        {
            Vector2Int bossPos = currentPos + DirectionToVector(dir);

            if (!floorPlan.ContainsKey(bossPos) && IsValidBossPlacement(bossPos, dir))
            {
                GridNode currentNode = floorPlan[currentPos];
                currentNode.connections[dir] = true;

                bossNode = new GridNode { position = bossPos, roomType = bossRoom };
                bossNode.connections[OppositeDirection(dir)] = true;
                floorPlan[bossPos] = bossNode;

                return true;
            }
        }

        return false;
    }

    bool IsValidBossPlacement(Vector2Int pos, int connectionDir)
    {
        for (int dir = 0; dir < 4; dir++)
        {
            if (dir == OppositeDirection(connectionDir)) continue;

            Vector2Int adjacentPos = pos + DirectionToVector(dir);
            if (floorPlan.ContainsKey(adjacentPos))
                return false;
        }
        return true;
    }

    bool AddBranchRooms()
    {
        for (int i = 0; i < numberOfAddRooms; i++)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();

            foreach (var node in pathNodes)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    Vector2Int adjacentPos = node.position + DirectionToVector(dir);

                    if (!floorPlan.ContainsKey(adjacentPos) && !IsAdjacentToBoss(adjacentPos))
                    {
                        validPositions.Add(adjacentPos);
                    }
                }
            }

            if (validPositions.Count == 0) return false;

            Vector2Int selectedPos = validPositions[Random.Range(0, validPositions.Count)];

            for (int dir = 0; dir < 4; dir++)
            {
                Vector2Int neighborPos = selectedPos + DirectionToVector(dir);
                if (floorPlan.ContainsKey(neighborPos) && floorPlan[neighborPos] != bossNode)
                {
                    GridNode neighborNode = floorPlan[neighborPos];
                    neighborNode.connections[OppositeDirection(dir)] = true;

                    GridNode newNode = new GridNode { position = selectedPos, roomType = normalRoom };
                    newNode.connections[dir] = true;
                    floorPlan[selectedPos] = newNode;
                    addNodes.Add(newNode);
                    break;
                }
            }
        }

        return true;
    }

    bool AddExtensionRooms()
    {
        for (int i = 0; i < numberOfExtRooms; i++)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();

            foreach (var node in addNodes)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    Vector2Int adjacentPos = node.position + DirectionToVector(dir);

                    if (!floorPlan.ContainsKey(adjacentPos) && !IsAdjacentToBoss(adjacentPos))
                    {
                        validPositions.Add(adjacentPos);
                    }
                }
            }

            if (validPositions.Count == 0) return true;

            Vector2Int selectedPos = validPositions[Random.Range(0, validPositions.Count)];

            for (int dir = 0; dir < 4; dir++)
            {
                Vector2Int neighborPos = selectedPos + DirectionToVector(dir);
                if (floorPlan.ContainsKey(neighborPos) && addNodes.Contains(floorPlan[neighborPos]))
                {
                    GridNode neighborNode = floorPlan[neighborPos];
                    neighborNode.connections[OppositeDirection(dir)] = true;

                    GridNode newNode = new GridNode { position = selectedPos, roomType = normalRoom };
                    newNode.connections[dir] = true;
                    floorPlan[selectedPos] = newNode;
                    extNodes.Add(newNode);
                    break;
                }
            }
        }

        return true;
    }

    bool IsAdjacentToBoss(Vector2Int pos)
    {
        if (bossNode == null) return false;

        for (int dir = 0; dir < 4; dir++)
        {
            if (pos + DirectionToVector(dir) == bossNode.position)
                return true;
        }
        return false;
    }

    bool ReplaceRoomTypes()
    {
        List<GridNode> replaceableCandidates = new List<GridNode>();
        replaceableCandidates.AddRange(addNodes);
        replaceableCandidates.AddRange(extNodes);
        replaceableCandidates.RemoveAll(node => node.roomType != normalRoom);

        if (keyRoom != null)
        {
            if (!ReplaceWithRoomType(keyRoom, replaceableCandidates))
                return false;
        }

        foreach (RoomType customType in additionalRoomTypes)
        {
            if (customType != null)
            {
                if (!ReplaceWithRoomType(customType, replaceableCandidates))
                    return false;
            }
        }

        return true;
    }

    bool ReplaceWithRoomType(RoomType roomType, List<GridNode> candidates)
    {
        int rolledSpawns = Random.Range(roomType.possibleSpawns.x, roomType.possibleSpawns.y + 1);
        int placedCount = 0;
        int maxAttempts = candidates.Count * 3;

        for (int attempt = 0; attempt < maxAttempts && placedCount < rolledSpawns; attempt++)
        {
            List<GridNode> validCandidates = candidates.FindAll(node => node.roomType == normalRoom);

            // if isExtRoom flag is set, only allow extension rooms
            if (roomType.isExtRoom)
            {
                validCandidates = validCandidates.FindAll(node => extNodes.Contains(node));
            }

            if (validCandidates.Count == 0) break;

            GridNode candidate = validCandidates[Random.Range(0, validCandidates.Count)];

            int distToStart = GetDistanceBetween(candidate.position, startNode.position);
            int distToBoss = GetDistanceBetween(candidate.position, bossNode.position);

            bool validStart = (distToStart >= roomType.distFromStart.x && distToStart <= roomType.distFromStart.y);
            bool validBoss = (distToBoss >= roomType.distFromBoss.x && distToBoss <= roomType.distFromBoss.y);

            int connectionCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if (candidate.connections[i]) connectionCount++;
            }
            bool validConnections = (connectionCount >= roomType.minConnections && connectionCount <= roomType.maxConnections);

            if (validStart && validBoss && validConnections)
            {
                candidate.roomType = roomType;
                placedCount++;
            }
        }

        if (placedCount >= roomType.possibleSpawns.x)
            return true;

        string extRoomNote = roomType.isExtRoom ? " (isExtRoom=true, only extension rooms allowed)" : "";
        Debug.LogWarning($"Failed to place minimum {roomType.name}: {placedCount}/{roomType.possibleSpawns.x}{extRoomNote}");
        return false;
    }

    bool AddExtraConnections()
    {
        if (numberOfExtConnections <= 0) return true;

        List<(Vector2Int pos1, Vector2Int pos2, int dir)> potentialConnections = new List<(Vector2Int, Vector2Int, int)>();

        foreach (var kvp in floorPlan)
        {
            GridNode node = kvp.Value;
            if (node.roomType == bossRoom) continue;

            for (int dir = 0; dir < 4; dir++)
            {
                if (!node.connections[dir])
                {
                    Vector2Int neighborPos = node.position + DirectionToVector(dir);
                    if (floorPlan.ContainsKey(neighborPos) && floorPlan[neighborPos].roomType != bossRoom)
                    {
                        potentialConnections.Add((node.position, neighborPos, dir));
                    }
                }
            }
        }

        Shuffle(potentialConnections);

        int connectionsAdded = 0;
        foreach (var connection in potentialConnections)
        {
            if (connectionsAdded >= numberOfExtConnections) break;

            if (IsValidExtraConnection(connection.pos1, connection.pos2, connection.dir))
            {
                GridNode node1 = floorPlan[connection.pos1];
                GridNode node2 = floorPlan[connection.pos2];

                node1.connections[connection.dir] = true;
                node2.connections[OppositeDirection(connection.dir)] = true;

                connectionsAdded++;
            }
        }

        return true;
    }

    bool IsValidExtraConnection(Vector2Int pos1, Vector2Int pos2, int dir)
    {
        GridNode node1 = floorPlan[pos1];
        GridNode node2 = floorPlan[pos2];

        // temporarily add connection
        node1.connections[dir] = true;
        node2.connections[OppositeDirection(dir)] = true;

        bool valid = true;
        string invalidReason = "";

        // check start to boss distance is maintained at pathLength
        int startToBossDist = GetDistanceBetween(startNode.position, bossNode.position);
        if (startToBossDist < pathLength)
        {
            invalidReason = $"Connection would reduce start-to-boss distance to {startToBossDist}, below pathLength {pathLength}";
            valid = false;
        }

        // check all special rooms still meet distance constraints
        if (valid)
        {
            foreach (var kvp in floorPlan)
            {
                GridNode node = kvp.Value;

                if (node.roomType == normalRoom || node.roomType == bossRoom || node.roomType == startRoom)
                    continue;

                int distToStart = GetDistanceBetween(node.position, startNode.position);
                int distToBoss = GetDistanceBetween(node.position, bossNode.position);

                bool validStart = (distToStart >= node.roomType.distFromStart.x && distToStart <= node.roomType.distFromStart.y);
                bool validBoss = (distToBoss >= node.roomType.distFromBoss.x && distToBoss <= node.roomType.distFromBoss.y);

                if (!validStart)
                {
                    invalidReason = $"Room at {node.position} ({node.roomType.name}) distFromStart={distToStart} violates range [{node.roomType.distFromStart.x}, {node.roomType.distFromStart.y}]";
                    valid = false;
                    break;
                }
                if (!validBoss)
                {
                    invalidReason = $"Room at {node.position} ({node.roomType.name}) distFromBoss={distToBoss} violates range [{node.roomType.distFromBoss.x}, {node.roomType.distFromBoss.y}]";
                    valid = false;
                    break;
                }
            }
        }

        // check max connections
        if (valid)
        {
            int connections1 = 0, connections2 = 0;
            for (int i = 0; i < 4; i++)
            {
                if (node1.connections[i]) connections1++;
                if (node2.connections[i]) connections2++;
            }

            if (node1.roomType != normalRoom && connections1 > node1.roomType.maxConnections)
            {
                invalidReason = $"Room at {pos1} ({node1.roomType.name}) would have {connections1} connections, exceeds max {node1.roomType.maxConnections}";
                valid = false;
            }
            if (node2.roomType != normalRoom && connections2 > node2.roomType.maxConnections)
            {
                invalidReason = $"Room at {pos2} ({node2.roomType.name}) would have {connections2} connections, exceeds max {node2.roomType.maxConnections}";
                valid = false;
            }
        }

        // remove temporary connection
        node1.connections[dir] = false;
        node2.connections[OppositeDirection(dir)] = false;

        if (!valid)
        {
            Debug.Log($"Extra connection {pos1} -> {pos2} rejected: {invalidReason}");
        }

        return valid;
    }

    int GetDistanceBetween(Vector2Int start, Vector2Int end)
    {
        if (start == end) return 0;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDist = distances[current];

            if (current == end) return currentDist;

            GridNode currentNode = floorPlan[current];
            for (int dir = 0; dir < 4; dir++)
            {
                if (currentNode.connections[dir])
                {
                    Vector2Int neighbor = current + DirectionToVector(dir);
                    if (floorPlan.ContainsKey(neighbor) && !distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = currentDist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return int.MaxValue;
    }

    void SetupDoorStates()
    {
        Vector2Int bossConnectionPos = Vector2Int.zero;
        int bossConnectionDir = -1;

        for (int dir = 0; dir < 4; dir++)
        {
            if (bossNode.connections[dir])
            {
                bossConnectionPos = bossNode.position + DirectionToVector(dir);
                bossConnectionDir = OppositeDirection(dir);
                break;
            }
        }

        foreach (var kvp in floorPlan)
        {
            GridNode node = kvp.Value;
            if (node.spawnedRoom == null) continue;

            DoorConnector[] connectors = node.spawnedRoom.GetComponentsInChildren<DoorConnector>();

            foreach (DoorConnector connector in connectors)
            {
                if (connector.isHallway) continue;

                ConnectionDirection connectorDir = connector.GetWorldDirection();
                int dirIndex = (int)connectorDir;

                bool isDoor = node.connections[dirIndex];
                bool isLocked = (node.position == bossConnectionPos && dirIndex == bossConnectionDir);

                connector.SetConnectionState(isDoor, isLocked);
            }
        }
    }

    void PopulateRooms()
    {
        foreach (var kvp in floorPlan)
        {
            GridNode node = kvp.Value;
            if (node.spawnedRoom == null) continue;

            RoomPopulator populator = node.spawnedRoom.GetComponent<RoomPopulator>();
            if (populator != null)
            {
                Debug.Log($"PopulateRooms: {node.spawnedRoom.name} has RoomPopulator, calling PopulateRoom");
                populator.PopulateRoom(node.roomType);
            }
            else
            {
                RoomStarter starter = node.spawnedRoom.GetComponent<RoomStarter>();
                if (starter != null)
                {
                    Debug.Log($"PopulateRooms: {node.spawnedRoom.name} has no RoomPopulator, calling RoomStart directly");
                    starter.RoomStart();
                }
                else
                {
                    Debug.LogError($"PopulateRooms: {node.spawnedRoom.name} has neither RoomPopulator nor RoomStarter!");
                }
            }
        }
    }

    void SetupMinimap()
    {
        if (minimapManager != null)
        {
            minimapManager.GenerateMinimap(floorPlan, startNode, revealAllRooms);
        }
    }

    void SpawnPlayer()
    {
        if (startNode == null || startNode.spawnedRoom == null)
        {
            Debug.LogError("Start room not found");
            return;
        }

        Transform startPoint = null;
        foreach (Transform child in startNode.spawnedRoom.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("StartPoint"))
            {
                startPoint = child;
                break;
            }
        }

        if (startPoint == null)
        {
            Debug.LogWarning("No StartPoint tag found, using room center");
            startPoint = startNode.spawnedRoom.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 spawnPos = startPoint.position + Vector3.up * startNode.roomType.vertOffset;
            player.transform.position = spawnPos;
            player.transform.rotation = startPoint.rotation;
        }
    }

    Vector2Int DirectionToVector(int direction)
    {
        switch (direction)
        {
            case 0: return Vector2Int.up;    // North
            case 1: return Vector2Int.down;  // South
            case 2: return Vector2Int.right; // East
            case 3: return Vector2Int.left;  // West
            default: return Vector2Int.zero;
        }
    }

    int OppositeDirection(int direction)
    {
        switch (direction)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 3;
            case 3: return 2;
            default: return direction;
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || floorPlan == null || floorPlan.Count == 0) return;

        foreach (var kvp in floorPlan)
        {
            GridNode node = kvp.Value;
            Vector3 worldPos = transform.position + new Vector3(node.position.x * 10f, 0, node.position.y * 10f);

            if (node == startNode)
                Gizmos.color = Color.green;
            else if (node == bossNode)
                Gizmos.color = Color.red;
            else if (pathNodes.Contains(node))
                Gizmos.color = Color.yellow;
            else if (addNodes.Contains(node))
                Gizmos.color = Color.cyan;
            else if (extNodes.Contains(node))
                Gizmos.color = Color.magenta;
            else
                Gizmos.color = Color.white;

            Gizmos.DrawWireCube(worldPos, Vector3.one * gizmoSize);

            Gizmos.color = Color.white;
            for (int dir = 0; dir < 4; dir++)
            {
                if (node.connections[dir])
                {
                    Vector3 directionVector = new Vector3(DirectionToVector(dir).x, 0, DirectionToVector(dir).y) * 5f;
                    Gizmos.DrawLine(worldPos, worldPos + directionVector);
                }
            }
        }
    }

    [Button("Clear Dungeon")]
    void ClearDungeonButton()
    {
        ClearDungeon();
    }
}
