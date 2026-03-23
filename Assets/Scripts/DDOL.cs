using UnityEngine;

public class DDOL : MonoBehaviour
{
    private static DDOL instance;

    private void Awake()
    {
        // one instance per client
        if (instance != null && instance != this){Destroy(gameObject); return;}
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
