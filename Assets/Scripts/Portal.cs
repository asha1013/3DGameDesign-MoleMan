using UnityEngine;

public class Portal : MonoBehaviour
{
    bool portalUsed;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&& GameManager.Instance.inGarden && !portalUsed)
        {
            SceneTransitionManager.Instance.LoadDungeonFromGarden();
            portalUsed=true;
        }

        else if (other.CompareTag("Player")&& GameManager.Instance.inDungeon && !portalUsed)
        {
           // SceneTransitionManager.Instance.LoadGardenFromDungeon();
            portalUsed=true;
        }
        
    }

}
