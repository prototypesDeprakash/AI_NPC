using UnityEngine;
using TMPro;

namespace mygame
{
    public class NPCDialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue Source")]
        [SerializeField] private NPCWander wandererDialogue;

        private string[] npcDialogues;

        [Range(0f, 1f)]
        public float talkChance = 0.7f;

        [Header("UI")]
        public GameObject dialogueCanvas;
        public TextMeshProUGUI dialogueText;

        private void Awake()
        {
            if (wandererDialogue != null)
                npcDialogues = wandererDialogue.Normal_Dialogues;
        }

        private void Start()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("dialogenter");
            if (!other.CompareTag("Player")) return;

            if (npcDialogues == null || npcDialogues.Length == 0) return;

            if (Random.value > talkChance )
                return;

            int index = Random.Range(0, npcDialogues.Length);
            dialogueText.text = npcDialogues[index];
            dialogueCanvas.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            dialogueCanvas.SetActive(false);
            dialogueText.text = string.Empty;
        }
    }
}
