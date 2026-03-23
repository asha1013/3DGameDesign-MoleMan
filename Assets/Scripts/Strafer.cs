using System;
using UnityEngine;
using UnityEngine.AI;

public class Strafer : MonoBehaviour
{
    private GameObject player;
    private Camera playerCamera;
    private float range;
    private float backupRange;
    [UnityEngine.Range(0f, 1f)] public float backupDistance = .3f; // How much of an enemies range should they stay away from the player

    Enemy enemyScript;


    public bool isStrafe;
    public float strafeDistance = 3f;
    public bool isSneaky;
    public float sneakAngle = 110f;
    public Vector3 pathTarget;
    public bool shouldMove;

    private bool strafingRight;
    private bool strafeInitialized = false;
    private bool sneakRight; // Tracks which side the sneaky enemy committed to
    private bool sneakInitialized = false;
    private float lastRecalcTime;
    private float recalcInterval = 0.5f; // Recalculate path every 0.5 seconds
    private Vector3 lastPlayerPosition;
    private float playerMovementThreshold = 2f;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        enemyScript = GetComponent<Enemy>();
        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCam != null) playerCamera = mainCam.GetComponent<Camera>();
    }

    void Start()
    {
        if (enemyScript != null)
        {
            range = enemyScript.range;
            backupRange = range * backupDistance; // % of range to back away to
        }
        if (player != null) lastPlayerPosition = player.transform.position;
    }

    public void CalculatePathTarget(Vector3 playerPosition, float currentDistance)
    {
        // Only recalculate if enough time has passed or player moved significantly
        bool playerMoved = Vector3.Distance(lastPlayerPosition, playerPosition) > playerMovementThreshold;
        if (Time.time - lastRecalcTime < recalcInterval && !playerMoved)
            return;

        lastRecalcTime = Time.time;
        lastPlayerPosition = playerPosition;


        if (isSneaky)
        {
            CalculateSneakPosition(playerPosition, currentDistance);
        }
        else if (isStrafe)
        {
            // Strafe enemies keep strafing as long as they're within range
            if (currentDistance <= range)
            {
                CalculateStrafePosition(playerPosition, currentDistance);
            }
            else
            {
                // Too far, move closer using default behavior
                CalculateDefaultPosition(playerPosition, currentDistance);
            }
        }
        else
        {
            CalculateDefaultPosition(playerPosition, currentDistance); // Standard enemy behaviour
        }
    }

    void CalculateDefaultPosition(Vector3 playerPosition, float currentDistance)
    {
        // Too close - back up to backup range
        if (currentDistance < backupRange)
        {
            Vector3 awayFromPlayer = (transform.position - playerPosition).normalized;
            pathTarget = playerPosition + awayFromPlayer * backupRange;
            shouldMove = true;
        }
        // In range with LOS, don't move
        else if (currentDistance <= range-.05 && HasLineOfSight(transform.position, playerPosition))
        {
            shouldMove = false;
        }
        // No LOS or out of range - find position in range with LOS
        else
        {
            if (FindPositionInRangeWithLOS(playerPosition, out Vector3 targetPos))
            {
                pathTarget = targetPos;
                shouldMove = true;
            }
            else
            {
                // Fallback: move closer to player
                pathTarget = playerPosition;
                shouldMove = true;
            }
        }
    }

    void CalculateStrafePosition(Vector3 playerPosition, float currentDistance)
    {
        // Calculate perpendicular direction to player
        Vector3 toPlayer = (playerPosition - transform.position).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, toPlayer).normalized;

        // Calculate strafe points at range distance, perpendicular to player
        float idealDist = range * 0.75f;
        Vector3 towardsPlayer = toPlayer * idealDist;

        Vector3 rightStrafePoint = playerPosition - towardsPlayer + rightDir * strafeDistance;
        Vector3 leftStrafePoint = playerPosition - towardsPlayer - rightDir * strafeDistance;

        // Initialize strafe direction if not yet done
        if (!strafeInitialized)
        {
            bool rightValid = FindNearestValidPosition(rightStrafePoint, out _);
            bool leftValid = FindNearestValidPosition(leftStrafePoint, out _);

            if (rightValid && leftValid)
            {
                strafingRight = UnityEngine.Random.value > 0.5f;
            }
            else if (rightValid)
            {
                strafingRight = true;
            }
            else if (leftValid)
            {
                strafingRight = false;
            }
            else
            {
                // Neither strafe point viable, stay still
                shouldMove = false;
                return;
            }
            strafeInitialized = true;
        }

        // Choose target based on current strafe direction
        Vector3 targetStrafePoint = strafingRight ? rightStrafePoint : leftStrafePoint;

        // Check if we've reached current strafe target, if so switch direction
        if (pathTarget != Vector3.zero && Vector3.Distance(transform.position, pathTarget) < 1f)
        {
            strafingRight = !strafingRight;
            targetStrafePoint = strafingRight ? rightStrafePoint : leftStrafePoint;
        }

        // Try to path to the target strafe point with LOS
        if (FindNearestValidPosition(targetStrafePoint, out Vector3 validPos) && HasLineOfSight(validPos, playerPosition))
        {
            pathTarget = validPos;
            shouldMove = true;
        }
        else
        {
            // Can't reach current strafe point with LOS, try opposite
            strafingRight = !strafingRight;
            targetStrafePoint = strafingRight ? rightStrafePoint : leftStrafePoint;

            if (FindNearestValidPosition(targetStrafePoint, out Vector3 altValidPos) && HasLineOfSight(altValidPos, playerPosition))
            {
                pathTarget = altValidPos;
                shouldMove = true;
            }
            else
            {
                // Can't strafe either direction with LOS, use default behavior
                CalculateDefaultPosition(playerPosition, currentDistance);
            }
        }
    }

    void CalculateSneakPosition(Vector3 playerPosition, float currentDistance)
    {
        if (playerCamera == null)
        {
            CalculateDefaultPosition(playerPosition, currentDistance);
            return;
        }

        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        // Initialize which side to sneak on (only once)
        if (!sneakInitialized)
        {
            Vector3 toEnemyInit = (transform.position - playerPosition).normalized;
            toEnemyInit.y = 0;

            // Calculate current angle from player's view
            float currentAngle = Vector3.SignedAngle(cameraForward, toEnemyInit, Vector3.up);

            // Determine which side is better to sneak to based on current position
            bool preferRight = Mathf.Abs(currentAngle) < 90f ? currentAngle < 0 : currentAngle > 0;

            // Check both sides to see which are viable
            float rightAngle = sneakAngle;
            Quaternion rightRotation = Quaternion.Euler(0, rightAngle, 0);
            Vector3 rightOffset = rightRotation * cameraForward * range;
            Vector3 rightTarget = playerPosition + rightOffset;

            float leftAngle = -sneakAngle;
            Quaternion leftRotation = Quaternion.Euler(0, leftAngle, 0);
            Vector3 leftOffset = leftRotation * cameraForward * range;
            Vector3 leftTarget = playerPosition + leftOffset;

            bool rightValid = FindNearestValidPosition(rightTarget, out _);
            bool leftValid = FindNearestValidPosition(leftTarget, out _);

            // Choose side: if both viable, use random; if only one viable, use that; if preferred viable, use that
            if (rightValid && leftValid)
            {
                sneakRight = UnityEngine.Random.value > 0.5f;
            }
            else if (rightValid)
            {
                sneakRight = true;
            }
            else if (leftValid)
            {
                sneakRight = false;
            }
            else
            {
                // Neither flank angle is viable, pick preferred side anyway and we'll compromise
                sneakRight = preferRight;
            }

            sneakInitialized = true;
        }

        // Now stick to the chosen side
        float targetAngle = sneakRight ? sneakAngle : -sneakAngle;

        // Use 75% of range as ideal distance
        float idealDist = range * 0.75f;

        // Calculate current angle from player's camera view
        Vector3 toEnemy = (transform.position - playerPosition).normalized;
        toEnemy.y = 0;
        float currentViewAngle = Mathf.Abs(Vector3.SignedAngle(cameraForward, toEnemy, Vector3.up));

        // If already at least sneakAngle away from player's view and in range with LOS, stay put
        if (currentViewAngle >= sneakAngle && currentDistance <= range && HasLineOfSight(transform.position, playerPosition))
        {
            shouldMove = false;
            return;
        }

        // Try to position at sneakAngle on chosen side
        Quaternion rotation = Quaternion.Euler(0, targetAngle, 0);
        Vector3 offset = rotation * cameraForward * idealDist;
        Vector3 sneakTarget = playerPosition + offset;

        if (FindNearestValidPosition(sneakTarget, out Vector3 validPos) && HasLineOfSight(validPos, playerPosition))
        {
            pathTarget = validPos;
            shouldMove = true;
            return;
        }

        // Try different distances at the same angle
        float[] distanceVariations = new float[] { range * 0.5f, range };

        foreach (float dist in distanceVariations)
        {
            Quaternion distRotation = Quaternion.Euler(0, targetAngle, 0);
            Vector3 distOffset = distRotation * cameraForward * dist;
            Vector3 distTarget = playerPosition + distOffset;

            if (FindNearestValidPosition(distTarget, out Vector3 distPos) && HasLineOfSight(distPos, playerPosition))
            {
                pathTarget = distPos;
                shouldMove = true;
                return;
            }
        }

        // Can't find ideal position, use default behavior
        CalculateDefaultPosition(playerPosition, currentDistance);
    }

    bool FindPositionInRangeWithLOS(Vector3 playerPosition, out Vector3 targetPosition)
    {
        // Sample positions around player at range distance in 8 directions
        int sampleCount = 8;
        float angleStep = 360f / sampleCount;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = angleStep * i;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 offset = rotation * Vector3.forward * range;
            Vector3 samplePos = playerPosition + offset;

            if (FindNearestValidPosition(samplePos, out Vector3 validPos) && HasLineOfSight(validPos, playerPosition))
            {
                targetPosition = validPos;
                return true;
            }
        }

        targetPosition = Vector3.zero;
        return false;
    }

    bool FindNearestValidPosition(Vector3 targetPos, out Vector3 validPosition)
    {
        // Find nearest point on NavMesh
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            validPosition = hit.position;
            return true;
        }

        validPosition = Vector3.zero;
        return false;
    }

    bool HasLineOfSight(Vector3 fromPos, Vector3 toPos)
    {
        Vector3 direction = toPos - fromPos;
        float distance = direction.magnitude;

        if (Physics.Raycast(fromPos + Vector3.up * 0.5f, direction.normalized, out RaycastHit hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        return true;
    }

    public void ResetStrafeDirection()
    {
        strafeInitialized = false;
    }
}
