using System.Collections.Generic;
using UnityEngine;

public class RunAwayState : IState
{
    // NUEVO:
    // Estas son las 3 opciones posibles que puede elegir la ruleta.
    // No reemplazan al Decision Tree: solo definen COMO va a escapar el NPC
    // una vez que el árbol ya decidió entrar en RunAway.
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
    private const float GuardCheckDelay = 2f;   // segundos antes de buscar guardia
    private const float ArrivalTolerance = 1.2f;

    public RunAwayState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        originalSpeed = npc.maxSpeed;
        npc.maxSpeed = originalSpeed * SpeedMultiplier;
        headingToGuard = false;
        guardCheckTimer = 0f;

        // NUEVO:
        // Antes siempre iba al waypoint más cercano.
        // Ahora se elige una estrategia de escape usando Roulette Wheel Selection.
        currentDestination = ChooseEscapeDestination();

        // NUEVO:
        // Verificación por consola para saber qué destino inicial quedó asignado.
        Debug.Log($"[{npc.name}] → RUNAWAY | Destino inicial: {(currentDestination.HasValue ? currentDestination.Value.ToString() : "SIN DESTINO")}");
    }

    public void OnUpdate()
    {
        float distToPlayer = Vector3.Distance(npc.transform.position, npc.player.position);
        guardCheckTimer += Time.deltaTime;

        // Si el player sigue cerca después del delay, buscar guardia
        // NUEVO:
        // Esto queda como respaldo si la ruleta no eligió guardia de entrada.
        // Por ejemplo, si eligió waypoint cercano pero el jugador sigue encima,
        // puede cambiar hacia el guardia más cercano.
        if (!headingToGuard && guardCheckTimer >= GuardCheckDelay && distToPlayer < SafeDistance)
        {
            Vector3? guardPos = GetNearestGuardPosition();

            if (guardPos.HasValue)
            {
                currentDestination = guardPos.Value;
                headingToGuard = true;

                // NUEVO:
                // Log para comprobar que cambió de estrategia durante la huida.
                Debug.Log($"[{npc.name}] RUNAWAY | Cambio de estrategia por peligro: yendo hacia guardia más cercano");
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

    // NUEVO:
    // Este método es el agregado principal.
    // Usa RouletteWheelSelector para elegir entre 3 estrategias de escape:
    // 1. Ir a un waypoint cercano.
    // 2. Ir a un waypoint lejos del jugador.
    // 3. Ir hacia el guardia más cercano.
    private Vector3? ChooseEscapeDestination()
    {
        float distToPlayer = Vector3.Distance(npc.transform.position, npc.player.position);
        Vector3? guardPos = GetNearestGuardPosition();

        // NUEVO:
        // Pesos de la ruleta.
        // Mientras más alto el peso, más probable es que esa opción sea elegida.
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

        // NUEVO:
        // Log importante para la defensa.
        // Muestra qué opción eligió la ruleta y con qué pesos.
        Debug.Log(
            $"[{npc.name}] RUNAWAY | Ruleta eligió: {selectedOption} | " +
            $"Pesos => Cercano: {weights[0]}, LejanoPlayer: {weights[1]}, Guardia: {weights[2]} | " +
            $"Distancia al player: {distToPlayer:F2}"
        );

        switch (selectedOption)
        {
            case EscapeOption.NearestWaypoint:
                // NUEVO:
                // Estrategia 1: usa la ruleta ya existente para favorecer waypoints cercanos.
                Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar al waypoint cercano.");
                return GetNearestWaypoint();

            case EscapeOption.FarthestFromPlayerWaypoint:
                // NUEVO:
                // Estrategia 2: favorece waypoints alejados del jugador.
                Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar al waypoint más lejano del jugador.");
                return GetFarthestWaypointFromPlayer();

            case EscapeOption.NearestGuard:
                // NUEVO:
                // Estrategia 3: si hay guardia, escapa hacia él.
                if (guardPos.HasValue)
                {
                    headingToGuard = true;
                    Debug.Log($"[{npc.name}] RUNAWAY | Estrategia aplicada: escapar hacia el guardia más cercano.");
                    return guardPos.Value;
                }

                // NUEVO:
                // Seguridad: si por alguna razón se eligió guardia pero no hay posición válida,
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
            // NUEVO:
            // Validación agregada para evitar errores si no hay waypoints asignados.
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No hay waypoints asignados.");
            return null;
        }

        var candidates = new List<Vector3>(npc.waypoints.Length);

        foreach (var wp in npc.waypoints)
        {
            // NUEVO:
            // Validación por si algún waypoint del array está vacío en el Inspector.
            if (wp != null)
                candidates.Add(wp.position);
        }

        if (candidates.Count == 0)
        {
            // NUEVO:
            // Evita intentar elegir si todos los waypoints estaban en null.
            Debug.LogWarning($"[{npc.name}] RUNAWAY | Todos los waypoints son null.");
            return null;
        }

        int idx = RouletteWheelSelector.SelectClosest(npc.transform.position, candidates);

        if (idx < 0)
        {
            // NUEVO:
            // Seguridad extra por si la ruleta devuelve índice inválido.
            Debug.LogWarning($"[{npc.name}] RUNAWAY | No se pudo elegir waypoint cercano.");
            return null;
        }

        // NUEVO:
        // Log para verificar qué waypoint fue elegido.
        Debug.Log($"[{npc.name}] RUNAWAY | Waypoint cercano elegido por ruleta: índice {idx}");

        return candidates[idx];
    }

    // NUEVO:
    // Selecciona un waypoint favoreciendo los que están más lejos del jugador.
    // Ojo: no necesariamente elige siempre el más lejano, porque usa ruleta ponderada.
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

        // NUEVO:
        // Log para verificar qué waypoint fue elegido.
        Debug.Log($"[{npc.name}] RUNAWAY | Waypoint más lejano del jugador elegido por ruleta: índice {idx}");

        return candidates[idx];
    }

    private Vector3? GetNearestGuardPosition()
    {
        NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        Vector3? bestPos = null;

        // NUEVO:
        // Guardamos el nombre para poder mostrar en consola qué guardia eligió.
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

        // NUEVO:
        // Verificación por consola para saber si encontró guardia o no.
        if (bestPos.HasValue)
        {
            Debug.Log($"[{npc.name}] RUNAWAY | Guardia más cercano detectado: {guardName} | Distancia: {bestDist:F2}");
        }
        else
        {
            Debug.Log($"[{npc.name}] RUNAWAY | No se encontró ningún guardia disponible.");
        }

        return bestPos;
    }
}