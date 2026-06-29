using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Guard, Scout }
public enum NPCStateID { Patrol, Idle, RunAway, Attack, Search, Alert }

[RequireComponent(typeof(LineOfSight))]
public class NPCController : SteeringAgent
{
    [Header("Identidad del NPC")]
    public EnemyType enemyType = EnemyType.Guard;
    public string groupName = "Grupo A";

    [Header("Referencias")]
    public Transform player;
    public Transform[] waypoints;

    [Header("Patrol → Idle")]
    public int idleAfterPatrolCycles = 3;
    public float idleDuration = 4f;

    [Header("Combate")]
    public float attackRange = 1.8f;
    public float combatSpeedMultiplier = 1.5f;

    [Header("Scout Cooldown")]
    public float runAwayCooldownDuration = 5f;

    [Header("Alerta (solo Guards)")]
    [SerializeField] private float alertDuration = 0.75f;
    public float AlertDuration => alertDuration;

    public int CurrentWaypointIndex { get; set; }
    public int LastPatrolWaypointIndex { get; set; } = -1;
    public bool PatrolForward { get; set; } = true;
    public int PatrolCycleCount { get; set; }
    public float IdleTimer { get; set; }
    public bool IsIdlePending { get; set; }
    public Vector3 LastKnownPlayerPosition { get; set; }

    public LineOfSight LOS { get; private set; }
    public bool PlayerVisible => LOS != null && LOS.HasLOS(player);

    private StateMachine fsm;
    public NPCStateID CurrentStateID { get; private set; }

    private PatrolState patrolState;
    private IdleState idleState;
    private RunAwayState runAwayState;
    private AttackState attackState;
    private SearchState searchState;
    private AlertState alertState;

    private DecisionTree decisionTree;
    private Animator animator;

    private float runAwayCooldownTimer = 0f;
    public bool RunAwayCooldownActive => runAwayCooldownTimer > 0f;

    // ── A* ────────────────────────────────────────────────────────────────
    private List<Vector3> currentPath;
    private int pathIndex;
    private float pathRefreshTimer;
    private Vector3 lastPathTarget;
    private const float PathRefreshInterval = 0.4f;
    private const float PathRefreshMoveDist = 2f;

    // ── Visit history (Roulette Wheel dinámico) ───────────────────────────
    private float[] waypointVisitTimes;

    public System.Action OnPlayerDetected;
    public System.Action<NPCController> OnAttackPlayer;

    protected override void Awake()
    {
        base.Awake();
        LOS = GetComponent<LineOfSight>();
        animator = GetComponentInChildren<Animator>();

        patrolState = new PatrolState(this);
        idleState = new IdleState(this);
        runAwayState = new RunAwayState(this);
        attackState = new AttackState(this);
        searchState = new SearchState(this);
        alertState = new AlertState(this);

        fsm = new StateMachine();
        decisionTree = BuildDecisionTree();

        InitVisitHistory();
        TransitionTo(NPCStateID.Patrol);
    }

    private void InitVisitHistory()
    {
        int count = waypoints != null ? waypoints.Length : 0;
        waypointVisitTimes = new float[count];
        for (int i = 0; i < count; i++) waypointVisitTimes[i] = 0f;
    }

    public void RegisterWaypointVisit(int index)
    {
        if (waypointVisitTimes != null && index < waypointVisitTimes.Length)
            waypointVisitTimes[index] = Time.time;
    }

    public float GetLastVisitTime(int index)
    {
        if (waypointVisitTimes == null || index >= waypointVisitTimes.Length) return 0f;
        return waypointVisitTimes[index];
    }

    private void Update()
    {
        if (runAwayCooldownTimer > 0f) runAwayCooldownTimer -= Time.deltaTime;
        pathRefreshTimer += Time.deltaTime;

        decisionTree.Execute();
        SyncFSM();
        fsm.Update();
    }

    private void SyncFSM()
    {
        IState desired = GetStateInstance(CurrentStateID);
        if (fsm.CurrentState != desired)
            fsm.ChangeState(desired);
    }

    private DecisionTree BuildDecisionTree()
    {
        var doAlert = new ActionNode(() => TransitionTo(NPCStateID.Alert));
        var doRunAway = new ActionNode(() => TransitionTo(NPCStateID.RunAway));
        var doNothing = new ActionNode(() => { });

        // Guard: solo entra a Alert si viene de Patrol o Idle.
        // Si ya está en Alert/Attack/Search, no reinicia nada (esos estados
        // gestionan su propia salida).
        var onGuardVisible = new ConditionNode(
            () => CurrentStateID == NPCStateID.Patrol
               || CurrentStateID == NPCStateID.Idle,
            doAlert,
            doNothing
        );

        // Guard → Alert (con la guarda de arriba); Scout → RunAway directo (como antes).
        var onPlayerVisible = new ConditionNode(
            () => enemyType == EnemyType.Guard,
            onGuardVisible,
            doRunAway
        );

        var root = new ConditionNode(
            () => PlayerVisible
               && !RunAwayCooldownActive
               && CurrentStateID != NPCStateID.RunAway
               && CurrentStateID != NPCStateID.Search,
            onPlayerVisible,
            doNothing
        );

        return new DecisionTree(root);
    }

    public void TransitionTo(NPCStateID id) => CurrentStateID = id;

    public void StartRunAwayCooldown() => runAwayCooldownTimer = runAwayCooldownDuration;

    // ── Navegación: Steering cuando hay LOS, A* cuando no ────────────────

    public bool HasClearPath(Vector3 target)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, obstacleLayer);
    }

    public void NavigateTo(Vector3 target)
    {
        if (HasClearPath(target))
        {
            currentPath = null;
            MoveDirectly(target);
            return;
        }

        bool targetMoved = Vector3.Distance(target, lastPathTarget) > PathRefreshMoveDist;
        if (currentPath == null || pathRefreshTimer >= PathRefreshInterval || targetMoved)
        {
            currentPath = AStar.FindPath(transform.position, target);
            pathIndex = 0;
            pathRefreshTimer = 0f;
            lastPathTarget = target;
        }

        if (currentPath != null && currentPath.Count > 0)
        {
            while (pathIndex < currentPath.Count - 1 &&
                   ReachedPosition(currentPath[pathIndex], 0.6f))
                pathIndex++;

            MoveDirectly(currentPath[pathIndex]);
        }
        else
        {
            MoveDirectly(target);
        }
    }

    public void MoveToward(Vector3 target) => NavigateTo(target);

    public void PursuePlayer()
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 playerVel = pRb != null ? pRb.linearVelocity : Vector3.zero;

        float savedSpeed = maxSpeed;
        maxSpeed *= combatSpeedMultiplier;

        Vector3 force = SteeringBehaviours.Pursuit(
            transform.position, rb.linearVelocity,
            player.position, playerVel, maxSpeed);

        ApplySteering(force);
        maxSpeed = savedSpeed;
    }

    public void EvadePlayer(Vector3? safeDestination = null)
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 playerVel = pRb != null ? pRb.linearVelocity : Vector3.zero;

        Vector3 evadeForce = SteeringBehaviours.Evade(
            transform.position, rb.linearVelocity,
            player.position, playerVel, maxSpeed);

        if (safeDestination.HasValue)
        {
            Vector3 navForce;
            if (HasClearPath(safeDestination.Value))
            {
                navForce = SteeringBehaviours.Seek(
                    transform.position, rb.linearVelocity,
                    safeDestination.Value, maxSpeed);
            }
            else
            {
                bool targetMoved = Vector3.Distance(safeDestination.Value, lastPathTarget) > PathRefreshMoveDist;
                if (currentPath == null || pathRefreshTimer >= PathRefreshInterval || targetMoved)
                {
                    currentPath = AStar.FindPath(transform.position, safeDestination.Value);
                    pathIndex = 0;
                    pathRefreshTimer = 0f;
                    lastPathTarget = safeDestination.Value;
                }

                Vector3 astarTarget = currentPath != null && currentPath.Count > 0
                    ? currentPath[Mathf.Min(pathIndex, currentPath.Count - 1)]
                    : safeDestination.Value;

                navForce = SteeringBehaviours.Seek(
                    transform.position, rb.linearVelocity, astarTarget, maxSpeed);
            }

            ApplySteering(evadeForce * 0.7f + navForce * 0.3f);
        }
        else
        {
            ApplySteering(evadeForce);
        }
    }

    public void SetAnimatorSpeed(float speed) => animator?.SetFloat("Speed", speed);
    public void TriggerAttackAnimation() => animator?.SetTrigger("Attack");

    public bool ReachedPosition(Vector3 target, float tolerance = 0.55f)
    {
        Vector3 a = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 b = new Vector3(target.x, 0, target.z);
        return Vector3.Distance(a, b) <= tolerance;
    }

    public AttackState GetAttackState() => attackState;

    private IState GetStateInstance(NPCStateID id) => id switch
    {
        NPCStateID.Patrol => (IState)patrolState,
        NPCStateID.Idle => idleState,
        NPCStateID.RunAway => runAwayState,
        NPCStateID.Attack => attackState,
        NPCStateID.Search => searchState,
        NPCStateID.Alert => alertState,
        _ => patrolState
    };

    private void OnDrawGizmos()
    {
        if (enemyType != EnemyType.Guard || attackState == null) return;
        if (CurrentStateID != NPCStateID.Attack && CurrentStateID != NPCStateID.Search) return;

        Vector3 lastPos = CurrentStateID == NPCStateID.Search
            ? LastKnownPlayerPosition
            : attackState.LastKnownPosition;

        Gizmos.color = CurrentStateID == NPCStateID.Search
            ? new Color(1f, 0.3f, 0f, 1f)
            : new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawSphere(lastPos, 0.35f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.up, lastPos + Vector3.up * 0.35f);

        if (currentPath != null)
        {
            Gizmos.color = Color.green;
            for (int i = pathIndex; i < currentPath.Count - 1; i++)
                Gizmos.DrawLine(currentPath[i] + Vector3.up * 0.2f,
                                currentPath[i + 1] + Vector3.up * 0.2f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(lastPos + Vector3.up * 0.9f,
            CurrentStateID == NPCStateID.Search ? $"Buscando\n{name}" : $"Última pos\n{name}");
#endif
    }
}