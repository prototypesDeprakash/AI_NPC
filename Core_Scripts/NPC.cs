using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace mygame
{

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]

    public class NPC : MonoBehaviour
    {
        [HideInInspector]
        public NavMeshAgent Agent;
        [HideInInspector]
        public Animator Animator;

        public float CurrentSpeed
        {
            get { return Agent.velocity.magnitude; }
        }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
        }

    }
}
