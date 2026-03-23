using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int damage;
    public float knockback;
    public bool hasHit = false;
    public Enemy enemyOwner;
    public bool playerAttack;
    private GameObject player;
    PlayerState playerState;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerState = player.GetComponent<PlayerState>();
        }
        else
        {
            Debug.LogError("Player not found");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasHit && !playerAttack && playerState != null)
        {
            playerState.GetHit(damage, false);
            hasHit = true;

            if (enemyOwner != null && enemyOwner.isAttacking && !playerAttack)
            {
                enemyOwner.OnLungeHit();
            }
        }
        else if (playerAttack && other.CompareTag("Enemy") && !hasHit) // Player attack
        {
            Debug.Log("enemy hit");
            hasHit = true;

            Enemy enemyScript = other.GetComponentInParent<Enemy>();
            if (enemyScript != null && player != null)
                enemyScript.GetHit(damage, knockback, player);
            else if (enemyScript == null)
                Debug.Log("No enemy script found");
        }
    }

    void OnDisable()
    {
        hasHit = false; // Reset when hitbox disables
    }
}


