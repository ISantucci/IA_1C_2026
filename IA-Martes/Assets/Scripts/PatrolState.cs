using System.Collections.Generic;
using UnityEngine;

public class PatrolState : IState
{
    private readonly NPCController npc;
    private bool waypointsValid;

    public PatrolState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        waypointsValid = npc.waypoints != null && npc.waypoints.Length >= 2;
        if (!waypointsValid)
        {
            Debug.LogWarning($"[{npc.name}] PatrolState: asignar al menos 2 waypoints.");
            return;
        }

        var candidates = new List<Vector3>(npc.waypoints.Length);
        foreach (var wp in npc.waypoints) candidates.Add(wp.position);

        int startIdx = RouletteWheelSelector.SelectClosest(npc.transform.position, candidates);
        npc.CurrentWaypointIndex = startIdx;
        npc.IsIdlePending = false;

        Debug.Log($"[{npc.name}] → PATROL (inicio en waypoint {startIdx})");
    }

    public void OnUpdate()
    {
        if (!waypointsValid) return;

        Vector3 target = npc.waypoints[npc.CurrentWaypointIndex].position;
        npc.MoveToward(target);

        if (npc.ReachedPosition(target))
            AdvanceWaypoint();
    }

    public void OnExit()
    {
        npc.StopAgent();
    }

    private void AdvanceWaypoint()
    {
        if (npc.PatrolForward)
        {
            if (npc.CurrentWaypointIndex >= npc.waypoints.Length - 1)
            {
                npc.PatrolForward = false;
                npc.CurrentWaypointIndex--;
                OnCycleComplete();
            }
            else
            {
                npc.CurrentWaypointIndex++;
            }
        }
        else
        {
            if (npc.CurrentWaypointIndex <= 0)
            {
                npc.PatrolForward = true;
                npc.CurrentWaypointIndex++;
                OnCycleComplete();
            }
            else
            {
                npc.CurrentWaypointIndex--;
            }
        }
    }

    private void OnCycleComplete()
    {
        npc.PatrolCycleCount++;
        Debug.Log($"[{npc.name}] Ciclo #{npc.PatrolCycleCount}");

        if (npc.PatrolCycleCount >= npc.idleAfterPatrolCycles)
        {
            npc.PatrolCycleCount = 0;
            npc.TransitionTo(NPCStateID.Idle);
        }
    }
}