using mygame;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("UI/Suspicion World UI")]
public class SusPicionUI : MonoBehaviour
{
    [Header("References")]
    public PlayerSuspicion player;
    public Image barFill;     // prefer Filled type (Horizontal)
    public TMP_Text label;    // TextMeshPro label

    [Header("Positioning")]
    [Tooltip("Vertical offset above player's pivot in world units.")]
    public float verticalOffset = 2.2f;
    [Tooltip("Use this to smoothly follow player (0 = snap).")]
    public float followSmooth = 8f;

    [Header("Behavior")]
    [Tooltip("If true, UI will hide when suspicion is near zero.")]
    public bool hideWhenEmpty = false;
    [Range(0f, 1f)]
    public float hideThreshold = 0.02f;

    Camera cam;
    RectTransform barRect;     // for scale fallback
    bool useFilledImage = false;

    // store original Y/Z scale for scale fallback
    Vector3 originalBarScale = Vector3.one;
    Vector2 originalPivot;

    void Start()
    {
        cam = Camera.main;
        if (player == null)
            player = FindObjectOfType<PlayerSuspicion>();

        if (barFill != null)
        {
            barRect = barFill.rectTransform;

            // Try to force nice settings for left->right fill
            try
            {
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Horizontal;
                // Origin 0 = Left for Horizontal
                barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                useFilledImage = true;
            }
            catch
            {
                useFilledImage = (barFill.type == Image.Type.Filled);
            }

            // Save original transform state for scale fallback
            originalBarScale = barRect.localScale;
            originalPivot = barRect.pivot;

            // Ensure pivot is left for scale fallback (0,0.5) so scaling grows to right
            if (!useFilledImage)
            {
                barRect.pivot = new Vector2(0f, 0.5f);
            }
        }

        // If label not assigned but there's a TMP component on this object, auto-assign it
        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>();
        }
    }

    void LateUpdate()
    {
        // update visuals (keeps your commented camera/position logic untouched as you had it)
        float pct = 0f;
        if (player != null && player.maxSuspicion > 0f)
            pct = Mathf.Clamp01(player.suspicion / player.maxSuspicion);

        // fill handling:
        if (barFill != null)
        {
            if (useFilledImage)
            {
                // ensure fill amount and origin left-to-right
                barFill.fillAmount = pct;
                // safety
                barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            else
            {
                // fallback: scale the rect transform on X (assumes left-aligned anchor/pivot)
                float targetX = Mathf.Max(0.0001f, pct);
                Vector3 s = barRect.localScale;
                s.x = Mathf.Lerp(s.x, targetX, Time.deltaTime * 20f);
                s.y = originalBarScale.y;
                s.z = originalBarScale.z;
                barRect.localScale = s;

                // ensure pivot is left
                barRect.pivot = new Vector2(0f, 0.5f);
            }
        }

        // label using TMP
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(pct * player.maxSuspicion) } %";
        }

        // hide when empty (optional)
        if (hideWhenEmpty)
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
            }
            cg.alpha = Mathf.Lerp(cg.alpha, pct <= hideThreshold ? 0f : 1f, Time.deltaTime * 8f);
            cg.interactable = cg.blocksRaycasts = cg.alpha > 0.5f;
        }
    }

    // Editor / runtime helper so other scripts can safely assign the player
    public void SetPlayer(PlayerSuspicion p)
    {
        player = p;
    }
}
