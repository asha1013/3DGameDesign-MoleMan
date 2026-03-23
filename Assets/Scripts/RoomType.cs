using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "New Room Type", menuName = "Dungeon/Room Type")]
public class RoomType : ScriptableObject
{
    public List<GameObject> roomPrefabs = new List<GameObject>();

    [BoxGroup("Room Type Settings")]
    public bool normalRoom = false;

    [BoxGroup("Room Type Settings")]
    [ShowIf("normalRoom")]
    public GameObject defaultRoom;

    [BoxGroup("Room Type Settings")]
    public bool startRoom = false;

    [BoxGroup("Room Type Settings")]
    [ShowIf("startRoom")]
    [InfoBox("Start rooms should have a child GameObject tagged 'StartPoint' for player spawn")]
    public float vertOffset = 1f;

    [BoxGroup("Room Type Settings")]
    public bool keyRoom = false;

    [BoxGroup("Room Type Settings")]
    public bool bossRoom = false;

    [BoxGroup("Room Type Settings")]
    [ShowIf("bossRoom")]
    [InfoBox("Boss rooms should have a child GameObject tagged 'BossSpawner' for boss spawn")]
    public List<GameObject> bossPrefabs = new List<GameObject>();

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowDistanceFields")]
    [MinMaxSlider(0, 8)]
    public Vector2Int distFromBoss = new Vector2Int(0, 8);

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowDistanceFields")]
    [MinMaxSlider(0, 8)]
    public Vector2Int distFromStart = new Vector2Int(0, 8);

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowSpecialRoomFields")]
    [MinMaxSlider(0, 6)]
    public Vector2Int possibleSpawns = new Vector2Int(0, 6);

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowMaxConnections")]
    [Range(1, 4)]
    public int maxConnections = 2;

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowMaxConnections")]
    [Range(0, 4)]
    public int minConnections = 0;

    [BoxGroup("Special Room Constraints")]
    [ShowIf("ShowDistanceFields")]
    [InfoBox("If true, this room type can only replace extension rooms (not add rooms)")]
    public bool isExtRoom = false;

    [BoxGroup("Minimap")]
    public Sprite minimapIcon;

    // conditional display helpers
    bool ShowDistanceFields() => !normalRoom && !startRoom && !bossRoom;
    bool ShowSpecialRoomFields() => !normalRoom && !startRoom && !bossRoom && !keyRoom;
    bool ShowMaxConnections() => !bossRoom && !normalRoom;
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public int difficulty = 1;
    public bool isRanged = false;
}
