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
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward, mainCameraTransform.rotation * Vector3.up);

        
    }
}