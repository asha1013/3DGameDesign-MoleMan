using UnityEngine;

public class SpawnManager : MonoBehaviour
{
   public GameObject managerPrefab;
    void Awake()
    {
        if (GameManager.instance=null) Instantiate(managerPrefab);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
