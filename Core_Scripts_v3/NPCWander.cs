using HutongGames.PlayMaker;
using mygame;
using UnityEngine;
using UnityEngine.AI;

public class NPCWander : NPCComponent
{
    public Area Area;
    public Area[] DanceArea;
    public Area PanicArea;
    enum EState
    {
        Wandering,
        Waiting,
        GoingTODance,
        Dancing,
        CallingCops,
        paniking
    }
    [SerializeField] float maxWaitTime = 3f;
    [SerializeField] private float MaxWaitTimeRandom = 5f;
    [SerializeField] private float waitTime = 0f;

   // float currentMaxwaitTime = 3f;
    [Space(15f)] float maxWanderTime = 10f;
    [SerializeField] private float WanderTime = 0f;


    private float maxDancingTime = 40f;
    [SerializeField] private float DancingTIme = 40f;

    [SerializeField] bool isDead = false;

    //Npc Death
    [SerializeField] Area deathAreaPrefab;
    [SerializeField] float deathAreaRadius = 2f;

    //For Npc Awarness
    [SerializeField] float callCopsDuration = 2f;
    [SerializeField] float panicDuration = 30f;
    private float callCopsTime = 0f;
    private float panicTime = 0f;
    [SerializeField] private float DeathAreaDestroyTime = 40f;
    [Header("Awareness")]
    [SerializeField] float reportRadius = 2f;   // close witnesses: call cops
    [SerializeField] float panicRadius = 10f;  // further: panic
    [SerializeField] float viewDistance = 12f;
    [SerializeField] float viewAngle = 90f;
    [SerializeField] LayerMask visionObstacles; // walls, props, etc.
   [SerializeField] private PlayerSuspicion sus;
    [SerializeField] Transform eyes; // optional: where NPC "sees" from

    // global event: any NPC death
    public static System.Action<Vector3, Transform> OnAnyNPCKilled;

    [SerializeField] Transform killerDebugTarget;

    //Ui expressions for Npcs
    [Header("Emotions Ui")]
    [SerializeField]private GameObject Calling_Police_ui;
    [SerializeField] private GameObject Paniking_ui;
    
    //[SerializeField] Transform player;



    [Header("Debugging")]
    [SerializeField]
    EState state = EState.Wandering;

    private void Start()
    {
        if (Random.Range(0f, 100.0f) > 50f)
        {
            ChangeState(EState.Wandering);
        }
        else
        {
            ChangeState(EState.Waiting);
        }
        OnAnyNPCKilled += HandleNPCKilled;


        //diable all ui
        Calling_Police_ui.SetActive(false);
        Paniking_ui.SetActive(false);
    }
    private void OnDestroy()
    {
        OnAnyNPCKilled -= HandleNPCKilled;
    }
    //Update Method
    private void Update()
    {
        if (isDead) return;
        if (state == EState.Waiting)
        {
            waitTime -= Time.deltaTime;
            if (waitTime < 0f)
            {
                ChangeState(EState.Wandering);
            }
        }
       else if(state == EState.Wandering)
        {
            WanderTime -= Time.deltaTime;
            if (HasArrived() || WanderTime< 0f)
            {
                if (Random.Range(0f, 100.0f) < 50f)
                {
                    ChangeState(EState.Wandering);
                }
                else
                {
                    ChangeState(EState.GoingTODance);
                }
               
            }
        }
       else if(state == EState.GoingTODance)
        {
            if (HasArrived())
            {
                ChangeState(EState.Dancing);
            }
        }
       else if (state == EState.Dancing)
        {
            DancingTIme -= Time.deltaTime;
            if (DancingTIme <= 0f)
            {
                ChangeState(EState.Waiting);
            }
        }
        else if (state == EState.CallingCops)
        {
            callCopsTime -= Time.deltaTime;
            if (callCopsTime <= 0f)
            {
                ChangeState(EState.paniking);
            }
        }
        else if(state == EState.paniking)
        {
            panicTime -= Time.deltaTime;
            if(panicTime<=0f || HasArrived())
            {
                ChangeState(EState.Wandering);
            }
        }


    }
    //Change State Method
    void ChangeState(EState newState)
    {
        state = newState;
        if (state == EState.Wandering)
        {
            npc.Agent.isStopped = false;
            SetRandomDestination();
            WanderTime = maxWanderTime;

            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isDancing", false);
                npc.Animator.SetBool("isPanicking", false);
            }
            //making sure calling ui is disabled
            Calling_Police_ui.SetActive(false);
            Paniking_ui.SetActive(false);

        } else if (state == EState.Waiting)
        {
            waitTime = maxWaitTime + Random.Range(0f, MaxWaitTimeRandom);
            npc.Agent.isStopped = true;
            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isDancing", false);
                npc.Animator.SetBool("isPanicking", false);
            }
            //making sure callling ui is diable

            Calling_Police_ui.SetActive(false);
            Paniking_ui.SetActive(false);
        }
        else if (state == EState.GoingTODance)
        {
            npc.Agent.isStopped = false;

            SetDancingDestination();
            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isPanicking", false);
            }

            //maing srue calling ui is disabled
            Calling_Police_ui.SetActive(false);
            Paniking_ui.SetActive(false);


        }
        else if (state == EState.Dancing)
        {
            npc.Agent.isStopped = true;
            DancingTIme = maxDancingTime;
            Debug.Log("Play Dancing Animation");
            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isDancing", true);
                npc.Animator.SetBool("isPanicking", false);
            }
        }
        else if (state == EState.CallingCops)
        {
            npc.Agent.isStopped = true;
            callCopsTime = callCopsDuration;
            //callingCops
            //if (player != null && PoliceManager.Instance != null)
            //{
            //    PoliceManager.Instance.CreateAlert(player.position);
            //}
            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isDancing", false);
                npc.Animator.SetBool("isPanicking", false);
                //will fix later animation wont work for now
                npc.Animator.SetBool("CallCops",true);
                
            }
            Debug.Log(name + " is calling cops!");
            Calling_Police_ui.SetActive(true);
            Paniking_ui.SetActive(false);

        }
        else if(state  == EState.paniking)
        {
            npc.Agent.isStopped = false;
            panicTime = panicDuration;
            SetPanicDestination();
            if (npc.Animator != null)
            {
                npc.Animator.SetBool("isDancing", false);
                npc.Animator.SetBool("isPanicking", true); // create bool in Animator
            }
            Paniking_ui.SetActive(true);
            Calling_Police_ui.SetActive(true);



        }
    }
    
     bool HasArrived()
     {
         // return npc.Agent.remainingDistance <= npc.Agent.stoppingDistance;
         var agent = npc.Agent;

         if (!agent.isOnNavMesh)
             return true; 

         if (agent.pathPending)
             return false;

        //If any bug remove this line later || agent.velocity.sqrMagnitude < 0.001f
        // If agent has no path or velocity is basically zero and distance is small-ish, call it arrived
        if (!agent.hasPath )
             return true;

         if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
         {
             if (agent.velocity.sqrMagnitude < 0.01f)
                 return true;
         }

         return false;

     }
    
    /*
    bool HasArrived()
    {
        var agent = npc.Agent;

        if (!agent.isOnNavMesh)
            return true; // fallback: agent not on navmesh

        if (agent.pathPending)
            return false;

        // If agent has a path, check arrival conditions
        if (agent.hasPath)
        {
            // If path is partial or invalid, consider NOT arrived yet (we'll handle blocked elsewhere)
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid || agent.pathStatus == NavMeshPathStatus.PathPartial)
                return false;

            // Normal arrival: close enough and nearly stopped
            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (agent.velocity.sqrMagnitude < 0.01f)
                    return true;
            }

            return false;
        }

        // No path and not pending: treat as NOT arrived (likely blocked)
        return false;
    }

    */
    public void Die(Transform killer = null)
    {
        if (isDead) return;
        isDead = true;

        npc.Agent.isStopped = true;
        npc.Agent.ResetPath();

        if(npc.Animator!= null)
        {
            npc.Animator.SetBool("Die", true);
        }
       
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
        RaycastHit hit;
        Vector3 start = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(start, Vector3.down, out hit, 5f))
        {
            transform.position = hit.point;
        }

        NPCWander wander = GetComponent<NPCWander>();
        wander.enabled = false;

        //Creating death Area
        if (deathAreaPrefab != null)
        {
            Vector3 areaPos = killer != null ? killer.position : transform.position;

            Area newArea = Instantiate(deathAreaPrefab, areaPos, Quaternion.identity);
            newArea.Radius = deathAreaRadius;

            // Destroy ONLY THIS instance after X seconds
            Destroy(newArea.gameObject, DeathAreaDestroyTime);   // <-- set your own duration
        }
        OnAnyNPCKilled?.Invoke(transform.position, killer);
        //increase player sus

        sus.suspicion += 100;
        // Disable Ui
        Calling_Police_ui.SetActive(false);

    }



    // Logic to react to a npc death
    void HandleNPCKilled(Vector3 deathPos, Transform killer)
    {
        if (isDead) return;     // dead don't react
        if (!enabled) return;   // this NPC is disabled

        float dist = Vector3.Distance(transform.position, deathPos);

        // Too far to care
        if (dist > panicRadius)
            return;

        // If within report radius and has line of sight to killer → call cops
        if (dist <= reportRadius)
        {
            ChangeState(EState.CallingCops);
        }
        // Otherwise if within panic radius → panic and run
        else if (dist <= panicRadius)
        {
            ChangeState(EState.paniking);
        }
    }
    bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;

        Vector3 eyePos = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * 1.6f;

        Vector3 dir = (target.position - eyePos);
        float dist = dir.magnitude;
        dir.Normalize();

        if (dist > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f)
            return false;

        // Check if something blocks view
        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist, visionObstacles))
        {
            // Hit something before the player
            if (hit.transform != target)
                return false;
        }

        return true;
    }


    //Select random point in wandering area as destination
    void SetRandomDestination()
    {
        npc.Agent.SetDestination(Area.GetRandomPoint());

    }
    //selecting random point in dancing area
    void SetDancingDestination()
    {
        // npc.Agent.SetDestination(DanceArea.GetRandomPoint());
        Area chosen = DanceArea[Random.Range(0, DanceArea.Length)];
        npc.Agent.SetDestination(chosen.GetRandomPoint());

    }
    //selecting random point form panic area    
    void SetPanicDestination()
    {
        if (PanicArea != null)
        {
            npc.Agent.SetDestination(PanicArea.GetRandomPoint());
        }
        else
        {
            // fallback if you forgot to set PanicArea
            npc.Agent.SetDestination(Area.GetRandomPoint());
        }
    }


    //for visual Debugging
    private void OnDrawGizmosSelected()
    {
        if (isDead) return;

        // Determine the eye position
        Vector3 eyePos = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * 1.6f;

        // Draw view distance
        Gizmos.color = new Color(0, 1, 0, 0.25f); // light green
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        // Draw panic radius (optional)
        Gizmos.color = new Color(1, 0.5f, 0, 0.25f); // orange
        Gizmos.DrawWireSphere(transform.position, panicRadius);

        // Draw report radius (optional)
        Gizmos.color = new Color(1, 0, 0.5f, 0.25f); // pink
        Gizmos.DrawWireSphere(transform.position, reportRadius);

        // Draw FOV cone lines
        Gizmos.color = Color.yellow;

        Vector3 forward = transform.forward;

        // Left boundary
        Quaternion leftRot = Quaternion.Euler(0, -viewAngle * 0.5f, 0);
        Vector3 leftDir = leftRot * forward;

        // Right boundary
        Quaternion rightRot = Quaternion.Euler(0, viewAngle * 0.5f, 0);
        Vector3 rightDir = rightRot * forward;

        Gizmos.DrawLine(eyePos, eyePos + leftDir * viewDistance);
        Gizmos.DrawLine(eyePos, eyePos + rightDir * viewDistance);

        // Draw forward line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePos, eyePos + forward * viewDistance);

        // Optional: draw raycast hit preview (only in Play Mode)
        if (Application.isPlaying)
        {
            if (killerDebugTarget != null)
            {
                Vector3 dir = (killerDebugTarget.position - eyePos).normalized;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(eyePos, killerDebugTarget.position);
            }
        }
    }



}
