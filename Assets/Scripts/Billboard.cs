using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform cameraTransform;
    public float turnSpeed = 2f;
    public bool yAxisOnly = false;

    void Start()
    {
        cameraTransform = GameObject.FindWithTag("MainCamera").transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 toCamera = transform.position - cameraTransform.position;
        Vector3 horizontalDir = new Vector3(toCamera.x, 0, toCamera.z).normalized;

        Quaternion targetRotation;
        if (yAxisOnly)
        {
            targetRotation = Quaternion.LookRotation(horizontalDir);
        }
        else
        {
            float horizontalDist = Mathf.Sqrt(toCamera.x * toCamera.x + toCamera.z * toCamera.z);
            float verticalAngle = Mathf.Atan2(toCamera.y, horizontalDist) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Clamp(verticalAngle, -15f, 15f);
            targetRotation = Quaternion.LookRotation(horizontalDir) * Quaternion.Euler(-verticalAngle, 0, 0);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }
}
