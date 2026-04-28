using UnityEngine;

public class AttackState : IState
{
    private readonly NPCController npc;
    private float originalSpeed;

    private enum Phase { Pursuing, SearchingLastPos, Returning }
    private Phase phase;

    private float searchTimer;
    private float lostSightTimer;

    private const float LostSightGrace = 0.5f;
    private const float SearchDuration = 4f;
    private const float ArrivalTolerance = 0.8f;

    public Vector3 LastKnownPosition { get; private set; }
    public bool IsSearchingLastPos => phase == Phase.SearchingLastPos;

    public AttackState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed *= npc.combatSpeedMultiplier;
        phase = Phase.Pursuing;
        lostSightTimer = 0f;
        searchTimer = 0f;
        LastKnownPosition = npc.player.position;

        npc.OnPlayerDetected?.Invoke();
        Debug.Log($"[{npc.name}] → ATTACK");
    }

    public void OnUpdate()
    {
        switch (phase)
        {
            case Phase.Pursuing: UpdatePursuing(); break;
            case Phase.SearchingLastPos: UpdateSearching(); break;
            case Phase.Returning: UpdateReturning(); break;
        }
    }

    public void OnExit()
    {
        npc.maxSpeed = originalSpeed;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
    }

    private void UpdatePursuing()
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

            npc.MoveToward(LastKnownPosition);
            npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);

            if (lostSightTimer >= LostSightGrace)
            {
                phase = Phase.SearchingLastPos;
                searchTimer = 0f;
                Debug.Log($"[{npc.name}] Player perdido → buscando en última posición");
            }
        }
    }

    private void UpdateSearching()
    {
        bool arrived = npc.ReachedPosition(LastKnownPosition, ArrivalTolerance);

        if (!arrived)
        {
            npc.MoveToward(LastKnownPosition);
            npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);
        }
        else
        {
            npc.StopAgent();
            npc.SetAnimatorSpeed(0f);
            searchTimer += Time.deltaTime;
        }

        if (npc.PlayerVisible)
        {
            phase = Phase.Pursuing;
            lostSightTimer = 0f;
            Debug.Log($"[{npc.name}] ¡Player reencontrado! Reanudando persecución");
            return;
        }

        if (searchTimer >= SearchDuration)
        {
            phase = Phase.Returning;
            Debug.Log($"[{npc.name}] Búsqueda agotada → volviendo a patrulla");
        }
    }

    private void UpdateReturning()
    {
        if (npc.PlayerVisible)
        {
            phase = Phase.Pursuing;
            lostSightTimer = 0f;
            Debug.Log($"[{npc.name}] ¡Player reencontrado durante retorno! Persiguiendo");
            return;
        }

        int lastWP = npc.LastPatrolWaypointIndex;
        if (lastWP < 0 || npc.waypoints == null || lastWP >= npc.waypoints.Length)
        {
            npc.TransitionTo(NPCStateID.Patrol);
            return;
        }

        Vector3 returnTarget = npc.waypoints[lastWP].position;
        npc.MoveToward(returnTarget);
        npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);

        if (npc.ReachedPosition(returnTarget, ArrivalTolerance))
        {
            Debug.Log($"[{npc.name}] Llegó al waypoint de retorno → PATROL");
            npc.TransitionTo(NPCStateID.Patrol);
        }
    }
}