using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using mygame;


public class AiSensor : MonoBehaviour
{
    [SerializeField] private float distance = 10f;
    [SerializeField] private float angle = 30f;
    [SerializeField] private float height = 1.0f;
    [SerializeField] private Color meshColor = Color.green;

    [SerializeField] private int scanFrequency = 30;
    [SerializeField] private LayerMask layers;
    [SerializeField] private LayerMask occulsionLayers;
    public List<GameObject> Objects  = new List<GameObject>();
    public static System.Action<Vector3> OnAnyNPCKilled;

    Collider[] colliders =  new Collider[50];
    Mesh mesh;
    int count;
    float scanInterval;
    float scanTimer;
    [SerializeField] Area deathAreaPrefab;
    [SerializeField] float deathAreaRadius = 10f;
    [SerializeField] private float DeathAreaDestroyTime = 40f;


    void Start()
    {
        scanInterval =1.0f / scanFrequency; 
    }

    void Update()
    {
        scanTimer -=Time.deltaTime;
        if ((scanTimer <= 0))
        {
            scanTimer += scanInterval;
            Scan();
        }
        {
            
        }
    }

    public bool IsInSight(GameObject obj)
    {
        Vector3 origin  = transform.position;
        Vector3 dest   = obj.transform.position;
        Vector3 direction = dest - origin;
        if(direction.y< 0 || direction.y > height)
        {
            return false;
        }
          
        direction.y = 0;
        float deltaAngle = Vector3.Angle(direction,transform.forward);
        if (deltaAngle > angle)
        {
            return false;
        }

        origin.y += height / 2;
        dest.y = origin.y;
        if (Physics.Linecast(origin, dest,occulsionLayers))
        {
            return false;
        }

        return true;


    }


    //private void Scan()
    //{
    //    count = Physics.OverlapSphereNonAlloc(transform.position, distance, colliders, layers, QueryTriggerInteraction.Collide) ;
    //    Objects.Clear();
    //    for (int i = 0; i < count; i++)
    //    {
    //       GameObject obj = colliders[i].gameObject;
    //        if(IsInSight(obj))
    //        {
    //            Objects.Add(obj);
    //        }
    //    }


    //}
    //private void Scan()
    //{
    //    count = Physics.OverlapSphereNonAlloc(
    //        transform.position,
    //        distance,
    //        colliders,
    //        layers,
    //        QueryTriggerInteraction.Collide
    //    );

    //    Objects.Clear();

    //    for (int i = 0; i < count; i++)
    //    {
    //        GameObject obj = colliders[i].gameObject;

    //        // NPC CAN SEE IT
    //        if (IsInSight(obj))
    //        {
    //            Objects.Add(obj);

    //            // Only when NPC actually sees the death marker
    //            if (obj.CompareTag("deathMark") &&
    //                obj.layer == LayerMask.NameToLayer("dead"))
    //            {
    //                Debug.Log("hello");
    //                OnAnyNPCKilled?.Invoke(obj.transform.position);

    //                Destroy(obj); // IMPORTANT: fire only once

    //                if (deathAreaPrefab != null)
    //                {
    //                   // Vector3 areaPos = killer != null ? killer.position : transform.position;

    //                    Area newArea = Instantiate(deathAreaPrefab, transform.position, Quaternion.identity);
    //                    newArea.Radius = deathAreaRadius;

    //                    // Destroy ONLY THIS instance after X seconds
    //                   // Destroy(newArea.gameObject, DeathAreaDestroyTime);   // <-- set your own duration
    //                }
    //            }
    //        }
    //    }
    //}

    private void Scan()
    {
        count = Physics.OverlapSphereNonAlloc(
            transform.position,
            distance,
            colliders,
            layers,
            QueryTriggerInteraction.Collide
        );

        Objects.Clear();

        bool foundDeadNPC = false;
        Vector3 deathPos = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = colliders[i].gameObject;

            // Only consider things in line of sight
            if (!IsInSight(obj))
                continue;

            Objects.Add(obj);

            // Check if this is an NPC and if it's dead
            NPCWander npc = obj.GetComponent<NPCWander>();
            //if (npc != null && npc.IsDead)          // <- see note below
            //{
            //    foundDeadNPC = true;
            //    deathPos = obj.transform.position;
            //    break; // one dead body is enough to trigger a response
            //}
        }

        if (foundDeadNPC)
        {
            Debug.Log($"{name} sees a dead NPC at {deathPos}, creating death area.");

            if (deathAreaPrefab != null)
            {
                Area newArea = Instantiate(deathAreaPrefab, deathPos, Quaternion.identity);
                newArea.Radius = deathAreaRadius;
                // Destroy(newArea.gameObject, DeathAreaDestroyTime);
            }

            // If you still want to notify others globally:
            OnAnyNPCKilled?.Invoke(deathPos);
        }
    }


    Mesh CreateWedgeMesh()
    {
        Mesh mesh = new Mesh();

        int segments = 10;

        int numTriangles = (segments * 4) + 2 + 2;
        int numVertices = numTriangles *3;
        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        Vector3 bottomCenter = Vector3.zero;
        Vector3 bottomLeft = Quaternion.Euler(0, -angle, 0) * Vector3.forward * distance;
        Vector3 bottomRight = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;

        Vector3 topCenter = bottomCenter + Vector3.up * height;
        Vector3 topRight = bottomRight + Vector3.up * height;
        Vector3 topLeft = bottomLeft + Vector3.up * height;

        int vert = 0;

        //left side 
        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomLeft;
        vertices[vert++] = topLeft;


        vertices[vert++] = topLeft;
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;

        //right side 
        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topRight;

        vertices[vert++] = topRight;
        vertices[vert++] = bottomRight;
        vertices[vert++] = bottomCenter;


        float currentAngle = -angle;    
        float deltaAngle = (angle * 2) / segments;
        for(int i = 0; i < segments; i++)
        {

             
             bottomLeft = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * distance;
             bottomRight = Quaternion.Euler(0, currentAngle+deltaAngle, 0) * Vector3.forward * distance;

             
             topRight = bottomRight + Vector3.up * height;
             topLeft = bottomLeft + Vector3.up * height;


            //far side
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;
            vertices[vert++] = topRight;

            vertices[vert++] = topRight;
            vertices[vert++] = topLeft;
            vertices[vert++] = bottomLeft;

            //top
            vertices[vert++] = topCenter;
            vertices[vert++] = topLeft;
            vertices[vert++] = topRight;

            //bottom
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomRight;
            vertices[vert++] = bottomLeft;

            currentAngle += deltaAngle;

        }

      

        for (int i = 0; i < numVertices; ++i)
        {

            triangles[i] = i;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnValidate()
    {
        mesh = CreateWedgeMesh();
        scanInterval = 1.0f / scanFrequency;

    }
    private void OnDrawGizmos()
    {
        if (mesh)
        {
            Gizmos.color = meshColor;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }
        Gizmos.DrawWireSphere(transform.position, distance);    
        for(int i=0; i < count; i++)
        {   
            Gizmos.DrawSphere(colliders[i].transform.position,0.2f);
        }
        Gizmos.color = Color.red;
        foreach(var obj in Objects)
        { 
            Gizmos.DrawSphere(obj.transform.position,0.2f);
        }
    }

}
