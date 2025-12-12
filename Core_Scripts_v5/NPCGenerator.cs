using mygame;
using UnityEngine;

public class NPCGenerator : MonoBehaviour
{
    [SerializeField] NPC NPCPrefab;

    [Header("Spawn")]
    [SerializeField] Area spawnArea;

    [Header("Behavior")]
    [SerializeField] Area wanderArea;

    [SerializeField] int count = 15;

    private void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 position = spawnArea.GetRandomPoint();
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            NPC npc = Instantiate(NPCPrefab, position, rotation);

            var wander = npc.GetComponent<NPCWander>();
            wander.Area = wanderArea;
        }
    }
}
