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

        // Si viene de Attack, retomar desde el ultimo waypoint guardado
        if (npc.LastPatrolWaypointIndex >= 0)
        {
            npc.CurrentWaypointIndex = npc.LastPatrolWaypointIndex;
            Debug.Log($"[{npc.name}] → PATROL (retomando desde waypoint {npc.CurrentWaypointIndex})");
        }
        else
        {
            var candidates = new List<Vector3>(npc.waypoints.Length);
            foreach (var wp in npc.waypoints) candidates.Add(wp.position);
            int startIdx = RouletteWheelSelector.SelectClosest(npc.transform.position, candidates);

            if (npc.ReachedPosition(npc.waypoints[startIdx].position))
                startIdx = (startIdx + 1) % npc.waypoints.Length;

            npc.CurrentWaypointIndex = startIdx;
            Debug.Log($"[{npc.name}] → PATROL (inicio en waypoint {startIdx})");
        }

        npc.LastPatrolWaypointIndex = -1;
    }

    public void OnUpdate()
    {
        if (!waypointsValid) return;

        Vector3 target = npc.waypoints[npc.CurrentWaypointIndex].position;
        npc.MoveToward(target);

        float speed = npc.Velocity.magnitude / npc.maxSpeed;
        npc.SetAnimatorSpeed(speed);

        if (npc.ReachedPosition(target))
            AdvanceWaypoint();
    }

    public void OnExit()
    {
        // Guardar waypoint actual antes de salir
        npc.LastPatrolWaypointIndex = npc.CurrentWaypointIndex;
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
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