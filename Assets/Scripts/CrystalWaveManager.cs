using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrystalWaveManager : MonoBehaviour
{
    private GameObject[] projectilePrefabs;
    private float startRadius;
    private float radiusDist;
    private int baseDensity;
    private float waveStep;
    private float waveSpeed;
    private AudioClip waveClip;
    private float waveClipVolume;

    private float currentRadius;
    private bool waveActive = true;
    private List<CrystalWaveProjectile> activeProjectiles = new List<CrystalWaveProjectile>();
    private bool alternateOffset = false;
    private AudioSource audioSource;

    public void Initialize(GameObject[] prefabs, float radius, float radDist, int density, float step, float speed, AudioClip clip, float volume)
    {
        projectilePrefabs = prefabs;
        startRadius = radius;
        radiusDist = radDist;
        baseDensity = density;
        waveStep = step;
        waveSpeed = speed;
        waveClip = clip;
        waveClipVolume = volume;

        currentRadius = startRadius;

        // Add audio source
        audioSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(WaveExpansion());
    }

    IEnumerator WaveExpansion()
    {
        while (waveActive)
        {
            // Play wave sound
            if (waveClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(waveClip, waveClipVolume);
            }

            // Disable previous projectiles
            foreach (CrystalWaveProjectile proj in activeProjectiles)
            {
                if (proj != null) proj.gameObject.SetActive(false);
            }
            activeProjectiles.Clear();

            // Calculate new projectile count based on radius scaling
            int currentDensity = Mathf.RoundToInt(baseDensity * (currentRadius / startRadius));
            if (currentDensity < baseDensity) currentDensity = baseDensity;

            // Spawn 3 rings with alternating positions
            float offset = alternateOffset ? (180f / currentDensity) : 0f;

            for (int ring = 0; ring < 3; ring++)
            {
                float ringRadius = currentRadius - (ring * radiusDist);
                float ringOffset = offset + (ring * (180f / currentDensity) / 3f); // Slight offset per ring

                for (int i = 0; i < currentDensity; i++)
                {
                    float angle = (360f / currentDensity) * i + ringOffset;
                    Quaternion rotation = Quaternion.Euler(0, angle, 0);
                    Vector3 direction = rotation * Vector3.forward;
                    Vector3 spawnPos = transform.position + direction * ringRadius;
                    spawnPos.y = transform.position.y + 0.1f; // Slightly above ground

                    // Random prefab selection
                    GameObject selectedPrefab = projectilePrefabs[Random.Range(0, projectilePrefabs.Length)];

                    // Add random rotation variation (±15 degrees on each axis)
                    float randomX = Random.Range(-15f, 15f);
                    float randomY = Random.Range(-15f, 15f);
                    float randomZ = Random.Range(-15f, 15f);
                    Quaternion randomRotation = rotation * Quaternion.Euler(randomX, randomY, randomZ);

                    GameObject projectile = Instantiate(selectedPrefab, spawnPos, randomRotation);
                    CrystalWaveProjectile projScript = projectile.GetComponent<CrystalWaveProjectile>();
                    if (projScript != null)
                    {
                        projScript.SetManager(this);
                        activeProjectiles.Add(projScript);
                    }
                }
            }

            alternateOffset = !alternateOffset;
            currentRadius += waveStep;

            yield return new WaitForSeconds(waveSpeed);
        }

        // Wave stopped, wait 1 second then destroy
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    public void OnWallHit()
    {
        waveActive = false;
    }
}
