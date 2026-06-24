using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineOfSight))]
public class FlockAgent : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3.5f;
    public float maxForce = 8f;

    [Header("Patrol al aislarse")]
    [Tooltip("Si está vacío, se usa automáticamente el recorrido del NPC más cercano")]
    public Transform[] waypoints;
    public float isolationThreshold = 5f;

    [Header("Chequeo de aislamiento")]
    [Tooltip("Radio dentro del cual se cuentan otros flocking agents")]
    public float isolationCheckRadius = 4f;
    [Tooltip("Cantidad mínima de agentes dentro del radio para NO considerarse aislado")]
    public int requiredNeighborCount = 2;
    [Tooltip("Mostrar el gizmo de aislamiento siempre, no solo al seleccionar")]
    public bool alwaysShowGizmo = true;

    [Header("Obstacle Avoidance (patrol)")]
    public float avoidanceDistance = 3.5f;
    public float avoidanceRadius = 0.45f;
    public LayerMask obstacleLayer;

    private Rigidbody rb;
    private LineOfSight los;
    private StateMachine fsm;

    private FlockNormalState normalState;
    private FlockScatterState scatterState;
    private FlockPatrolState patrolState;

    public Vector3 Velocity => rb.linearVelocity;
    public bool CanSeePlayer { get; private set; }

    public bool IsIsolated { get; private set; }
    public int CurrentWaypointIndex { get; set; }
    public bool PatrolForward { get; set; } = true;
    public int CurrentNeighborCount { get; private set; }

    private float isolationTimer;
    private bool searchedForWaypoints;
    private bool waypointsBorrowed;

    private List<Vector3> currentPath;
    private int pathIndex;
    private float pathRefreshTimer;
    private Vector3 lastPathTarget;
    private const float PathRefreshInterval = 0.4f;
    private const float PathRefreshMoveDist = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        los = GetComponent<LineOfSight>();

        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        normalState = new FlockNormalState(this);
        scatterState = new FlockScatterState(this);
        patrolState = new FlockPatrolState(this);

        fsm = new StateMachine();
        fsm.ChangeState(normalState);
    }

    private void Update()
    {
        if (FlockManager.Instance == null) return;

        CanSeePlayer = los.HasLOS(FlockManager.Instance.Player);

        UpdateIsolation();

        IState desired = DecideState();
        fsm.ChangeState(desired);
        fsm.Update();

        pathRefreshTimer += Time.deltaTime;
    }

    private void UpdateIsolation()
    {
        CurrentNeighborCount = FlockManager.Instance.CountNeighborsNear(this, isolationCheckRadius);
        bool hasEnoughNeighbors = CurrentNeighborCount >= requiredNeighborCount;

        bool wasIsolated = IsIsolated;

        if (hasEnoughNeighbors)
        {
            isolationTimer = 0f;
            IsIsolated = false;
            searchedForWaypoints = false;

            if (waypointsBorrowed)
            {
                waypoints = null;
                waypointsBorrowed = false;
            }
        }
        else
        {
            isolationTimer += Time.deltaTime;
            IsIsolated = isolationTimer >= isolationThreshold;

            if (IsIsolated && !searchedForWaypoints)
            {
                EnsureWaypoints();
                searchedForWaypoints = true;
            }
        }

        // Log de diagnóstico: confirma que CADA instancia corre su propia lógica
        if (wasIsolated != IsIsolated)
        {
            Debug.Log($"[{name}] IsIsolated cambió a {IsIsolated} | vecinos: {CurrentNeighborCount}/{requiredNeighborCount} | waypoints válidos: {(waypoints != null && waypoints.Length >= 2)}");
        }
    }
    private void EnsureWaypoints()
    {
        if (waypoints != null && waypoints.Length >= 2) return;

        NPCController[] allNPCs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);

        float bestDist = float.MaxValue;
        NPCController nearest = null;

        foreach (var npc in allNPCs)
        {
            if (npc.waypoints == null || npc.waypoints.Length < 2) continue;
            float dist = Vector3.Distance(transform.position, npc.transform.position);
            if (dist < bestDist) { bestDist = dist; nearest = npc; }
        }

        if (nearest != null)
        {
            waypoints = nearest.waypoints;
            waypointsBorrowed = true;
            Debug.Log($"[{name}] Aislado → tomando el recorrido de {nearest.name}");
        }
        else
        {
            Debug.LogWarning($"[{name}] Aislado, pero no hay NPCs con waypoints en la escena.");
        }
    }

    private IState DecideState()
    {
        if (FlockManager.Instance.PlayerSpotted) return scatterState;
        if (IsIsolated && waypoints != null && waypoints.Length >= 2) return patrolState;
        return normalState;
    }

    public void ApplyForce(Vector3 force)
    {
        force = Vector3.ClampMagnitude(force, maxForce);
        Vector3 newVel = rb.linearVelocity + force * Time.deltaTime;
        newVel.y = 0f;
        newVel = Vector3.ClampMagnitude(newVel, moveSpeed);
        rb.linearVelocity = newVel;

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            Quaternion rot = Quaternion.LookRotation(
                new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 8f);
        }
    }

    public void StopAgent()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public bool ReachedPosition(Vector3 target, float tolerance = 0.6f)
    {
        Vector3 a = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 b = new Vector3(target.x, 0, target.z);
        return Vector3.Distance(a, b) <= tolerance;
    }

    public bool HasClearPath(Vector3 target)
    {
        Vector3 flatTarget = FlattenToAgentHeight(target);
        Vector3 origin = transform.position;
        Vector3 dir = flatTarget - origin;

        if (dir.sqrMagnitude < 0.0001f) return true;

        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, obstacleLayer);
    }

    public void NavigateTo(Vector3 target)
    {
        Vector3 flatTarget = FlattenToAgentHeight(target);

        if (HasClearPath(flatTarget))
        {
            currentPath = null;
            MoveDirectly(flatTarget);
            return;
        }

        bool targetMoved = Vector3.Distance(flatTarget, lastPathTarget) > PathRefreshMoveDist;
        if (currentPath == null || pathRefreshTimer >= PathRefreshInterval || targetMoved)
        {
            currentPath = AStar.FindPath(transform.position, flatTarget);
            pathIndex = 0;
            pathRefreshTimer = 0f;
            lastPathTarget = flatTarget;
        }

        if (currentPath != null && currentPath.Count > 0)
        {
            while (pathIndex < currentPath.Count - 1 &&
                   ReachedPosition(currentPath[pathIndex], 0.6f))
                pathIndex++;

            MoveDirectly(FlattenToAgentHeight(currentPath[pathIndex]));
        }
        else
        {
            MoveDirectly(flatTarget);
        }
    }

    private Vector3 FlattenToAgentHeight(Vector3 target)
    {
        return new Vector3(target.x, transform.position.y, target.z);
    }

    private void MoveDirectly(Vector3 target)
    {
        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        Vector3 seek = SteeringBehaviours.Seek(
            transform.position, rb.linearVelocity, target, moveSpeed);

        Vector3 avoidance = SteeringBehaviours.ObstacleAvoidance(
            transform.position, moveDir, avoidanceDistance, avoidanceRadius,
            obstacleLayer, moveSpeed);

        ApplyForce(seek + avoidance);
    }

    private void OnDrawGizmos()
    {
        if (!alwaysShowGizmo) return;
        DrawIsolationGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (alwaysShowGizmo) return; 
        DrawIsolationGizmo();
    }

    private void DrawIsolationGizmo()
    {
        Gizmos.color = IsIsolated
            ? new Color(1f, 0.2f, 0.2f, 0.8f)
            : new Color(0.2f, 1f, 0.4f, 0.5f);

        Gizmos.DrawWireSphere(transform.position, isolationCheckRadius);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            $"{name}\n{CurrentNeighborCount}/{requiredNeighborCount} vecinos");
#endif
    }
}

public class FlockNormalState : IState
{
    private readonly FlockAgent agent;
    public FlockNormalState(FlockAgent agent) => this.agent = agent;

    public void OnEnter() { }

    public void OnUpdate()
    {
        var fm = FlockManager.Instance;
        if (fm == null) return;

        Vector3 sep = fm.GetSeparation(agent) * fm.separationWeight;
        Vector3 coh = fm.GetCohesion(agent) * fm.cohesionWeight;
        Vector3 ali = fm.GetAlignment(agent) * fm.alignmentWeight;

        agent.ApplyForce(sep + coh + ali);
    }

    public void OnExit() { }
}

public class FlockScatterState : IState
{
    private readonly FlockAgent agent;
    public FlockScatterState(FlockAgent agent) => this.agent = agent;

    public void OnEnter() { }

    public void OnUpdate()
    {
        var fm = FlockManager.Instance;
        if (fm?.Player == null) return;

        Vector3 flee = (agent.transform.position - fm.Player.position).normalized * agent.moveSpeed * 2f;
        Vector3 sep = fm.GetSeparation(agent) * fm.separationWeight;

        agent.ApplyForce(flee + sep);
    }

    public void OnExit() { }
}

public class FlockPatrolState : IState
{
    private readonly FlockAgent agent;

    public FlockPatrolState(FlockAgent agent) => this.agent = agent;

    public void OnEnter()
    {
        int closest = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < agent.waypoints.Length; i++)
        {
            float d = Vector3.Distance(agent.transform.position, agent.waypoints[i].position);
            if (d < bestDist) { bestDist = d; closest = i; }
        }

        agent.CurrentWaypointIndex = closest;
        Debug.Log($"[{agent.name}] Aislado → patrullando (waypoint {closest})");
    }

    public void OnUpdate()
    {
        if (agent.waypoints == null || agent.waypoints.Length < 2) return;

        Vector3 target = agent.waypoints[agent.CurrentWaypointIndex].position;
        agent.NavigateTo(target);

        if (agent.ReachedPosition(target))
            AdvanceWaypoint();
    }

    public void OnExit()
    {
        agent.StopAgent();
    }

    private void AdvanceWaypoint()
    {
        if (agent.PatrolForward)
        {
            if (agent.CurrentWaypointIndex >= agent.waypoints.Length - 1)
            {
                agent.PatrolForward = false;
                agent.CurrentWaypointIndex--;
            }
            else
            {
                agent.CurrentWaypointIndex++;
            }
        }
        else
        {
            if (agent.CurrentWaypointIndex <= 0)
            {
                agent.PatrolForward = true;
                agent.CurrentWaypointIndex++;
            }
            else
            {
                agent.CurrentWaypointIndex--;
            }
        }
    }
}