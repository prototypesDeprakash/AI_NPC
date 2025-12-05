using UnityEngine;

public class SimpleFollowCam : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    Vector3 offset;

    void Start()
    {
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}
