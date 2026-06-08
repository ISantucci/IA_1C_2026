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

        npc.IsIdlePending = false;

        if (npc.LastPatrolWaypointIndex >= 0)
        {
            npc.CurrentWaypointIndex = npc.LastPatrolWaypointIndex;
            Debug.Log($"[{npc.name}] → PATROL (retomando waypoint {npc.CurrentWaypointIndex})");
        }
        else
        {
            int startIdx = SelectStartWaypoint();
            if (npc.ReachedPosition(npc.waypoints[startIdx].position))
                startIdx = (startIdx + 1) % npc.waypoints.Length;
            npc.CurrentWaypointIndex = startIdx;
            Debug.Log($"[{npc.name}] → PATROL (waypoint {startIdx})");
        }

        npc.LastPatrolWaypointIndex = -1;
    }

    public void OnUpdate()
    {
        if (!waypointsValid) return;

        Vector3 target = npc.waypoints[npc.CurrentWaypointIndex].position;
        npc.NavigateTo(target);
        npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);

        if (npc.ReachedPosition(target))
        {
            npc.RegisterWaypointVisit(npc.CurrentWaypointIndex);
            AdvanceWaypoint();
        }
    }

    public void OnExit()
    {
        npc.LastPatrolWaypointIndex = npc.CurrentWaypointIndex;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
    }

    private int SelectStartWaypoint()
    {
        var positions = new List<Vector3>(npc.waypoints.Length);
        var visitTimes = new List<float>(npc.waypoints.Length);
        for (int i = 0; i < npc.waypoints.Length; i++)
        {
            positions.Add(npc.waypoints[i].position);
            visitTimes.Add(npc.GetLastVisitTime(i));
        }
        return RouletteWheelSelector.SelectWithVisitHistory(
            npc.transform.position, positions, visitTimes);
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
            else npc.CurrentWaypointIndex++;
        }
        else
        {
            if (npc.CurrentWaypointIndex <= 0)
            {
                npc.PatrolForward = true;
                npc.CurrentWaypointIndex++;
                OnCycleComplete();
            }
            else npc.CurrentWaypointIndex--;
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