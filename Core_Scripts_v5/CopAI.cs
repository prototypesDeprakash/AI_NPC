using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace mygame
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CopAI : MonoBehaviour
    {
        enum State { Waiting, GoingToArea, SearchingArea, ChasingPlayer, Returning }

        [Header("References")]
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Transform player;

        [Header("Vision")]
        [SerializeField] float viewDistance = 12f;
        [SerializeField] float viewAngle = 100f;
        [SerializeField] LayerMask visionObstacles; // layers that block view (walls)
        [SerializeField] LayerMask playerLayer;     // layer for player raycasts if needed

        [Header("Search / timings")]
        [SerializeField] float searchDuration = 12f;      // how long to search the area
        [SerializeField] float timeToLosePlayer = 2.0f;   // seconds before giving up chase
        [SerializeField] float reachThreshold = 0.5f;     // considered "reached" an agent destination
        [SerializeField] float arrestDistance = 1.2f;     // distance at which cop arrests player
        [SerializeField] float arrivalTolerance = 0.2f;   // tolerance for being on top of a point

        [Header("Area lookup")]
        [SerializeField] float maxAreaFindDistance = 15f; // max distance from deathPos to consider an Area valid
        [SerializeField] float navMeshSampleDistance = 2f; // how far to search for a nearby navmesh sample

        [Header("repathing if doors closed")]
        [SerializeField] float repathInterval = 0.5f;
        float repathTimer = 0f;

        // runtime
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        State state = State.Waiting;
        Area currentArea;
        Vector3 currentTargetPos; // world position we are moving to (area center or sampled navmesh pos)
        float searchTimer;
        float loseSightTimer;
        float nextPatrolAt = 0f;

        //for busted 
        [SerializeField] GameObject bustedUI;
        [SerializeField] float bustedDelay = 2f; // wait before reload
        [SerializeField] string sceneName = "";  // leave empty to reload current scene

        bool alreadyBusted = false;

        //Light effects 
       [SerializeField] private DiscoFogRGB discoFog;


        // debugging
        public bool debugGizmos = false;
        [SerializeField] Color gizmoColorArea = Color.yellow;
        [SerializeField] Color gizmoColorChase = Color.red;

       

        void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            agent.updateRotation = true;
            agent.updateUpAxis = true;
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
                    // idle - can add idle behavior here
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

            // Always check for player visibility on every frame (helps cops spot player en route)
            if (state != State.ChasingPlayer && CanSeePlayer() && IsPlayerRelevant())
            {
                StartChasing();
            }
        }

        // Event handler called when ANY NPC dies
        /*  void HandleAnyNPCKilled(Vector3 deathPos, Transform killer)
          {
              // Find the nearest Area instance to deathPos (if any)
              Area found = FindNearestArea(deathPos, maxAreaFindDistance);

              if (found != null)
              {
                  currentArea = found;
                  // prefer moving to the area's center (cop then will wander/search inside)
                  Vector3 target = currentArea.transform.position;
                  SetDestinationTo(target);
                  state = State.GoingToArea;
              }
              else
              {
                  // No Area instance found — move directly to the deathPos (sampled to navmesh)
                  currentArea = null;
                  SetDestinationTo(deathPos);
                  state = State.GoingToArea;
              }
          }
        */
        void HandleAnyNPCKilled(Vector3 deathPos, Transform killer)
        {
            // 1. If already chasing, do NOT get distracted by new bodies
            if (state == State.ChasingPlayer)
                return;

            // 2. If we currently see the player, don't switch to death-area mode
            if (CanSeePlayer())
                return;

            // 3. (Optional) If we're very close to spawn / idle, maybe ignore far crimes
            // float distToCrime = Vector3.Distance(transform.position, deathPos);
            // if (distToCrime > maxAreaFindDistance * 1.5f) return;

            // 4. Normal behavior: go investigate this crime
            Area found = FindNearestArea(deathPos, maxAreaFindDistance);

            if (found != null)
            {
                currentArea = found;
                Vector3 target = currentArea.transform.position;
                SetDestinationTo(target);
                state = State.GoingToArea;
            }
            else
            {
                currentArea = null;
                SetDestinationTo(deathPos);
                state = State.GoingToArea;
            }
        }


        /*
        void SetDestinationTo(Vector3 worldPos)
        {
            // Sample navmesh near worldPos to get a reachable spot
            NavMeshHit hit;
            if (NavMesh.SamplePosition(worldPos, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                currentTargetPos = hit.position;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
            }
            else
            {
                // fallback: if we can't sample, still set worldPos — agent will attempt and pathStatus will inform us
                currentTargetPos = worldPos;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
            }
        }
        */
        void SetDestinationTo(Vector3 worldPos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(worldPos, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                currentTargetPos = hit.position;
            }
            else
            {
                currentTargetPos = worldPos;
            }

            agent.isStopped = false;
            agent.SetDestination(currentTargetPos);
            repathTimer = repathInterval;     // reset
        }
        void UpdateGoingToArea()
        {
            //Light effect 
            discoFog.policsChasingLightEffect = true;
            // If no target, bail out
            if (currentTargetPos == Vector3.zero && currentArea == null)
            {
                StartReturnToSpawn();
                return;
            }

            // 🔁 If path is invalid/partial and we’re not close, keep trying to repath
            if (!agent.pathPending &&
                (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial))
            {
                // far from target? try again occasionally (door might have opened)
                if (Vector3.Distance(transform.position, currentTargetPos) > reachThreshold + arrivalTolerance)
                {
                    repathTimer -= Time.deltaTime;
                    if (repathTimer <= 0f)
                    {
                        SetDestinationTo(currentTargetPos);
                        return;
                    }
                }
            }

            // If we see player in the area -> chase
            if (IsPlayerInsideCurrentArea() && CanSeePlayer())
            {
                if (Vector3.Distance(transform.position, currentTargetPos) <= (currentArea != null ? currentArea.Radius * 0.8f : 3f))
                {
                    StartChasing();
                    return;
                }
            }

            // Arrival logic
            if (!agent.pathPending)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold &&
                        agent.velocity.sqrMagnitude < 0.1f)
                    {
                        BeginSearch();
                        return;
                    }
                }
                else
                {
                    // partial/invalid but very close -> treat as "arrived"
                    if (Vector3.Distance(transform.position, currentTargetPos) <= reachThreshold + arrivalTolerance)
                    {
                        BeginSearch();
                        return;
                    }
                }
            }
        }

        /*
        void UpdateGoingToArea()
        {
            // If no target, bail out
            if (currentTargetPos == Vector3.zero && currentArea == null)
            {
                StartReturnToSpawn();
                return;
            }

            // If we see the player inside the area on the way, start chase immediately
            if (IsPlayerInsideCurrentArea() && CanSeePlayer())
            {
                // if cop is reasonably close to area center, abandon path and chase
                if (Vector3.Distance(transform.position, currentTargetPos) <= (currentArea != null ? currentArea.Radius * 0.8f : 3f))
                {
                    StartChasing();
                    return;
                }
            }

            // Check arrival accounting for agent.pathStatus and remainingDistance
            if (!agent.pathPending)
            {
                // If path is invalid/partial — still allow arriving if we are close by
                if (agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold && agent.velocity.sqrMagnitude < 0.1f)
                    {
                        BeginSearch();
                        return;
                    }
                }
                else
                {
                    // Path partial or invalid: if we are physically near the target, treat as arrived so we can search
                    if (Vector3.Distance(transform.position, currentTargetPos) <= reachThreshold + arrivalTolerance)
                    {
                        BeginSearch();
                        return;
                    }
                }
            }
        }
        */
        void BeginSearch()
        {
            state = State.SearchingArea;
            searchTimer = searchDuration;
            nextPatrolAt = Time.time + 0.1f;
            // Move to a first random point to start searching
            MoveToRandomPointInAreaOrAroundTarget();
        }

        void UpdateSearchingArea()
        {

            // If player present and visible -> chase
            if (IsPlayerInsideCurrentArea() && CanSeePlayer())
            {
                StartChasing();
                return;
            }

            // Keep patrolling random points inside area while searching
            if (Time.time >= nextPatrolAt)
            {
                MoveToRandomPointInAreaOrAroundTarget();
                nextPatrolAt = Time.time + Random.Range(0.8f, 2.0f);
            }

            // Decrease timer
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

            // chase
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
                    // lost the player for too long -> give up and return
                    //MoveToRandomPointInAreaOrAroundTarget();
                   StartReturnToSpawn();
                    return;
                }
            }

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= arrestDistance)
            {
                if (!alreadyBusted)
                {
                    alreadyBusted = true;
                    Debug.Log($"{name}: ARRESTED player!");
                    StartCoroutine(HandleBusted());
                }
            }

            //if (dist <= arrestDistance)
            //{
            //    Debug.Log($"{name}: ARRESTED player!");
            //    // Implement player arrest/damage logic here (call player's method or game manager)
            //    // here i am calling the reload game method
            //    StartReturnToSpawn(); // or keep chasing if you prefer
            //}
        }
        IEnumerator HandleBusted()
        {
            // stop AI so it doesn't keep moving
            agent.isStopped = true;

            // show UI
            if (bustedUI != null)
                bustedUI.SetActive(true);

            // wait
            yield return new WaitForSeconds(bustedDelay);

            // reload scene
            if (string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        void UpdateReturning()
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance + reachThreshold)
                {
                    // Reset to idle
                    state = State.Waiting;
                    agent.isStopped = true;
                    transform.rotation = spawnRotation;
                }
            }
        }

        void StartChasing()
        {
            discoFog.policsChasingLightEffect = true;

            state = State.ChasingPlayer;
            loseSightTimer = timeToLosePlayer;
            agent.isStopped = false;
            // immediate set destination to current player pos
            if (player != null)
                agent.SetDestination(player.position);
        }

        void StartReturnToSpawn()
        {
            //light effect reset to disco on chase ending
            discoFog.policsChasingLightEffect = false;

            currentArea = null;
            state = State.Returning;
            agent.isStopped = false;
            SetDestinationTo(spawnPosition);
        }

        void MoveToRandomPointInAreaOrAroundTarget()
        {
            // If we have a valid currentArea -> pick point inside it
            if (currentArea != null)
            {
                Vector3 pt = currentArea.GetRandomPoint();
                SetDestinationTo(pt);
            }
            else
            {
                // No Area -> pick a random point around the crime target position
                Vector3 randomOffset = Random.insideUnitSphere * Mathf.Max(1f, agent.stoppingDistance + 2f);
                randomOffset.y = 0f;
                SetDestinationTo(currentTargetPos + randomOffset);
            }
        }

        bool IsPlayerInsideCurrentArea()
        {
            if (player == null) return false;
            if (currentArea != null)
                return Vector3.Distance(player.position, currentArea.transform.position) <= currentArea.Radius;
            // fallback: check distance to currentTargetPos (which is the crime pos)
            return Vector3.Distance(player.position, currentTargetPos) <= Mathf.Max(2f, 3f);
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

            // Raycast, but ignore the player's own colliders by layer mask
            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, visionObstacles))
            {
                // Hit something before player -> blocked
                return false;
            }

            return true;
        }

        bool IsPlayerRelevant()
        {
            // quick check: only chase if player is inside area or close enough to the crime spot
            if (currentArea != null)
                return IsPlayerInsideCurrentArea();
            // if no area, consider the player relevant if near the target pos
            return Vector3.Distance(player.position, currentTargetPos) <= Mathf.Max(3f, maxAreaFindDistance * 0.5f);
        }

        // find nearest Area instance to pos within a maxDistance (returns null if none)
        Area FindNearestArea(Vector3 pos, float maxDistance)
        {
            Area[] all = FindObjectsOfType<Area>();
            Area best = null;
            float bestDist = Mathf.Infinity;

            for (int i = 0; i < all.Length; i++)
            {
                // skip disabled or destroyed ones
                if (all[i] == null || !all[i].gameObject.activeInHierarchy) continue;

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

        void OnDrawGizmosSelected()
        {
            if (!debugGizmos) return;

            Gizmos.color = gizmoColorArea;
            Gizmos.DrawWireSphere(currentTargetPos == Vector3.zero ? transform.position : currentTargetPos, 0.6f);

            if (player != null)
            {
                Gizmos.color = gizmoColorChase;
                Gizmos.DrawWireSphere(player.position, arrestDistance);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, viewDistance);
        }
    }
}
