using UnityEngine;

[CreateAssetMenu(fileName = "NewMushroom", menuName = "Scriptable Objects/Mushroom")]
public class Shroom : ScriptableObject
{
    public string mushroomName;
    public Item reagent;
    public Material growingMat;
    public Material grownMat;
    public int reqProgress;
    public Color color;
    public GameObject rewardPrefab;
}
