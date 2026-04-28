using System.Collections.Generic;
using UnityEngine;

public class RunAwayState : IState
{
    private readonly NPCController npc;
    private float originalSpeed;

    private Vector3? currentDestination;
    private bool headingToGuard;
    private float guardCheckTimer;

    private const float SpeedMultiplier = 1.4f;
    private const float SafeDistance = 12f;
    private const float GuardCheckDelay = 2f;   // segundos antes de buscar guardia
    private const float ArrivalTolerance = 1.2f;

    public RunAwayState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed = originalSpeed * SpeedMultiplier;
        headingToGuard = false;
        guardCheckTimer = 0f;

        // Ir al waypoint más cercano
        currentDestination = GetNearestWaypoint();
        Debug.Log($"[{npc.name}] → RUNAWAY (waypoint más cercano)");
    }

    public void OnUpdate()
    {
        float distToPlayer = Vector3.Distance(npc.transform.position, npc.player.position);
        guardCheckTimer += Time.deltaTime;

        // Si el player sigue cerca después del delay, buscar guardia
        if (!headingToGuard && guardCheckTimer >= GuardCheckDelay && distToPlayer < SafeDistance)
        {
            Vector3? guardPos = GetNearestGuardPosition();
            if (guardPos.HasValue)
            {
                currentDestination = guardPos.Value;
                headingToGuard = true;
                Debug.Log($"[{npc.name}] RUNAWAY → yendo hacia guardia más cercano");
            }
        }

        // Moverse: Evade + Seek al destino
        npc.EvadePlayer(currentDestination);

        float speed = npc.Velocity.magnitude / npc.maxSpeed;
        npc.SetAnimatorSpeed(speed);

        // Condicion de salida: llegó al destino y el player está lejos o sin LOS
        bool arrivedAtDest = currentDestination.HasValue &&
                             npc.ReachedPosition(currentDestination.Value, ArrivalTolerance);

        if (arrivedAtDest && (distToPlayer >= SafeDistance || !npc.PlayerVisible))
        {
            npc.TransitionTo(NPCStateID.Patrol);
            return;
        }

        // Condición de salida fallback: muy lejos y sin LOS
        if (distToPlayer >= SafeDistance && !npc.PlayerVisible)
        {
            npc.TransitionTo(NPCStateID.Patrol);
        }
    }

    public void OnExit()
    {
        npc.maxSpeed = originalSpeed;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
        npc.StartRunAwayCooldown();
        Debug.Log($"[{npc.name}] Escape completado. Cooldown activado.");
    }

    private Vector3? GetNearestWaypoint()
    {
        if (npc.waypoints == null || npc.waypoints.Length == 0) return null;

        var candidates = new List<Vector3>(npc.waypoints.Length);
        foreach (var wp in npc.waypoints) candidates.Add(wp.position);

        int idx = RouletteWheelSelector.SelectClosest(npc.transform.position, candidates);
        return npc.waypoints[idx].position;
    }

    private Vector3? GetNearestGuardPosition()
    {
        NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        Vector3? bestPos = null;

        foreach (var other in allNPCs)
        {
            if (other == npc) continue;
            if (other.enemyType != EnemyType.Guard) continue;

            float dist = Vector3.Distance(npc.transform.position, other.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = other.transform.position;
            }
        }

        return bestPos;
    }
}