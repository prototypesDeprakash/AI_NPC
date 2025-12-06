using UnityEngine;

namespace mygame
{
    public class NPCRandomModel : MonoBehaviour
    {
        [SerializeField] GameObject[] variants;
        [SerializeField] NPC npc;   // your main NPC script that has Animator & CurrentSpeed

        void Awake()
        {
            // Auto-assign NPC if not set in inspector
            if (npc == null)
            {
                npc = GetComponent<NPC>();
                if (npc == null)
                {
                    Debug.LogError($"[NPCRandomModel] No NPC component found on {name}");
                    return;
                }
            }

            if (variants == null || variants.Length == 0)
            {
                Debug.LogError($"[NPCRandomModel] No variants assigned on {name}");
                return;
            }

            int selected = Random.Range(0, variants.Length);

            for (int i = 0; i < variants.Length; i++)
            {
                bool isSelected = i == selected;
                variants[i].SetActive(isSelected);

                if (isSelected)
                {
                    var variantAnimator = variants[i].GetComponent<Animator>();
                    if (variantAnimator == null)
                    {
                        Debug.LogError($"[NPCRandomModel] No Animator on variant {variants[i].name}");
                        return;
                    }

                    npc.Animator = variantAnimator;
                }
            }
        }
    }
}
