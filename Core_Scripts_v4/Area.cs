using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.AI;

namespace mygame
{
    public class Area : MonoBehaviour
    {
        public float Radius = 20f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, Radius);
            
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius);

            Gizmos.color = Color.green;
          //  Gizmos.DrawSphere(transform.position, Radius);
        }


        public Vector3 GetRandomPoint()
        {
            Vector3 RandomDirection = Random.insideUnitSphere * Radius;
            RandomDirection.y = 0f;
            Vector3 RandomPoint = transform.position + RandomDirection;
            NavMeshHit hit;
            Vector3 finalPosition = transform.position;
            if(NavMesh.SamplePosition(RandomPoint , out  hit , 2f, 1))
            {
                finalPosition = hit.position;
            } 
            return finalPosition;
        }

    }

}