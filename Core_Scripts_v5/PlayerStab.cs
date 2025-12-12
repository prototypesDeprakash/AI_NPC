using UnityEngine;
using UnityEngine.Rendering.Universal;


public class PlayerStab : MonoBehaviour
{
    [SerializeField] float stabRange = 1.8f;        // how far you can stab
    [SerializeField] float coneAngle = 60f;         // 60° forward cone
    [SerializeField] KeyCode stabKey = KeyCode.E;
    [SerializeField] LayerMask npcLayer;            // NPC layer (for OverlapSphere)
    [SerializeField] LayerMask obstacleLayer;       // Layers that block stabbing (walls/props)
    [SerializeField] Transform stabOrigin;          // optional, e.g. the player's chest/hand transform
                                                    // Improved stab routine: more reliable, uses SphereCastAll forward and LOS checks
    public float sphereCastRadius = 0.35f;        // small radius to simulate the blade area
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    public float angleToleranceExtra = 3f;        // small extra forgiveness on angle checks

    //effect


    LensDistortion ld;
    private void Update()
    {
        if (Input.GetKeyDown(stabKey))
        {
            TryStab();
        }
    }
    /*
        public void TryStab()
        {
            Vector3 origin = stabOrigin != null ? stabOrigin.position : transform.position + Vector3.up * 1f;

            // find all NPC colliders in range (only NPC layer)
            Collider[] hits = Physics.OverlapSphere(origin, stabRange, npcLayer);

            NPCWander bestTarget = null;
            float closestDist = Mathf.Infinity;

            foreach (var h in hits)
            {
                // find the NPC root
                NPCWander npc = h.GetComponentInParent<NPCWander>();
                if (npc == null) continue;

                // ignore self (in case player has NPC layer set accidentally)
                if (npc.transform == transform) continue;

                Vector3 targetPos = h.transform.position;
                Vector3 dir = (targetPos - origin);
                float dist = dir.magnitude;
                dir.Normalize();

                // cone check
                float angle = Vector3.Angle(transform.forward, dir);
                if (angle > coneAngle * 0.5f)
                    continue;

                // IMPORTANT: raycast *only* against obstacleLayer.
                // If it hits something, stabbing is blocked.
                // If it hits nothing in obstacleLayer, it's clear.
                if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleLayer))
                {
                    // hit an obstacle before reaching the NPC
                    continue;
                }

                // At this point we didn't hit any blocking obstacle.
                // As an extra safety, ensure the hit collider actually belongs to the NPC or its child (OverlapSphere sometimes returns weird colliders)
                // We can still accept this npc as valid.
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = npc;
                }
            }

            if (bestTarget != null)
            {
                bestTarget.Die(transform);
            }
        }
    */
    public void TryStab()
    {
        Vector3 origin = stabOrigin != null ? stabOrigin.position : transform.position + Vector3.up * 1f;
        Vector3 fwd = transform.forward;

        NPCWander bestTarget = null;
        float bestDist = Mathf.Infinity;

        // 1) Directed check: spherecast forward to get hits in front of the player.
        RaycastHit[] sphereHits = Physics.SphereCastAll(origin, sphereCastRadius, fwd, stabRange, npcLayer, triggerInteraction);
        for (int i = 0; i < sphereHits.Length; i++)
        {
            var hit = sphereHits[i];
            NPCWander npc = hit.collider.GetComponentInParent<NPCWander>();
            if (npc == null) continue;
            if (npc.transform == transform) continue; // just in case

            Vector3 toHit = hit.point - origin;
            float dist = toHit.magnitude;
            Vector3 dir = toHit.normalized;

            // cone check with small extra tolerance
            float angle = Vector3.Angle(fwd, dir);
            if (angle > (coneAngle * 0.5f + angleToleranceExtra))
                continue;

            // line-of-sight: raycast against obstacleLayer
            if (Physics.Raycast(origin, dir, out RaycastHit block, dist, obstacleLayer))
            {
                // something blocking before reaching the target
                continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = npc;
            }
        }

        // 2) Fallback: if spherecast found nothing, try a short OverlapSphere (useful for targets slightly off-forward)
        if (bestTarget == null)
        {
            Collider[] hits = Physics.OverlapSphere(origin, stabRange, npcLayer, triggerInteraction);
            foreach (var h in hits)
            {
                NPCWander npc = h.GetComponentInParent<NPCWander>();
                if (npc == null) continue;
                if (npc.transform == transform) continue;

                // choose a reasonable point on the collider to aim at (closest point)
                Vector3 closestPoint = h.ClosestPoint(origin);
                Vector3 toPoint = closestPoint - origin;
                float dist = toPoint.magnitude;
                Vector3 dir = dist > 0.0001f ? toPoint.normalized : fwd;

                float angle = Vector3.Angle(fwd, dir);
                if (angle > (coneAngle * 0.5f + angleToleranceExtra))
                    continue;

                if (Physics.Raycast(origin, dir, out RaycastHit block, dist, obstacleLayer))
                {
                    continue;
                }

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = npc;
                }
            }
        }

        if (bestTarget != null)
        {
            bestTarget.Die(transform);
        }
        else
        {
            // Optional: debug log or FX when miss
            // Debug.Log("Stab missed");
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 origin = stabOrigin != null ? stabOrigin.position : transform.position + Vector3.up * 1f;

        // Draw cone directions
        Vector3 leftDir = Quaternion.Euler(0, -coneAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, coneAngle * 0.5f, 0) * transform.forward;

        Gizmos.DrawLine(origin, origin + leftDir * stabRange);
        Gizmos.DrawLine(origin, origin + rightDir * stabRange);
        Gizmos.DrawWireSphere(origin, stabRange);
    }
}
