using UnityEngine;

public class PlayerStab : MonoBehaviour
{
    [SerializeField] float stabRange = 1.8f;        // how far you can stab
    [SerializeField] float coneAngle = 60f;         // 60° forward cone
    [SerializeField] KeyCode stabKey = KeyCode.E;
    [SerializeField] LayerMask npcLayer;            // NPC layer

    private void Update()
    {
        if (Input.GetKeyDown(stabKey))
        {
            TryStab();
        }
    }

    public void TryStab()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stabRange, npcLayer);

        NPCWander bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var h in hits)
        {
            Vector3 targetPos = h.transform.position;
            Vector3 dir = (targetPos - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > coneAngle * 0.5f)
                continue;

            float dist = Vector3.Distance(transform.position, targetPos);

            // prevent stabbing through walls
            if (Physics.Raycast(transform.position + Vector3.up * 1f, dir, out RaycastHit hit, dist))
            {
                // if ray hits something that is NOT the NPC → blocked
                if (hit.collider.transform != h.transform &&
                    hit.collider.transform != h.transform.parent)
                    continue;
            }

            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = h.GetComponentInParent<NPCWander>();
            }
        }

        if (bestTarget != null)
        {
            bestTarget.Die(transform);
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Draw the sphere radius
        Gizmos.DrawWireSphere(transform.position, stabRange);

        // Draw cone directions
        Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir * stabRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * stabRange);
    }
}
