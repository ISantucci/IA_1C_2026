using System.Collections.Generic;
using UnityEngine;

public class RunAwayState : IState
{
    private enum EscapeOption
    {
        NearestWaypoint,
        FarthestFromPlayerWaypoint,
        NearestGuard
    }

    private readonly NPCController npc;
    private float originalSpeed;

    private Vector3? currentDestination;
    private bool headingToGuard;
    private float guardCheckTimer;

    private const float SpeedMultiplier = 1.4f;
    private const float SafeDistance = 12f;
    private const float GuardCheckDelay = 2f;  
    private const float ArrivalTolerance = 1.2f;

    public RunAwayState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed = originalSpeed * SpeedMultiplier;
        headingToGuard = false;
        guardCheckTimer = 0f;

        currentDestination = ChooseEscapeDestination();

        Debug.Log($"[{npc.name}] → RUNAWAY | Destino inicial: {(currentDestination.HasValue ? currentDestination.Value.ToString() : "SIN DESTINO")}");
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

                Debug.Log($"[{npc.name}] RUNAWAY Yendo hacia guardia más cercano");
            }
        }

        // Mover
        npc.EvadePlayer(currentDestination);

        float speed = npc.Velocity.magnitude / npc.maxSpeed;
        npc.SetAnimatorSpeed(speed);

        // player lejos o sin LOS
        bool arrivedAtDest = currentDestination.HasValue &&
                             npc.ReachedPosition(currentDestination.Value, ArrivalTolerance);

        if (arrivedAtDest && (distToPlayer >= SafeDistance || !npc.PlayerVisible))
        {
            npc.TransitionTo(NPCStateID.Patrol);
            return;
        }

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

    private Vector3? ChooseEscapeDestination()
    {
        float distToPlayer = Vector3.Distance(npc.transform.position, npc.player.position);
        Vector3? guardPos = GetNearestGuardPosition();

        // Pesos de la ruleta.
        List<float> weights = new List<float>
        {
            30f,                                      // NearestWaypoint
            distToPlayer < SafeDistance ? 60f : 25f, // FarthestFromPlayerWaypoint
            guardPos.HasValue ? 50f : 0f             // NearestGuard
        };

        int selectedIndex = RouletteWheelSelector.Select(weights);

        if (selectedIndex < 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | Ruleta no pudo elegir estrategia. Fallback: waypoint cercano.");
            return GetNearestWaypoint();
        }

        EscapeOption selectedOption = (EscapeOption)selectedIndex;

        Debug.Log(
            $"[{npc.name}] RUNAWAY | Ruleta eligió: {selectedOption} | " +
            $"Cercano: {weights[0]}, Lejano: {weights[1]}, Guardia: {weights[2]} | " +
            $"Distancia: {distToPlayer:F2}"
        );

        switch (selectedOption)
        {
            case EscapeOption.NearestWaypoint:
                // usa la ruleta ya existente para favorecer waypoints cercanos.
                Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar al waypoint cercano.");
                return GetNearestWaypoint();

            case EscapeOption.FarthestFromPlayerWaypoint:
                // waypoints alejados del jugador.
                Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar al waypoint más lejano del jugador.");
                return GetFarthestWaypointFromPlayer();

            case EscapeOption.NearestGuard:
                //  si hay guardia, escapa hacia él.
                if (guardPos.HasValue)
                {
                    headingToGuard = true;
                    Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar hacia el guardia más cercano.");
                    return guardPos.Value;
                }

                // vuelve a una opción segura.
                Debug.LogWarning($"[{npc.name}] RUNAWAY | Se eligió guardia, pero no hay guardia válido. Fallback: waypoint cercano.");
                return GetNearestWaypoint();

            default:
                Debug.LogWarning($"[{npc.name}] RUNAWAY | Estrategia desconocida. Fallback: waypoint cercano.");
                return GetNearestWaypoint();
        }
    }

    private Vector3? GetNearestWaypoint()
    {
        if (npc.waypoints == null || npc.waypoints.Length == 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No hay waypoints asignados.");
            return null;
        }

        var candidates = new List<Vector3>(npc.waypoints.Length);

        foreach (var wp in npc.waypoints)
        {
            if (wp != null)
                candidates.Add(wp.position);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | Todos los waypoints son null.");
            return null;
        }

        int idx = RouletteWheelSelector.SelectClosest(npc.transform.position, candidates);

        if (idx < 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No se pudo elegir waypoint cercano.");
            return null;
        }
        Debug.Log($"[{npc.name}] RUNAWAY | Waypoint cercano elegido por ruleta: índice {idx}");

        return candidates[idx];
    }

    private Vector3? GetFarthestWaypointFromPlayer()
    {
        if (npc.waypoints == null || npc.waypoints.Length == 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No hay waypoints asignados para elegir el más lejano.");
            return null;
        }

        var candidates = new List<Vector3>(npc.waypoints.Length);

        foreach (var wp in npc.waypoints)
        {
            if (wp != null)
                candidates.Add(wp.position);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | Todos los waypoints son null.");
            return null;
        }

        int idx = RouletteWheelSelector.SelectFarthestFrom(npc.player.position, candidates);

        if (idx < 0)
        {
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No se pudo elegir waypoint lejano del jugador.");
            return null;
        }
        // waypoint elegido.
        Debug.Log($"[{npc.name}] RUNAWAY | Waypoint lejano del jugador elegido por ruleta: índice {idx}");

        return candidates[idx];
    }

    private Vector3? GetNearestGuardPosition()
    {
        NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        Vector3? bestPos = null;

        string guardName = "";

        foreach (var other in allNPCs)
        {
            if (other == npc) continue;
            if (other.enemyType != EnemyType.Guard) continue;

            float dist = Vector3.Distance(npc.transform.position, other.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = other.transform.position;
                guardName = other.name;
            }
        }

        if (bestPos.HasValue)
        {
            Debug.Log($"[{npc.name}] RUNAWAY | Guardia cercano detectado: {guardName} | Distancia: {bestDist:F2}");
        }
        else
        {
            Debug.Log($"[{npc.name}] RUNAWAY | No guardia disponible.");
        }

        return bestPos;
    }
}