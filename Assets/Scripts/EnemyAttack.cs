using UnityEngine;

[System.Serializable]
public class EnemyAttack
{
    public enum AttackType
    { Melee, Projectile, Lunging, Unique }

    public AttackType attackType;
}


