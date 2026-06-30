using UnityEngine;

/// <summary>
/// Estado de reacción para Guards. Se activa al detectar al Player desde Patrol/Idle.
/// El Guard se frena y se orienta hacia el estímulo durante un instante corto antes
/// de comprometerse a perseguir. No persigue, no navega, no usa A*: solo reacción.
/// Salida: si sigue viendo al Player → Attack; si perdió visión → Search.
/// </summary>
public class AlertState : IState
{
    private readonly NPCController npc;
    private float timer;

    public AlertState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        timer = 0f;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);

        if (npc.PlayerVisible && npc.player != null)
            npc.LastKnownPlayerPosition = npc.player.position;

        Debug.Log($"[{npc.name}] → ALERT");
    }

    public void OnUpdate()
    {
        timer += Time.deltaTime;

        // Mantenerse frenado (sin steering ni navegación)
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);

        // Orientación: hacia el Player si lo ve, si no hacia la última posición conocida
        Vector3 lookTarget;
        if (npc.PlayerVisible && npc.player != null)
        {
            npc.LastKnownPlayerPosition = npc.player.position;
            lookTarget = npc.player.position;
        }
        else
        {
            lookTarget = npc.LastKnownPlayerPosition;
        }

        FaceToward(lookTarget);

        if (timer >= npc.AlertDuration)
        {
            if (npc.PlayerVisible)
                npc.TransitionTo(NPCStateID.Attack);
            else
                npc.TransitionTo(NPCStateID.Search);
        }
    }

    public void OnExit() { }

    private void FaceToward(Vector3 target)
    {
        Vector3 dir = target - npc.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        npc.transform.rotation = Quaternion.Slerp(
            npc.transform.rotation, rot, Time.deltaTime * 10f);
    }
}
