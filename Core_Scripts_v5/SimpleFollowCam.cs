using UnityEngine;

public class SimpleFollowCam : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public float mapBackwardDistance = 5f;

    Vector3 offset;

    // Map zoom settings
    public float mapHeightOffset = 20f;   // how high the camera should rise
    public float mapTransitionSpeed = 3f;

    bool goToMapView = false;
    Vector3 originalOffset;

    void Start()
    {
        offset = transform.position - target.position;
        originalOffset = offset; // store the normal offset
    }

    /* void LateUpdate()
     {
         if (target == null) return;

         // choose which offset to use
         Vector3 currentOffset = goToMapView
             ? new Vector3(originalOffset.x, mapHeightOffset, originalOffset.z)
             : originalOffset;

         // smooth transition of offset
         offset = Vector3.Lerp(offset, currentOffset, mapTransitionSpeed * Time.deltaTime);

         Vector3 desiredPos = target.position + offset;
         transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
     }
    */
    void LateUpdate()
    {
        if (target == null) return;

        // Determine the Z-component for the map view
        float newZOffset = originalOffset.z;

        if (goToMapView)
        {
            // Check the sign of the original Z-offset to maintain direction
            if (originalOffset.z > 0)
            {
                // If original Z is positive, move it further positive (backward)
                newZOffset += mapBackwardDistance;
            }
            else
            {
                // If original Z is negative, move it further negative (backward)
                newZOffset -= mapBackwardDistance;
            }
        }

        // choose which offset to use
        Vector3 currentOffset = goToMapView
            // The map view now uses the increased Z offset
            ? new Vector3(originalOffset.x, mapHeightOffset, newZOffset)
            : originalOffset;

        // smooth transition of offset
        offset = Vector3.Lerp(offset, currentOffset, mapTransitionSpeed * Time.deltaTime);

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    public void ZoomOutToMap()
    {
        goToMapView = true;
    }

    public void ResetCamera()
    {
        goToMapView = false;
    }
}
