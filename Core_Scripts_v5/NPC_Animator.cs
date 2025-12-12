using Unity.VisualScripting;
using UnityEngine;


namespace mygame {

    public class NPC_Animator : NPCComponent
    {
        private void Update()
        {
            npc.Animator.SetFloat("Mag", npc.CurrentSpeed);
        }
    }
}