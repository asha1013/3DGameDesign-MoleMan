#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

//[ExecuteInEditMode] // Temporarily disabled to test wizard conflict
public class EnemyVariableSetter : MonoBehaviour
{
    public void CreateMissingComponents()
    {
        Enemy enemyScript = GetComponent<Enemy>();
        if (enemyScript == null)
        {
            Debug.Log("Enemy script not found.");
            return;
        }

        if (enemyScript.attacks == null || enemyScript.attacks.Length == 0)
        {
            Debug.Log("Enemy has no attacks.");
            return;
        }

        EnemyAttack attack = enemyScript.attacks[0];
        string enemyName = enemyScript.enemyName;

        // Find or create body transform
        Transform bodyTransform = transform.Find(enemyName + "Body");
        if (bodyTransform == null)
        {
            GameObject bodyObject = new GameObject(enemyName + "Body");
            bodyObject.transform.SetParent(transform);
            bodyObject.transform.localPosition = Vector3.zero;
            bodyTransform = bodyObject.transform;
            Debug.Log(enemyName + "Body not found, creating in prefab");
        }

        // Create idle child if missing
        Transform idleTransform = bodyTransform.Find(enemyName + "Idle");
        if (idleTransform == null)
        {
            GameObject idleObject = new GameObject(enemyName + "Idle");
            idleObject.transform.SetParent(bodyTransform);
            idleObject.transform.localPosition = Vector3.zero;
            idleObject.AddComponent<MeshRenderer>();
            idleObject.AddComponent<MeshFilter>();
            idleTransform = idleObject.transform;
            Debug.Log(enemyName + "Idle not found, creating in prefab");
        }

        // Create move child if missing
        Transform moveTransform = bodyTransform.Find(enemyName + "Move");
        if (moveTransform == null)
        {
            GameObject moveObject = new GameObject(enemyName + "Move");
            moveObject.transform.SetParent(bodyTransform);
            moveObject.transform.localPosition = Vector3.zero;
            moveObject.AddComponent<MeshRenderer>();
            moveObject.AddComponent<MeshFilter>();
            moveTransform = moveObject.transform;
            Debug.Log(enemyName + "Move not found, creating in prefab");
        }

        // Create windup child if missing
        Transform windupTransform = bodyTransform.Find(enemyName + "Windup");
        if (windupTransform == null)
        {
            GameObject windupObject = new GameObject(enemyName + "Windup");
            windupObject.transform.SetParent(bodyTransform);
            windupObject.transform.localPosition = Vector3.zero;
            windupObject.AddComponent<MeshRenderer>();
            windupObject.AddComponent<MeshFilter>();
            windupTransform = windupObject.transform;
            Debug.Log(enemyName + "Windup not found, creating in prefab");
        }

        // Create GlowPart if missing
        if (windupTransform != null && windupTransform.Find("GlowPart") == null)
        {
            GameObject glowPart = new GameObject("GlowPart");
            glowPart.transform.SetParent(windupTransform);
            glowPart.transform.localPosition = Vector3.zero;
            glowPart.AddComponent<MeshRenderer>();
            glowPart.AddComponent<MeshFilter>();
            Debug.Log("GlowPart not found, creating in prefab");
        }

        // Create attack child if missing
        Transform attackTransform = bodyTransform.Find(enemyName + "Attack");
        if (attackTransform == null)
        {
            GameObject attackObject = new GameObject(enemyName + "Attack");
            attackObject.transform.SetParent(bodyTransform);
            attackObject.transform.localPosition = Vector3.zero;
            attackObject.AddComponent<MeshRenderer>();
            attackObject.AddComponent<MeshFilter>();
            attackTransform = attackObject.transform;
            Debug.Log(enemyName + "Attack not found, creating in prefab");
        }

        // Add Rigidbody for lunging attacks with jump height
        if (attack.attackType == EnemyAttack.AttackType.Lunging && enemyScript.jumpHeight > 0)
        {
            if (attackTransform.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = attackTransform.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = true;
                Debug.Log("Rigidbody not found on attack child, creating for lunging attack");
            }
        }

        // Create hitbox for melee/lunging attacks
        if (attack.attackType == EnemyAttack.AttackType.Melee || attack.attackType == EnemyAttack.AttackType.Lunging)
        {
            Transform hitboxTransform = attackTransform.Find("Hitbox");
            if (hitboxTransform == null)
            {
                GameObject hitboxObject = new GameObject("Hitbox");
                hitboxObject.transform.SetParent(attackTransform);
                hitboxObject.transform.localPosition = Vector3.zero;
                BoxCollider hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
                hitboxCollider.isTrigger = true;
                hitboxCollider.size = Vector3.one;
                hitboxObject.AddComponent<AttackHitbox>();
                Debug.Log("Hitbox not found, creating in prefab");
            }
            else
            {
                // Ensure it has collider and script
                if (hitboxTransform.GetComponent<Collider>() == null)
                {
                    BoxCollider hitboxCollider = hitboxTransform.gameObject.AddComponent<BoxCollider>();
                    hitboxCollider.isTrigger = true;
                    hitboxCollider.size = Vector3.one;
                    Debug.Log("Hitbox collider not found, creating in prefab");
                }
                if (hitboxTransform.GetComponent<AttackHitbox>() == null)
                {
                    hitboxTransform.gameObject.AddComponent<AttackHitbox>();
                    Debug.Log("AttackHitbox script not found, creating in prefab");
                }
            }
        }

        // Projectile-specific components
        if (attack.attackType == EnemyAttack.AttackType.Projectile)
        {
            // Create ProjectileOrigin if missing
            Transform originTransform = attackTransform.Find("ProjectileOrigin");
            if (originTransform == null)
            {
                GameObject originObject = new GameObject("ProjectileOrigin");
                originObject.transform.SetParent(attackTransform);
                originObject.transform.localPosition = Vector3.forward;
                Debug.Log("ProjectileOrigin not found, creating in prefab");
            }
        
        }

        Debug.Log("Finished creating missing components for " + enemyName);
    }

    public void SetVariables()
    {
        Enemy enemyScript = GetComponent<Enemy>();
        if (enemyScript == null)
        {
            Debug.Log("Enemy script not found.");
            return;
        }

        if (enemyScript.attacks == null || enemyScript.attacks.Length == 0)
        {
            Debug.Log("Enemy has no attacks.");
            return;
        }

        EnemyAttack attack = enemyScript.attacks[0];
        string enemyName = enemyScript.enemyName;

        GameObject idleChild = null;
        GameObject moveChild = null;
        GameObject windupChild = null;
        GameObject attackChild = null;

        // Find state children
        Transform bodyTransform = transform.Find(enemyName + "Body");

        if (bodyTransform != null)
        {
            Transform idleTransform = bodyTransform.Find(enemyName + "Idle");
            if (idleTransform != null)
            {
                idleChild = idleTransform.gameObject;
                enemyScript.GetType().GetField("idleChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, idleChild);
            }
            else Debug.Log("Idle Child not found.");

            Transform moveTransform = bodyTransform.Find(enemyName + "Move");
            if (moveTransform != null)
            {
                moveChild = moveTransform.gameObject;
                enemyScript.GetType().GetField("moveChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, moveChild);
            }
            else Debug.Log("Move Child not found.");

            Transform windupTransform = bodyTransform.Find(enemyName + "Windup");
            if (windupTransform != null)
            {
                windupChild = windupTransform.gameObject;
                enemyScript.GetType().GetField("windupChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, windupChild);
            }

            Transform attackTransform = bodyTransform.Find(enemyName + "Attack");
            if (attackTransform != null)
            {
                attackChild = attackTransform.gameObject;
                enemyScript.GetType().GetField("attackChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, attackChild);
            }
            else Debug.Log("Attack Child not found.");
        }

        // Find idle material - needed for both child-based and material-swap enemies
        Material idleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Idle.mat");
        if (idleMaterial != null)
            enemyScript.GetType().GetField("idleMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, idleMaterial);
        else if (idleChild != null)
        {
            // If material not found but child exists, get material from child's renderer
            MeshRenderer renderer = idleChild.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
                enemyScript.GetType().GetField("idleMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, renderer.sharedMaterial);
        }
        else Debug.Log("Idle Material not found at Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Idle.mat");

        if (moveChild == null)
        {
            Material moveMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Move.mat");
            if (moveMaterial != null)
                enemyScript.GetType().GetField("moveMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, moveMaterial);
            else Debug.Log("Move Material not found at Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Move.mat");
        }
        else
        {
            // moveChild exists, so no material needed
        }

        if (windupChild == null)
        {
            Material windupMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Windup.mat");
            if (windupMaterial != null)
                enemyScript.GetType().GetField("windupMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, windupMaterial);
            else Debug.Log("Windup Material not found at Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Windup.mat");
        }
        else
        {
            // windupChild exists, so no material needed
        }

        if (attackChild == null)
        {
            Material attackMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Attack.mat");
            if (attackMaterial != null)
                enemyScript.GetType().GetField("attackMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, attackMaterial);
            else Debug.Log("Attack Material not found at Assets/Prefabs/Char/" + enemyName + "/Materials/" + enemyName + "Attack.mat");
        }
        else
        {
            // attackChild exists, so no material needed
        }

        // Find flash renderer
        Transform glowPart = null;
        if (windupChild != null)
        {
            glowPart = windupChild.transform.Find("GlowPart");
        }

        if (glowPart == null && attackChild != null)
        {
            glowPart = attackChild.transform.Find("GlowPart");
        }

        if (glowPart == null && idleChild != null)
        {
            glowPart = idleChild.transform.Find("GlowPart");
        }

        if (glowPart != null)
        { 
            MeshRenderer flashRenderer = glowPart.GetComponent<MeshRenderer>();
            if (flashRenderer != null)
                enemyScript.GetType().GetField("flashRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, flashRenderer);
            else Debug.Log("Flash renderer not found.");
        }

        // Find audio clips (check both .wav and .ogg)
        AudioClip agroClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Agro");
        if (agroClip != null)
            enemyScript.GetType().GetField("agroClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, agroClip);

        AudioClip dieClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Die");
        if (dieClip != null)
            enemyScript.GetType().GetField("dieClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, dieClip);

        AudioClip moveClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Move");
        if (moveClip != null)
            enemyScript.GetType().GetField("moveClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, moveClip);

        AudioClip damagedClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Damaged");
        if (damagedClip != null)
            enemyScript.GetType().GetField("damagedClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, damagedClip);

        AudioClip windupClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Windup");
        if (windupClip != null)
            enemyScript.GetType().GetField("windupClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, windupClip);

        AudioClip attackClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "Attack");
        if (attackClip != null)
            enemyScript.GetType().GetField("attackClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, attackClip);

        // Attack hit clip for melee/lunging
        if (attack.attackType == EnemyAttack.AttackType.Melee || attack.attackType == EnemyAttack.AttackType.Lunging)
        {
            AudioClip attackHitClip = LoadAudioClip("Assets/Prefabs/Char/" + enemyName + "/SFX/" + enemyName + "AttackHit");
            if (attackHitClip != null)
                enemyScript.GetType().GetField("attackHitClip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, attackHitClip);
        }
        
        // Projectile-specific variables
        if (attack.attackType == EnemyAttack.AttackType.Projectile)
        {
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Char/" + enemyName + "/" + enemyName + "Projectile.prefab");
            if (projectilePrefab != null)
            {
                enemyScript.GetType().GetField("projectilePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, projectilePrefab);
            }
            else Debug.Log("Projectile Prefab not found at Assets/Prefabs/Char/" + enemyName + "/" + enemyName + "Projectile.prefab");

            Transform originTransform = null;
            if (attackChild != null)
            {
                originTransform = attackChild.transform.Find("ProjectileOrigin");
            }

            if (originTransform == null && idleChild != null)
            {
                originTransform = idleChild.transform.Find("ProjectileOrigin");
            }

            if (originTransform != null)
            {
                enemyScript.GetType().GetField("projectileOrigin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, originTransform.gameObject);
            }
            else
            {
                Debug.Log("Projectile Origin not found.");
            }
        }

        // Find hitbox object (for melee/lunging attacks)
        if (attack.attackType == EnemyAttack.AttackType.Melee || attack.attackType == EnemyAttack.AttackType.Lunging)
        {
            GameObject hitboxObject = null;

            // Check attackChild first - prioritize Hitbox child, then attackChild itself
            if (attackChild != null)
            {
                Transform hitboxTransform = attackChild.transform.Find("Hitbox");
                if (hitboxTransform != null)
                {
                    hitboxObject = hitboxTransform.gameObject;
                }
                else
                {
                    Collider col = attackChild.GetComponent<Collider>();
                    if (col != null && col.isTrigger && attackChild.GetComponent<AttackHitbox>() != null)
                    {
                        hitboxObject = attackChild;
                    }
                }
            }

            // Check idleChild if not found - prioritize Hitbox child, then idleChild itself
            if (hitboxObject == null && idleChild != null)
            {
                Transform hitboxTransform = idleChild.transform.Find("Hitbox");
                if (hitboxTransform != null)
                {
                    hitboxObject = hitboxTransform.gameObject;
                }
                else
                {
                    Collider col = idleChild.GetComponent<Collider>();
                    if (col != null && col.isTrigger && idleChild.GetComponent<AttackHitbox>() != null)
                    {
                        hitboxObject = idleChild;
                    }
                }
            }

            if (hitboxObject != null)
            {
                enemyScript.GetType().GetField("hitboxObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(enemyScript, hitboxObject);
            }
            else
            {
                Debug.Log("Hitbox object not found.");
            }
        }

        // Mark the Enemy component as dirty to ensure changes are saved
        EditorUtility.SetDirty(enemyScript);

        Debug.Log($"Variables set for {enemyName}. Make sure to save the prefab!");
    }

    // Helper method to load audio clips with both .wav and .ogg extensions
    private AudioClip LoadAudioClip(string basePath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + ".wav");
        if (clip == null)
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(basePath + ".ogg");
        return clip;
    }
}
#endif

