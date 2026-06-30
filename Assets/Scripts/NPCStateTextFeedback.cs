using UnityEngine;

/// <summary>
/// Feedback de estado por TEXTO sobre la cabeza del NPC, visible en Game View / build.
/// Lee NPCController.CurrentStateID y lo muestra en world-space usando TextMesh built-in
/// (sin TextMeshPro, sin Canvas, sin Gizmos).
///
/// - Crea automáticamente un hijo "StateLabel" en runtime: solo hay que agregar
///   este componente al GameObject/prefab del NPC.
/// - Solo actualiza el texto cuando cambia el estado.
/// - Hace billboard hacia Camera.main, sin texto espejado.
/// - No modifica la FSM ni la lógica de IA (solo lee CurrentStateID).
/// </summary>
[DisallowMultipleComponent]
public class NPCStateTextFeedback : MonoBehaviour
{
    [Header("Posición y tamaño")]
    [Tooltip("Desplazamiento del texto respecto del NPC.")]
    public Vector3 offset = new Vector3(0f, 2.2f, 0f);
    [Tooltip("Tamaño de cada caracter en el mundo (escala física del texto).")]
    public float characterSize = 0.2f;
    [Tooltip("Resolución de la fuente. Más alto = más nítido.")]
    public int fontSize = 64;

    [Header("Color")]
    public Color textColor = Color.white;

    private NPCController npc;
    private Transform label;
    private TextMesh textMesh;
    private Camera cam;

    private NPCStateID lastState;
    private bool firstApply = true;
    private bool disabled;
    private bool warnedNoCamera;

    private void Awake()
    {
        npc = GetComponent<NPCController>();
        if (npc == null) npc = GetComponentInParent<NPCController>();

        if (npc == null)
        {
            Debug.LogWarning(
                $"[NPCStateTextFeedback] en '{name}': no se encontró NPCController. " +
                "Feedback de texto desactivado (gameplay sigue normal).", this);
            disabled = true;
            enabled = false;
            return;
        }

        CreateLabel();
    }

    private void CreateLabel()
    {
        // Reutiliza el hijo si ya existe; si no, lo crea.
        Transform existing = transform.Find("StateLabel");
        GameObject go = existing != null ? existing.gameObject : new GameObject("StateLabel");

        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        go.transform.localScale = Vector3.one;
        label = go.transform;

        textMesh = go.GetComponent<TextMesh>();
        if (textMesh == null) textMesh = go.AddComponent<TextMesh>();

        // Fuente built-in de Unity (no requiere importar nada).
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            textMesh.font = font;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = textColor;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = fontSize;
        textMesh.text = string.Empty;
    }

    private void LateUpdate()
    {
        if (disabled || label == null) return;

        // Actualizar texto solo al cambiar de estado.
        NPCStateID state = npc.CurrentStateID;
        if (firstApply || state != lastState)
        {
            textMesh.text = state.ToString();
            textMesh.color = textColor;
            lastState = state;
            firstApply = false;
        }

        // Mantener la posición arriba del NPC.
        label.position = transform.position + offset;

        // Billboard hacia la cámara (sin espejar el texto).
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            if (!warnedNoCamera)
            {
                Debug.LogWarning(
                    $"[NPCStateTextFeedback] en '{name}': no hay Camera.main (tag 'MainCamera'). " +
                    "El texto no rota hacia la cámara, pero existe y el gameplay sigue normal.", this);
                warnedNoCamera = true;
            }
            return;
        }

        // El texto debe "mirar" en la dirección que va de la cámara al label;
        // así no queda espejado.
        Vector3 dir = label.position - cam.transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            label.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
