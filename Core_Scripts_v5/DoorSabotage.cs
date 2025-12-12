using System.Collections;
using UnityEngine;

namespace mygame
{
    public class DoorSabotage : MonoBehaviour
    {
        // 🔥 The door the player is currently near
        public static DoorSabotage activeDoor;

        [Header("Door")]
        [Tooltip("Door gameobject that blocks the path.")]
        public GameObject doorObject;

        [Header("Timing")]
        public float closedDuration = 4f;
        public float cooldownDuration = 8f;

        [Header("Input")]
        public KeyCode sabotageKey = KeyCode.F;

        [Header("Suspicion")]
        public PlayerSuspicion playerSuspicion;

        [SerializeField] private GameObject Sabotage_ui;

        // internal
        bool playerInRange = false;
        bool isOnCooldown = false;
        Coroutine routine;

        private void Start()
        {
            if (Sabotage_ui != null)
                Sabotage_ui.SetActive(false);

            // Door starts OPEN
            if (doorObject != null)
                doorObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = true;
            if (Sabotage_ui != null)
                Sabotage_ui.SetActive(true);

            // mark THIS door as the active one
            activeDoor = this;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = false;
            if (Sabotage_ui != null)
                Sabotage_ui.SetActive(false);

            // clear only if we’re leaving this door
            if (activeDoor == this)
                activeDoor = null;
        }

        private void Update()
        {
            // keyboard version
            if (!playerInRange) return;
            if (isOnCooldown) return;

            if (Input.GetKeyDown(sabotageKey))
                StartSabotage();
        }

        // instance-level sabotage start (used by keyboard & button)
        void StartSabotage()
        {
            if (routine != null) return;
            routine = StartCoroutine(SabotageRoutine());
        }

        // 🎮 UI button calls THIS (on ANY DoorSabotage in the scene)
        public void SabotageButtonPressed()
        {
            // use the door the player is currently near
            if (activeDoor == null) return;
            if (activeDoor.isOnCooldown) return;
            if (activeDoor.routine != null) return;

            activeDoor.routine = activeDoor.StartCoroutine(activeDoor.SabotageRoutine());
        }

        IEnumerator SabotageRoutine()
        {
            isOnCooldown = true;

            //// Max suspicion
            //if (playerSuspicion != null)
            //{
            //    float add = playerSuspicion.maxSuspicion - playerSuspicion.suspicion;
            //    if (add > 0f)
            //        playerSuspicion.suspicion += add;
            //}

            // CLOSE
            if (doorObject != null)
                doorObject.SetActive(true);

            yield return new WaitForSeconds(closedDuration);

            // OPEN
            if (doorObject != null)
                doorObject.SetActive(false);

            yield return new WaitForSeconds(cooldownDuration);

            isOnCooldown = false;
            routine = null;
        }
    }
}
