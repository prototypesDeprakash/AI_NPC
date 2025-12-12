using mygame;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(NPCWander))]
public class NPCVision : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float viewDistance = 12f;
    [SerializeField] private float viewAngle = 90f;

    [Tooltip("Layer where all NPCs live")]
    [SerializeField] private LayerMask npcLayer;

    [Tooltip("Layers that can block vision (walls, props, etc.)")]
    [SerializeField] private LayerMask obstacleLayers;

    [Tooltip("Optional: eye position. If null, uses transform.position + Y")]
    [SerializeField] private Transform eyes;

    [Header("Death Area (when corpse is SEEN)")]
    [SerializeField] private Area deathAreaPrefab;
    [SerializeField] private float deathAreaRadius = 2f;
    [SerializeField] private float deathAreaDestroyTime = 40f;

    // references
    private NPCWander self;

    // to avoid spamming multiple times for the same dead NPC
    private readonly HashSet<NPCWander> reportedCorpses = new HashSet<NPCWander>();

    private void Awake()
    {
        self = GetComponent<NPCWander>();
    }

    private void Update()
    {
        // If this NPC is dead, it doesn't see anything
        if (self.IsDead)
            return;

        List<NPCWander> visibleNPCs = GetVisibleNPCs();

        if (visibleNPCs.Count == 0)
            return;

        // Log how many NPCs we see
        Debug.Log($"{name} sees {visibleNPCs.Count} NPC(s):");

        foreach (var npc in visibleNPCs)
        {
            Debug.Log($"   {npc.name} -> state = {npc.CurrentState}");

            // If NPC is dead and not already processed, handle corpse
            if (npc.IsDead && !reportedCorpses.Contains(npc))
            {
                HandleCorpseSeen(npc);
            }
        }
    }

    /// <summary>
    /// Returns all NPCWander visible in front of this NPC
    /// </summary>
    private List<NPCWander> GetVisibleNPCs()
    {
        List<NPCWander> result = new List<NPCWander>();

        Vector3 eyePos = GetEyePosition();

        // Get all colliders in a sphere around us on NPC layer
        Collider[] hits = Physics.OverlapSphere(eyePos, viewDistance, npcLayer);

        foreach (Collider hit in hits)
        {
            NPCWander other = hit.GetComponentInParent<NPCWander>();
            if (other == null)
                continue;

            if (other == self)
                continue;

            // direction to other NPC
            Vector3 dir = other.transform.position - eyePos;
            float dist = dir.magnitude;
            dir.Normalize();

            // check angle
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > viewAngle * 0.5f)
                continue;

            // line of sight check
            if (Physics.Raycast(eyePos, dir, out RaycastHit hitInfo, dist, obstacleLayers))
            {
                // something blocking and it's not that NPC (or its child)
                if (!hitInfo.transform.IsChildOf(other.transform))
                    continue;
            }

            result.Add(other);
        }

        return result;
    }

    private Vector3 GetEyePosition()
    {
        if (eyes != null)
            return eyes.position;

        return transform.position + Vector3.up * 1.6f;
    }

    /// <summary>
    /// When this NPC sees a dead NPC, simulate "body discovered":
    /// - spawn death area
    /// - raise OnAnyNPCKilled event
    /// </summary>
    private void HandleCorpseSeen(NPCWander deadNpc)
    {
        reportedCorpses.Add(deadNpc);

        Vector3 deathPos = deadNpc.transform.position;

        // 1. Create death area at corpse position
        if (deathAreaPrefab != null)
        {
            Area newArea = Instantiate(deathAreaPrefab, deathPos, Quaternion.identity);
            newArea.Radius = deathAreaRadius;
            Destroy(newArea.gameObject, deathAreaDestroyTime);
           // sus.suspicion += 100;
        }

        // 2. Notify other NPCs (like Die() does)
        NPCWander.OnAnyNPCKilled?.Invoke(deathPos, deadNpc.transform);

        Debug.Log($"{name} discovered a dead NPC: {deadNpc.name}");
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * 1.6f;

        // View distance sphere
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        // FOV cone lines
        Vector3 forward = transform.forward;

        Quaternion leftRot = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f);
        Quaternion rightRot = Quaternion.Euler(0f, viewAngle * 0.5f, 0f);

        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePos, eyePos + leftDir * viewDistance);
        Gizmos.DrawLine(eyePos, eyePos + rightDir * viewDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePos, eyePos + forward * viewDistance);
    }
}
