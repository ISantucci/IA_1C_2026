using UnityEngine;

public class AttackState : IState
{
    private readonly NPCController npc;
    private float lostSightTimer;
    private float originalSpeed;

    private const float LostSightTimeout = 4f;

    public AttackState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed *= npc.combatSpeedMultiplier;
        lostSightTimer = 0f;

        npc.OnPlayerDetected?.Invoke();
        Debug.Log($"[{npc.name}] → ATTACK (Pursuit activo)");
    }

    public void OnUpdate()
    {
        float dist = Vector3.Distance(npc.transform.position, npc.player.position);

        if (dist <= npc.attackRange)
        {
            npc.StopAgent();
            npc.OnAttackPlayer?.Invoke(npc);
            return;
        }

        npc.PursuePlayer();

        if (!npc.PlayerVisible)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= LostSightTimeout)
            {
                Debug.Log($"[{npc.name}] Jugador perdido. Volviendo a Patrol.");
                npc.TransitionTo(NPCStateID.Patrol);
            }
        }
        else
        {
            lostSightTimer = 0f;
        }
    }

    public void OnExit()
    {
        npc.maxSpeed = originalSpeed;
    }
}