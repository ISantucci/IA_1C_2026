using System.Collections.Generic;
using UnityEngine;

public class RunAwayState : IState
{
    private readonly NPCController npc;
    private Vector3? safeDestination;
    private float originalSpeed;

    private const float SafeDistance = 15f;
    private const float SpeedMultiplier = 1.4f;

    public RunAwayState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed = originalSpeed * SpeedMultiplier;

        safeDestination = null;
        if (npc.waypoints != null && npc.waypoints.Length > 0)
        {
            var candidates = new List<Vector3>(npc.waypoints.Length);
            foreach (var wp in npc.waypoints) candidates.Add(wp.position);

            int safeIdx = RouletteWheelSelector.SelectFarthestFrom(
                npc.player.position, candidates);
            safeDestination = npc.waypoints[safeIdx].position;

            Debug.Log($"[{npc.name}] → RUNAWAY (Evade + destino seguro: waypoint {safeIdx})");
        }
        else
        {
            Debug.Log($"[{npc.name}] → RUNAWAY (Evade puro)");
        }
    }

    public void OnUpdate()
    {
        npc.EvadePlayer(safeDestination);

        float dist = Vector3.Distance(npc.transform.position, npc.player.position);
        if (dist >= SafeDistance && !npc.PlayerVisible)
        {
            npc.TransitionTo(NPCStateID.Patrol);
        }
    }

    public void OnExit()
    {
        npc.maxSpeed = originalSpeed;
    }
}