using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class RoomPopulator : MonoBehaviour
{
    [BoxGroup("Room Settings")] public bool hasItems = false;
    [BoxGroup("Room Settings")] public bool hasEnemies = false;

    [BoxGroup("Room Settings")] [ShowIf("hasEnemies")] public bool useDifficulty = true;

    [BoxGroup("Room Settings")] [ShowIf(EConditionOperator.And, "hasEnemies", "useDifficulty")] [Range(0, 20)] public int roomDifficulty = 5;

    [BoxGroup("Room Settings")] [ShowIf("hasEnemies")] public List<EnemySpawnData> enemies = new List<EnemySpawnData>();

    RoomStarter roomStarter;

    void Awake()
    {
        roomStarter = GetComponent<RoomStarter>();
    }

    public void PopulateRoom(RoomType roomType = null)
    {
        // check if this is a boss room
        if (roomType != null && roomType.bossRoom && roomType.bossPrefabs.Count > 0)
        {
            SpawnBoss(roomType);
        }
        else if (hasEnemies && enemies.Count > 0)
        {
            SpawnEnemies();
        }

        if (roomStarter != null)
        {
            roomStarter.RoomStart();
        }
        else
        {
            Debug.LogWarning($"RoomStarter not found on {gameObject.name}");
        }
    }

    void SpawnBoss(RoomType roomType)
    {
        // find BossSpawner tag in room
        Transform bossSpawner = null;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("BossSpawner"))
            {
                bossSpawner = child;
                break;
            }
        }

        if (bossSpawner == null)
        {
            Debug.LogWarning($"No BossSpawner tag found in {gameObject.name}, using room center");
            bossSpawner = transform;
        }

        // randomly select a boss prefab
        GameObject bossPrefab = roomType.bossPrefabs[Random.Range(0, roomType.bossPrefabs.Count)];
        if (bossPrefab != null)
        {
            GameObject boss = Instantiate(bossPrefab, bossSpawner.position, bossSpawner.rotation, transform);
            Debug.Log($"Spawned boss {bossPrefab.name} in {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Boss prefab is null in {gameObject.name}");
        }
    }

    void SpawnEnemies()
    {
        // find all spawners in room
        GameObject[] enemySpawners = GameObject.FindGameObjectsWithTag("EnemySpawner");
        GameObject[] rangedSpawners = GameObject.FindGameObjectsWithTag("RangedEnemySpawner");

        List<GameObject> allSpawners = new List<GameObject>();
        allSpawners.AddRange(enemySpawners);
        allSpawners.AddRange(rangedSpawners);

        // filter to only spawners that are children of this room
        allSpawners.RemoveAll(spawner => !spawner.transform.IsChildOf(transform));

        if (allSpawners.Count == 0)
        {
            Debug.Log($"No enemy spawners found in {gameObject.name}");
            return;
        }

        if (useDifficulty)
        {
            SpawnByDifficulty(allSpawners, enemySpawners, rangedSpawners);
        }
        else
        {
            SpawnRandom(allSpawners, enemySpawners, rangedSpawners);
        }
    }

    void SpawnByDifficulty(List<GameObject> allSpawners, GameObject[] enemySpawners, GameObject[] rangedSpawners)
    {
        int currentDifficulty = 0;
        List<GameObject> availableSpawners = new List<GameObject>(allSpawners);

        while (currentDifficulty < roomDifficulty && availableSpawners.Count > 0)
        {
            // randomly select a spawner
            int spawnerIndex = Random.Range(0, availableSpawners.Count);
            GameObject spawner = availableSpawners[spawnerIndex];
            availableSpawners.RemoveAt(spawnerIndex);

            bool isRangedSpawner = System.Array.Exists(rangedSpawners, s => s == spawner);

            // filter valid enemies for this spawner
            List<EnemySpawnData> validEnemies = enemies.FindAll(e =>
                (!isRangedSpawner || e.isRanged));

            if (validEnemies.Count == 0) continue;

            // select random enemy
            EnemySpawnData selectedEnemy = validEnemies[Random.Range(0, validEnemies.Count)];

            // spawn enemy
            if (selectedEnemy.enemyPrefab != null)
            {
                Instantiate(selectedEnemy.enemyPrefab, spawner.transform.position, spawner.transform.rotation, transform);
                currentDifficulty += selectedEnemy.difficulty;
            }
        }

        Debug.Log($"Spawned enemies with total difficulty {currentDifficulty}/{roomDifficulty} in {gameObject.name}");
    }

    void SpawnRandom(List<GameObject> allSpawners, GameObject[] enemySpawners, GameObject[] rangedSpawners)
    {
        foreach (GameObject spawner in allSpawners)
        {
            bool isRangedSpawner = System.Array.Exists(rangedSpawners, s => s == spawner);

            // filter valid enemies
            List<EnemySpawnData> validEnemies = enemies.FindAll(e =>
                (!isRangedSpawner || e.isRanged));

            if (validEnemies.Count == 0) continue;

            // randomly decide to spawn (50% chance)
            if (Random.value > 0.5f)
            {
                EnemySpawnData selectedEnemy = validEnemies[Random.Range(0, validEnemies.Count)];

                if (selectedEnemy.enemyPrefab != null)
                {
                    Instantiate(selectedEnemy.enemyPrefab, spawner.transform.position, spawner.transform.rotation, transform);
                }
            }
        }
    }
}
