using System.Collections;
using UnityEngine;


namespace mygame
{
    public class PlayerSuspicion : MonoBehaviour
    {
        [Header("Area / Death area")]
        [Tooltip("If player stands still inside this area, suspicion rises.")]
        public Area activeArea;
        [Tooltip("Prefab Area (same class you used for NPC death). Instantiated when suspicion trips.")]
        public Area deathAreaPrefab;
        public float deathAreaRadius = 2f;
        public float deathAreaDestroyTime = 40f;

        [Header("Suspicion tuning")]
        [Range(0, 100)] public float suspicion = 0f;
        public float maxSuspicion = 100f;
        [Tooltip("When suspicion reaches this value the player leaves a death-area and cops are alerted.")]
        public float tripThreshold = 75f;
        [Tooltip("How fast suspicion rises per second while standing still in active area")]
        public float standIncreasePerSecond = 8f;
        [Tooltip("How fast suspicion decreases per second while dancing (or when outside active area)")]
        public float dancingDecreasePerSecond = 12f;
        [Tooltip("Small passive decay when outside active area")]
        public float outsideDecayPerSecond = 6f;

        [Header("Movement detection")]
        [Tooltip("CharacterController (optional). If null, script uses transform delta each check.")]
        public CharacterController characterController;
        [Tooltip("If squared movement delta is below this, considered still.")]
        public float stillMovementThreshold = 0.01f;

        [Header("Dancing detection")]
        [Tooltip("Assign Animator if you use an Animator bool to mark dancing. Leave blank to disable dance-based decay.")]
        public Animator playerAnimator;
        [Tooltip("Animator bool param name that indicates dancing (case sensitive).")]
        public string dancingParamName = "isDancing";

        [Header("Timing")]
        [Tooltip("How often (s) we update suspicion checks")]
        public float checkInterval = 0.2f;
        [Tooltip("Cooldown after leaving a death area — during this time player cannot create another death area")]
        public float leaveDeathCooldown = 20f;

        [Header("Debug UI")]
        public bool showDebugGUI = true;
        public Vector2 debugGuiPosition = new Vector2(10, 10);

        // internal
        float checkTimer = 0f;
        Vector3 lastPosition;
        bool canLeaveDeathArea = true;
        float lastLeaveTime = -999f;

        void Start()
        {
            lastPosition = transform.position;
            suspicion = Mathf.Clamp(suspicion, 0f, maxSuspicion);
            checkTimer = 0f;
        }

        void Update()
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = checkInterval;
                EvaluateSuspicionTick();
            }

            // cooldown reset
            if (!canLeaveDeathArea && Time.time - lastLeaveTime >= leaveDeathCooldown)
                canLeaveDeathArea = true;
        }

        void EvaluateSuspicionTick()
        {
            bool inActive = IsInActiveArea();
            bool isDancing = IsDancing();
            bool isStill = IsStill();

            // If in active area and standing still and not dancing -> increase suspicion
            if (inActive && isStill && !isDancing)
            {
                suspicion += standIncreasePerSecond * checkInterval;
            }
            else if (inActive && isDancing)
            {
                // dancing in active area => reduce suspicion
                suspicion -= dancingDecreasePerSecond * checkInterval;
            }
            else
            {
                // outside active area => passive decay (or dancing reduces faster)
                if (isDancing)
                    suspicion -= dancingDecreasePerSecond * checkInterval * 0.5f;
                else
                    suspicion -= outsideDecayPerSecond * checkInterval;
            }

            suspicion = Mathf.Clamp(suspicion, 0f, maxSuspicion);

            // Trip logic
            if (suspicion >= tripThreshold && canLeaveDeathArea)
            {
                CreatePlayerDeathAreaAndAlert();
            }

            lastPosition = transform.position;
        }

        bool IsInActiveArea()
        {
            if (activeArea == null) return false;
            // Assumes Area has a transform and Radius property (your NPC code used Area.Radius previously)
            // Fallback: if Area doesn't have Radius exposed, try to do a conservative distance check on bounds
            float r = 0f;
            try
            {
                r = activeArea.Radius; // will work if Area has Radius
            }
            catch { r = 0f; }

            if (r > 0f)
            {
                return Vector3.Distance(transform.position, activeArea.transform.position) <= r;
            }
            else
            {
                // fallback box check: use bounds of object's renderer/collider if present
                Collider c = activeArea.GetComponent<Collider>();
                if (c != null) return c.bounds.Contains(transform.position);
                Renderer rend = activeArea.GetComponent<Renderer>();
                if (rend != null) return rend.bounds.Contains(transform.position);
                // final fallback: approximate via 5 units
                return Vector3.Distance(transform.position, activeArea.transform.position) <= 5f;
            }
        }

        bool IsDancing()
        {
            if (playerAnimator == null) return false;
            if (string.IsNullOrEmpty(dancingParamName)) return false;

            // safe: check that parameter exists by trying to read it (Animator returns false if missing)
            try
            {
                return playerAnimator.GetBool(dancingParamName);
            }
            catch
            {
                return false;
            }
        }

        bool IsStill()
        {
            Vector3 delta = transform.position - lastPosition;
            return delta.sqrMagnitude <= 0.0001f; // small but safe
        }

        void CreatePlayerDeathAreaAndAlert()
        {
            canLeaveDeathArea = false;
            lastLeaveTime = Time.time;

            Vector3 areaPos = transform.position;

            // spawn death area prefab (same as NPC death)
            if (deathAreaPrefab != null)
            {
                Area newA = Instantiate(deathAreaPrefab, areaPos, Quaternion.identity);
                newA.Radius = deathAreaRadius;
                Destroy(newA.gameObject, deathAreaDestroyTime);
            }

            // ⭐ TRIGGER YOUR EXISTING NPC DEATH EVENT SYSTEM
            NPCWander.OnAnyNPCKilled?.Invoke(areaPos, this.transform);

            Debug.Log("Suspicion TRIPPED: Player left a death area and alerted NPCs.");
        }


        /// <summary>
        /// Call this externally when the player kills an NPC to add a big spike to suspicion.
        /// Example: Player stab code should call PlayerSuspicion.Instance.AddSuspicionOnKill(30f)
        /// </summary>
        public void AddSuspicionOnKill(float amount)
        {
            if (amount <= 0f) return;
            suspicion = Mathf.Clamp(suspicion + amount, 0f, maxSuspicion);

            // immediate trip check
            if (suspicion >= tripThreshold && canLeaveDeathArea)
            {
                CreatePlayerDeathAreaAndAlert();
            }
        }

        // Small debug GUI
        /*
        void OnGUI()
        {
            if (!showDebugGUI) return;

            // layout
            float w = 500f;
            float h = 500f;
            float x = debugGuiPosition.x;
            float y = debugGuiPosition.y;

            // background
            Color bgCol = new Color(0f, 0f, 0f, 0.65f);
            DrawRect(new Rect(x - 8, y - 8, w + 16, h + 24), bgCol);

            // title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.normal.textColor = Color.white;
            titleStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(x + 6, y - 4, w, 20), "SUSPICION", titleStyle);

            // info text
            GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
            infoStyle.fontSize = 13;
            infoStyle.normal.textColor = Color.white;

            string state = $"InActiveArea:{IsInActiveArea()}  Dancing:{IsDancing()}  Still:{IsStill()}";
            GUI.Label(new Rect(x + 8, y + 20, w - 16, 20), state, infoStyle);

            // numeric suspicion
            GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
            bigStyle.fontSize = 18;
            bigStyle.normal.textColor = suspicion >= tripThreshold ? Color.red : Color.yellow;
            bigStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(x + 8, y + 40, w - 16, 26), $"Suspicion: {suspicion:F0} / {maxSuspicion:F0}", bigStyle);

            // progress bar background
            Rect barBg = new Rect(x + 8, y + 70, w - 16, 20);
            DrawRect(barBg, new Color(0.15f, 0.15f, 0.15f, 1f));

            // fill
            float pct = Mathf.Clamp01(suspicion / maxSuspicion);
            Rect fill = new Rect(barBg.x + 2, barBg.y + 2, (barBg.width - 4) * pct, barBg.height - 4);
            Color fillColor = suspicion >= tripThreshold ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.2f, 0.8f, 1f, 1f);
            DrawRect(fill, fillColor);

            // cooldown / death-area hint
            GUIStyle small = new GUIStyle(GUI.skin.label);
            small.fontSize = 11;
            small.normal.textColor = Color.grey;
            string cd = canLeaveDeathArea ? "Death-area ready" : $"Death cooldown: {Mathf.Max(0f, leaveDeathCooldown - (Time.time - lastLeaveTime)):0}s";
            GUI.Label(new Rect(x + 8, y + 96, w - 16, 16), cd, small);
        }

        // helper (simple colored rect using tiny texture)
        private static Texture2D _guiTex;
        private void DrawRect(Rect r, Color col)
        {
            if (_guiTex == null)
            {
                _guiTex = new Texture2D(1, 1);
                _guiTex.SetPixel(0, 0, Color.white);
                _guiTex.Apply();
            }
            GUI.color = col;
            GUI.DrawTexture(r, _guiTex);
            GUI.color = Color.white;
        }
        */

    }
}
