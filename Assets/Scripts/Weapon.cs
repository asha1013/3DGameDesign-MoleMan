using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/Weapon")]
public class Weapon : ScriptableObject
{
    public enum WeaponType { Ranged, Melee, Both }

    [BoxGroup("Visual")] public GameObject weaponObj;
    [BoxGroup("Visual")] public string weaponName;

    [BoxGroup("Attack Type")] public WeaponType attackType = WeaponType.Ranged;
    [BoxGroup("Attack Type")] [ShowIf("HasRanged")] public bool uniqueRanged = false;
    [BoxGroup("Attack Type")] [ShowIf("HasMelee")] public bool uniqueMelee = false;
    [BoxGroup("Attack Type")] [ShowIf("HasRanged")] public bool hasChargeAttack = false;
    [BoxGroup("Attack Type")] [ShowIf("HasRanged")] public bool hasAimAttack = false;

    [BoxGroup("Projectile")] [ShowIf("HasRanged")] public GameObject projectilePrefab;
    [BoxGroup("Projectile")] [ShowIf("HasRanged")] public bool overrideProjectileOrigin = false;
    [BoxGroup("Projectile")] [ShowIf("overrideProjectileOrigin")] public Vector3 projectileOriginLocalPosition;

    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public float fireRate = 0.5f;
    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public int baseDamage = 1;
    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public bool fireEmit = false;
    [BoxGroup("Fire Stats")] [ShowIf("fireEmit")] public Material[] fireMaterials;
    [BoxGroup("Fire Stats")] [ShowIf("fireEmit")] public float emitIntensity = 5f;
    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public float baseRangedStartup = 0.12f;
    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public float baseRangedTime = 0.57f;
    [BoxGroup("Fire Stats")] [ShowIf("HasRanged")] public bool hasShootSFX = false;
    [BoxGroup("Fire Stats")] [ShowIf("hasShootSFX")] public AudioClip shootClip;
    [BoxGroup("Fire Stats")] [ShowIf("hasShootSFX")] [Range(0f, 1f)] public float shootClipVolume = 1f;

    [BoxGroup("Melee Stats")] [ShowIf("HasMelee")] public int meleeDamage = 2;
    [BoxGroup("Melee Stats")] [ShowIf("HasMelee")] public float meleeSpeed = 0.64f;
    [BoxGroup("Melee Stats")] [ShowIf("HasMelee")] public float baseMeleeStartup = 0.27f;
    [BoxGroup("Melee Stats")] [ShowIf("HasMelee")] public float baseMeleeActive = 0.13f;
    [BoxGroup("Melee Stats")] [ShowIf("HasMelee")] public float baseMeleeTime = 0.64f;

    [BoxGroup("Projectile Stats")] [ShowIf("HasRanged")] public float projectileSpeed = 10f;
    [BoxGroup("Projectile Stats")] [ShowIf("ShowProjectileRange")] [Range(0.8f, 1f)] public float projectileRange = 1f;


    [BoxGroup("Optional Properties")] [ShowIf("HasRanged")] public float knockback = 5f;
    [BoxGroup("Optional Properties")] [ShowIf("HasRanged")] public bool travelArc = false;
    [BoxGroup("Optional Properties")] [ShowIf("travelArc")] public float arcHeight = 2f;
    [BoxGroup("Optional Properties")] [ShowIf("travelArc")] public float arcRange = 10f;
    [BoxGroup("Optional Properties")] [ShowIf("HasRanged")] public int penetration = 0;
    [BoxGroup("Optional Properties")] [ShowIf("HasRanged")] public bool isAOE = false;
    [BoxGroup("Optional Properties")] [ShowIf("isAOE")] public float splashRadius = 2f;

    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] public float chargeTime = 2f;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] [MinMaxSlider(0, 100)] public Vector2 damageRange = new Vector2(10, 50);
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] [MinMaxSlider(0.9f, 1f)] public Vector2 rangeRange = new Vector2(0.8f, 1f);
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] [MinMaxSlider(1, 50)] public Vector2 speedRange = new Vector2(5, 25); 
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] public float chargeEmit = 5f;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] public float chargedFlash = 10f;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] public AudioClip chargingClip;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] [Range(0f, 1f)] public float chargingClipVolume = 1f;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] public AudioClip chargedLoop;
    [BoxGroup("Charge Attack")] [ShowIf("hasChargeAttack")] [Range(0f, 1f)] public float chargedLoopVolume = 1f;

    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float startTime = 0.5f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float aimWeight = 1f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float minPower = 5f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float maxPower = 20f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float powerRampTime = 2f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public Material aimMat;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public float aimEmit = 5f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public AudioClip weaponAimClip;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] [Range(0f, 1f)] public float weaponAimClipVolume = 1f;
    [BoxGroup("Aim Attack")] [ShowIf("hasAimAttack")] public GameObject aimObject;

    bool HasRanged() => attackType == WeaponType.Ranged || attackType == WeaponType.Both;
    bool HasMelee() => attackType == WeaponType.Melee || attackType == WeaponType.Both;
    bool ShowProjectileRange() => HasRanged() && !travelArc;
}
