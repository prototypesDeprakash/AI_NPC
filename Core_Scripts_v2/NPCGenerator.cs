using UnityEngine;

namespace mygame
{
    public class NPCGenerator : MonoBehaviour
    {
        [SerializeField] NPC NPCPrefab;
        [SerializeField] Area Area;
        [SerializeField] int count = 15;

        private void Start()
        {
            for(int i = 0; i < count; i++)
            {
                Vector3 position = Area.GetRandomPoint();
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                NPC npc = Instantiate(NPCPrefab, position, rotation);
                npc.GetComponent<NPCWander>().Area = Area;
                
            }
        }

    }
}