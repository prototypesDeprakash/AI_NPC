using UnityEngine;
using UnityEngine.AI;

namespace mygame
{
    public class CopAI : MonoBehaviour
    {
        enum State { Waiting, GoingToArea, SearchingArea, ChasingPlayer, Returning }

        [Header("References")]
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Transform player;

        [Header("Vision")]
        [SerializeField] float viewDistance = 12f;
        [SerializeField] float viewAngle = 100f;
        [SerializeField] LayerMask visionObstacles; // layers that block view

        [Header("Search / timings")]
        [SerializeField] float searchDuration = 12f;      // how long to search the area
        [SerializeField] float timeToLosePlayer = 2.0f;   // seconds before giving up chase
        [SerializeField] float reachThreshold = 0.5f;     // considered "reached" an agent destination
        [SerializeField] float arrestDistance = 1.2f;     // distance at which cop arrests player

        [Header("Area lookup")]
        [SerializeField] float maxAreaFindDistance = 15f; // max distance from deathPos to consider an Area valid

        // runtime
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        State state = State.Waiting;
        Area currentArea;
        float searchTimer;
        float loseSightTimer;
        float nextPatrolAt = 0f;

        void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        void OnEnable()
        {
            NPCWander.OnAnyNPCKilled += HandleAnyNPCKilled;
        }

        void OnDisable()
        {
            NPCWander.OnAnyNPCKilled -= HandleAnyNPCKilled;
        }

        void Update()
        {
            switch (state)
            {
                case State.Waiting:
                    // idle - do nothing
                    break;
                case State.GoingToArea:
                    UpdateGoingToArea();
                    break;
                case State.SearchingArea:
                    UpdateSearchingArea();
                    break;
                case State.ChasingPlayer:
                    UpdateChasingPlayer();
                    break;
                case State.Returning:
                    UpdateReturning();
                    break;
            }
        }

        // Event handler called when ANY NPC dies
        void HandleAnyNPCKilled(Vector3 deathPos, Transform killer)
        {
            // find the nearest Area spawned near deathPos
            Area found = FindNearestArea(deathPos, maxAreaFindDistance);
            if (found == null) return;

            currentArea = found;
            GoToArea(currentArea);
        }

        void GoToArea(Area area)
        {
            if (area == null) return;
            state = State.GoingToArea;
            agent.isStopped = false;
            agent.SetDestination(area.transform.position);
        }

        void UpdateGoingToArea()
        {
            if (currentArea == null)
            {
                StartReturnToSpawn();
                return;
            }

            // If we see the player inside the area on the way, start chase immediately
            if (IsPlayerInsideCurrentArea() && CanSeePlayer())
            {
                float distToArea = Vector3.Distance(transform.position, currentArea.transform.position);

                // Cop must be fairly close to the area before he abandons path and chases
                if (distToArea <= currentArea.Radius * 0.7f)
                {
                    StartChasing();
                    return;
                }
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
            {
                // arrived near center -> start searching
                state = State.SearchingArea;
                searchTimer = searchDuration;
                MoveToRandomPointInArea();
                nextPatrolAt = Time.time + 1f; // small delay before next pick
            }
        }

        void UpdateSearchingArea()
        {
            if (currentArea == null)
            {
                StartReturnToSpawn();
                return;
            }

            // If player is inside the area and visible -> chase/arrest
            if (IsPlayerInsideCurrentArea() && CanSeePlayer())
            {
                StartChasing();
                return;
            }

            // pick new patrol points inside area occasionally
            if (Time.time >= nextPatrolAt)
            {
                MoveToRandomPointInArea();
                nextPatrolAt = Time.time + Random.Range(1.0f, 3.0f);
            }

            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                StartReturnToSpawn();
            }
        }

        void UpdateChasingPlayer()
        {
            if (player == null)
            {
                StartReturnToSpawn();
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (CanSeePlayer())
            {
                loseSightTimer = timeToLosePlayer; // reset lose timer while seeing player
            }
            else
            {
                loseSightTimer -= Time.deltaTime;
                if (loseSightTimer <= 0f)
                {
                    // lost the player for too long -> give up
                    StartReturnToSpawn();
                    return;
                }
            }

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= arrestDistance)
            {
                Debug.Log($"{name}: ARRESTED (player)");
                // you can call player's Die/Arrest method here
                StartReturnToSpawn(); // or keep chasing depending on design
            }
        }

        void UpdateReturning()
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
            {
                state = State.Waiting;
                agent.isStopped = true;
                transform.rotation = spawnRotation;
            }
        }

        void StartChasing()
        {
            state = State.ChasingPlayer;
            loseSightTimer = timeToLosePlayer;
            agent.isStopped = false;
        }

        void StartReturnToSpawn()
        {
            currentArea = null;
            state = State.Returning;
            agent.isStopped = false;
            agent.SetDestination(spawnPosition);
        }

        void MoveToRandomPointInArea()
        {
            if (currentArea == null || agent == null) return;
            Vector3 pt = currentArea.GetRandomPoint();
            agent.SetDestination(pt);
        }

        bool IsPlayerInsideCurrentArea()
        {
            if (currentArea == null || player == null) return false;
            return Vector3.Distance(player.position, currentArea.transform.position) <= currentArea.Radius;
        }

        bool CanSeePlayer()
        {
            if (player == null) return false;

            Vector3 origin = transform.position + Vector3.up * 1.6f;
            Vector3 toPlayer = player.position - origin;
            float dist = toPlayer.magnitude;
            if (dist > viewDistance) return false;

            Vector3 dir = toPlayer.normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            if (angle > viewAngle * 0.5f) return false;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, visionObstacles))
            {
                if (hit.transform != player)
                    return false;
            }

            return true;
        }

        // find nearest Area instance to pos within a maxDistance
        Area FindNearestArea(Vector3 pos, float maxDistance)
        {
            Area[] all = FindObjectsOfType<Area>();
            Area best = null;
            float bestDist = Mathf.Infinity;

            for (int i = 0; i < all.Length; i++)
            {
                float d = Vector3.Distance(all[i].transform.position, pos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = all[i];
                }
            }

            if (best != null && bestDist <= maxDistance) return best;
            return null;
        }
    }
}
