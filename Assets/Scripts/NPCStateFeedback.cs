using UnityEngine;

/// <summary>
/// Feedback de estado en runtime por color, visible en Game View / build.
/// Lee NPCController.CurrentStateID y tiñe un Renderer vía MaterialPropertyBlock
/// (no instancia materiales, no rompe batching, no toca la FSM).
/// </summary>
[DisallowMultipleComponent]
public class NPCStateFeedback : MonoBehaviour
{
    [Header("Colores por estado")]
    public Color patrolColor  = new Color(0.20f, 0.50f, 1.00f); // azul
    public Color idleColor    = new Color(0.60f, 0.60f, 0.60f); // gris
    public Color alertColor   = Color.cyan;                     // cian
    public Color attackColor  = new Color(1.00f, 0.15f, 0.15f); // rojo
    public Color runAwayColor = new Color(1.00f, 0.90f, 0.10f); // amarillo
    public Color searchColor  = new Color(1.00f, 0.50f, 0.00f); // naranja

    [Tooltip("Si está vacío, se autodetecta un Renderer en este objeto o sus hijos.")]
    [SerializeField] private Renderer targetRenderer;

    private NPCController npc;
    private MaterialPropertyBlock mpb;
    private int baseColorId;
    private int colorId;
    private bool hasBaseColor;
    private bool hasColor;
    private bool disabled;

    private NPCStateID lastApplied;
    private bool firstApply = true;

    private void Awake()
    {
        // NPCController en este objeto o en un parent
        npc = GetComponent<NPCController>();
        if (npc == null) npc = GetComponentInParent<NPCController>();

        // Renderer en este objeto o en hijos
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();

        if (npc == null || targetRenderer == null)
        {
            Debug.LogWarning(
                $"[NPCStateFeedback] en '{name}': " +
                (npc == null ? "no se encontró NPCController. " : "") +
                (targetRenderer == null ? "no se encontró Renderer. " : "") +
                "Feedback de color desactivado (gameplay sigue normal).", this);
            disabled = true;
            enabled = false;
            return;
        }

        mpb = new MaterialPropertyBlock();
        baseColorId = Shader.PropertyToID("_BaseColor"); // URP/Lit
        colorId = Shader.PropertyToID("_Color");         // Standard / fallback

        Material mat = targetRenderer.sharedMaterial;
        hasBaseColor = mat != null && mat.HasProperty(baseColorId);
        hasColor = mat != null && mat.HasProperty(colorId);

        if (!hasBaseColor && !hasColor)
        {
            Debug.LogWarning(
                $"[NPCStateFeedback] en '{name}': el material del Renderer no tiene " +
                "'_BaseColor' ni '_Color'. No se puede aplicar color. Feedback desactivado.", this);
            disabled = true;
            enabled = false;
        }
    }

    private void Update()
    {
        if (disabled) return;

        NPCStateID state = npc.CurrentStateID;
        if (!firstApply && state == lastApplied) return; // solo al cambiar

        ApplyColor(ColorForState(state));
        lastApplied = state;
        firstApply = false;
    }

    private Color ColorForState(NPCStateID state) => state switch
    {
        NPCStateID.Patrol  => patrolColor,
        NPCStateID.Idle    => idleColor,
        NPCStateID.Alert   => alertColor,
        NPCStateID.Attack  => attackColor,
        NPCStateID.RunAway => runAwayColor,
        NPCStateID.Search  => searchColor,
        _ => Color.white
    };

    private void ApplyColor(Color c)
    {
        targetRenderer.GetPropertyBlock(mpb);
        if (hasBaseColor) mpb.SetColor(baseColorId, c);
        if (hasColor) mpb.SetColor(colorId, c);
        targetRenderer.SetPropertyBlock(mpb);
    }
}
