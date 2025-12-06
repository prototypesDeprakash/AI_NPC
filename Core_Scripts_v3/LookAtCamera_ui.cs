using UnityEngine;

public class LookAtCamera_ui : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
           
            enabled = false; 
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null)
        {
            return;
        }

        // 1. Original code: Makes the object's local Z-axis point at the camera
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward, mainCameraTransform.rotation * Vector3.up);

        // 2. Apply a 180-degree rotation around the Y-axis to flip the UI to face forward
        transform.Rotate(0, 180, 0);
    }
}