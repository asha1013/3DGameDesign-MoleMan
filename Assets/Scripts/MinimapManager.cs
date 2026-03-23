using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

public class MinimapManager : MonoBehaviour
{
    private static MinimapManager instance;
    public static MinimapManager Instance => instance;

    [BoxGroup("Minimap Settings")] public GameObject minimapPanel;
    [BoxGroup("Minimap Settings")] public GameObject mapIconPrefab;
    [BoxGroup("Minimap Settings")] [Range(0f, 1f)] public float iconAlpha = 1f;
    [BoxGroup("Minimap Settings")] public float connectThickness = 10f;
    [BoxGroup("Minimap Settings")] public Color connectColor = Color.white;

    [BoxGroup("Sprites")] public Sprite roomSquare;
    [BoxGroup("Sprites")] public Sprite currentRoomSquare;

    Dictionary<Vector2Int, GameObject> roomIcons = new Dictionary<Vector2Int, GameObject>();
    Dictionary<Vector2Int, Shadow[]> roomShadows = new Dictionary<Vector2Int, Shadow[]>();
    HashSet<Vector2Int> discoveredRooms = new HashSet<Vector2Int>();
    Dictionary<Vector2Int, DungeonBuilder.GridNode> floorPlan;
    DungeonBuilder.GridNode startNode;
    Vector2Int? currentRoomPos = null;

    bool isInitialized = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void GenerateMinimap(Dictionary<Vector2Int, DungeonBuilder.GridNode> plan, DungeonBuilder.GridNode start, bool revealAll = false)
    {
        if (minimapPanel == null || mapIconPrefab == null)
        {
            Debug.LogWarning("Minimap panel or icon prefab not assigned");
            return;
        }

        floorPlan = plan;
        startNode = start;

        // clear existing minimap - use DestroyImmediate to ensure objects are removed before rebuilding
        for (int i = minimapPanel.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(minimapPanel.transform.GetChild(i).gameObject);
        }
        roomIcons.Clear();
        roomShadows.Clear();
        discoveredRooms.Clear();

        // find grid bounds
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var kvp in floorPlan)
        {
            Vector2Int pos = kvp.Key;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        int gridWidth = maxX - minX + 1;
        int gridHeight = maxY - minY + 1;

        // configure grid layout
        GridLayoutGroup gridLayout = minimapPanel.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = gridWidth;
        }

        // create grid of icons - top to bottom, left to right
        for (int y = maxY; y >= minY; y--)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (floorPlan.ContainsKey(pos))
                {
                    // create room icon
                    GameObject icon = Instantiate(mapIconPrefab, minimapPanel.transform);
                    icon.name = $"MapIcon_{pos}";

                    // get components
                    Image roomSquareImg = icon.GetComponent<Image>();
                    Image roomIcon = icon.transform.GetChild(0).GetComponent<Image>();

                    if (roomSquareImg != null)
                    {
                        if (roomSquare != null)
                        {
                            roomSquareImg.sprite = roomSquare;
                        }
                        Color squareColor = roomSquareImg.color;
                        squareColor.a = iconAlpha;
                        roomSquareImg.color = squareColor;
                        roomSquareImg.enabled = false; // start disabled
                    }

                    if (roomIcon != null)
                    {
                        DungeonBuilder.GridNode iconNode = floorPlan[pos];
                        if (iconNode.roomType != null && iconNode.roomType.minimapIcon != null)
                        {
                            roomIcon.sprite = iconNode.roomType.minimapIcon;
                        }
                        Color iconColor = roomIcon.color;
                        iconColor.a = iconAlpha;
                        roomIcon.color = iconColor;
                        roomIcon.enabled = false; // start disabled
                    }

                    // add shadows for connections (disabled initially)
                    DungeonBuilder.GridNode node = floorPlan[pos];
                    Shadow[] shadows = new Shadow[4];
                    if (node.connections[0]) shadows[0] = AddConnectionShadow(icon, Vector2.up * connectThickness, false); // North
                    if (node.connections[1]) shadows[1] = AddConnectionShadow(icon, Vector2.down * connectThickness, false); // South
                    if (node.connections[2]) shadows[2] = AddConnectionShadow(icon, Vector2.right * connectThickness, false); // East
                    if (node.connections[3]) shadows[3] = AddConnectionShadow(icon, Vector2.left * connectThickness, false); // West

                    roomIcons[pos] = icon;
                    roomShadows[pos] = shadows;
                }
                else
                {
                    // create empty spacer with LayoutElement
                    GameObject spacer = new GameObject($"Spacer_{x}_{y}");
                    spacer.transform.SetParent(minimapPanel.transform);

                    // add LayoutElement so it takes up grid space
                    LayoutElement layoutElement = spacer.AddComponent<LayoutElement>();
                    if (gridLayout != null)
                    {
                        layoutElement.minWidth = gridLayout.cellSize.x;
                        layoutElement.minHeight = gridLayout.cellSize.y;
                    }
                }
            }
        }

        // enable starting room
        if (roomIcons.ContainsKey(startNode.position))
        {
            RevealRoom(startNode.position);
        }

        // reveal all rooms if debug mode enabled
        if (revealAll)
        {
            foreach (var kvp in floorPlan)
            {
                RevealRoom(kvp.Key);
            }
        }

        isInitialized = true;
        StartCoroutine(UpdateMinimap());
    }

    Shadow AddConnectionShadow(GameObject icon, Vector2 effectDistance, bool enabled)
    {
        Shadow shadow = icon.AddComponent<Shadow>();
        shadow.effectDistance = effectDistance;
        shadow.effectColor = connectColor;
        shadow.useGraphicAlpha = true;
        shadow.enabled = enabled;
        return shadow;
    }

    void RevealRoom(Vector2Int pos)
    {
        if (!roomIcons.ContainsKey(pos)) return;

        // mark as discovered
        bool wasAlreadyDiscovered = discoveredRooms.Contains(pos);
        discoveredRooms.Add(pos);

        GameObject icon = roomIcons[pos];
        Image roomSquareImg = icon.GetComponent<Image>();
        Image roomIcon = icon.transform.GetChild(0).GetComponent<Image>();

        // enable room square
        if (roomSquareImg != null)
        {
            roomSquareImg.enabled = true;
        }

        // enable room icon only if roomType has a minimapIcon set
        if (roomIcon != null)
        {
            DungeonBuilder.GridNode node = floorPlan[pos];
            if (node.roomType != null && node.roomType.minimapIcon != null)
            {
                roomIcon.enabled = true;
            }
            else
            {
                roomIcon.enabled = false;
            }
        }

        // reveal all connected rooms
        DungeonBuilder.GridNode currentNode = floorPlan[pos];
        for (int dir = 0; dir < 4; dir++)
        {
            if (currentNode.connections[dir])
            {
                Vector2Int neighborPos = pos + DirectionToVector(dir);
                if (roomIcons.ContainsKey(neighborPos) && !discoveredRooms.Contains(neighborPos))
                {
                    GameObject neighborIcon = roomIcons[neighborPos];
                    Image neighborSquare = neighborIcon.GetComponent<Image>();
                    Image neighborIconImg = neighborIcon.transform.GetChild(0).GetComponent<Image>();

                    // enable neighbor square
                    if (neighborSquare != null)
                    {
                        neighborSquare.enabled = true;
                    }

                    // enable neighbor icon only if it has a minimapIcon
                    if (neighborIconImg != null)
                    {
                        DungeonBuilder.GridNode neighborNode = floorPlan[neighborPos];
                        if (neighborNode.roomType != null && neighborNode.roomType.minimapIcon != null)
                        {
                            neighborIconImg.enabled = true;
                        }
                        else
                        {
                            neighborIconImg.enabled = false;
                        }
                    }
                }
            }
        }

        // update connection shadows for this room
        UpdateConnectionShadows(pos);

        // update connection shadows for newly revealed neighbors
        if (!wasAlreadyDiscovered)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                if (currentNode.connections[dir])
                {
                    Vector2Int neighborPos = pos + DirectionToVector(dir);
                    if (discoveredRooms.Contains(neighborPos))
                    {
                        UpdateConnectionShadows(neighborPos);
                    }
                }
            }
        }
    }

    void UpdateConnectionShadows(Vector2Int pos)
    {
        if (!roomShadows.ContainsKey(pos)) return;

        DungeonBuilder.GridNode node = floorPlan[pos];
        Shadow[] shadows = roomShadows[pos];

        for (int dir = 0; dir < 4; dir++)
        {
            if (shadows[dir] != null && node.connections[dir])
            {
                Vector2Int neighborPos = pos + DirectionToVector(dir);
                // enable shadow only if neighbor is also discovered
                shadows[dir].enabled = discoveredRooms.Contains(neighborPos);
            }
        }
    }

    public void SetCurrentRoom(Vector2Int pos)
    {
        if (!roomIcons.ContainsKey(pos)) return;

        // revert previous current room to normal roomSquare
        if (currentRoomPos.HasValue && roomIcons.ContainsKey(currentRoomPos.Value))
        {
            GameObject prevIcon = roomIcons[currentRoomPos.Value];
            Image prevSquare = prevIcon.GetComponent<Image>();
            if (prevSquare != null && roomSquare != null)
            {
                prevSquare.sprite = roomSquare;
            }
        }

        // set new current room
        currentRoomPos = pos;
        GameObject icon = roomIcons[pos];
        Image roomSquareImg = icon.GetComponent<Image>();

        if (roomSquareImg != null)
        {
            roomSquareImg.enabled = true;
            // set to currentRoomSquare sprite
            if (currentRoomSquare != null)
            {
                roomSquareImg.sprite = currentRoomSquare;
            }
        }

        // enable icon only if roomType has a minimapIcon
        Image roomIcon = icon.transform.GetChild(0).GetComponent<Image>();
        if (roomIcon != null)
        {
            DungeonBuilder.GridNode node = floorPlan[pos];
            if (node.roomType != null && node.roomType.minimapIcon != null)
            {
                roomIcon.enabled = true;
            }
            else
            {
                roomIcon.enabled = false;
            }
        }
    }

    IEnumerator UpdateMinimap()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (!isInitialized) continue;

            foreach (var kvp in floorPlan)
            {
                Vector2Int pos = kvp.Key;
                DungeonBuilder.GridNode node = kvp.Value;

                if (node.spawnedRoom == null) continue;

                RoomStarter starter = node.spawnedRoom.GetComponent<RoomStarter>();
                if (starter != null && starter.hasActivated)
                {
                    RevealRoom(pos);
                }
            }
        }
    }

    Vector2Int DirectionToVector(int direction)
    {
        switch (direction)
        {
            case 0: return Vector2Int.up; // North
            case 1: return Vector2Int.down; // South
            case 2: return Vector2Int.right; // East
            case 3: return Vector2Int.left; // West
            default: return Vector2Int.zero;
        }
    }

    public void ClearMinimap()
    {
        if (minimapPanel == null) return;

        // destroy all minimap icons
        for (int i = minimapPanel.transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(minimapPanel.transform.GetChild(i).gameObject);
            else
                DestroyImmediate(minimapPanel.transform.GetChild(i).gameObject);
        }

        roomIcons.Clear();
        roomShadows.Clear();
        discoveredRooms.Clear();
        floorPlan = null;
        startNode = null;
        currentRoomPos = null;
        isInitialized = false;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        if (instance == this)
        {
            instance = null;
        }
    }
}
