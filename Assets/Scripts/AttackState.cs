using UnityEngine;

public class AttackState : IState
{
    private readonly NPCController npc;
    private float originalSpeed;
    private float lostSightTimer;

    private const float LostSightGrace = 0.4f;

    public Vector3 LastKnownPosition { get; private set; }

    public AttackState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed *= npc.combatSpeedMultiplier;
        lostSightTimer = 0f;
        LastKnownPosition = npc.player.position;

        npc.OnPlayerDetected?.Invoke();
        Debug.Log($"[{npc.name}] → ATTACK");
    }

    public void OnUpdate()
    {
        if (npc.PlayerVisible)
        {
            LastKnownPosition = npc.player.position;
            lostSightTimer = 0f;

            float dist = Vector3.Distance(npc.transform.position, npc.player.position);
            if (dist <= npc.attackRange)
            {
                npc.StopAgent();
                npc.SetAnimatorSpeed(0f);
                npc.TriggerAttackAnimation();
                npc.OnAttackPlayer?.Invoke(npc);
                return;
            }

            npc.PursuePlayer();
            npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            npc.NavigateTo(LastKnownPosition);
            npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);

            if (lostSightTimer >= LostSightGrace)
            {
                npc.LastKnownPlayerPosition = LastKnownPosition;
                npc.TransitionTo(NPCStateID.Search);
            }
        }
    }

    public void OnExit()
    {
        npc.maxSpeed = originalSpeed;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
    }
}