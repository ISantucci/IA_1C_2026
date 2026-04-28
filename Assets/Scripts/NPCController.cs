using UnityEngine;

public enum EnemyType { Guard, Scout }
public enum NPCStateID { Patrol, Idle, RunAway, Attack }

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

    public int CurrentWaypointIndex { get; set; }
    public int LastPatrolWaypointIndex { get; set; } = -1;
    public bool PatrolForward { get; set; } = true;
    public int PatrolCycleCount { get; set; }
    public float IdleTimer { get; set; }
    public bool IsIdlePending { get; set; }

    public LineOfSight LOS { get; private set; }
    public bool PlayerVisible => LOS != null && LOS.HasLOS(player);

    private StateMachine fsm;
    public NPCStateID CurrentStateID { get; private set; }

    private PatrolState patrolState;
    private IdleState idleState;
    private RunAwayState runAwayState;
    private AttackState attackState;

    private DecisionTree decisionTree;
    private Animator animator;

    private float runAwayCooldownTimer = 0f;
    public bool RunAwayCooldownActive => runAwayCooldownTimer > 0f;

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

        fsm = new StateMachine();
        decisionTree = BuildDecisionTree();

        TransitionTo(NPCStateID.Patrol);
    }

    private void Update()
    {
        if (runAwayCooldownTimer > 0f)
            runAwayCooldownTimer -= Time.deltaTime;

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
        var doAttack = new ActionNode(() => TransitionTo(NPCStateID.Attack));
        var doRunAway = new ActionNode(() => TransitionTo(NPCStateID.RunAway));
        var doNothing = new ActionNode(() => { });

        var onPlayerVisible = new ConditionNode(
            () => enemyType == EnemyType.Guard,
            doAttack,
            doRunAway
        );

        var root = new ConditionNode(
            () => PlayerVisible && !RunAwayCooldownActive && CurrentStateID != NPCStateID.RunAway,
            onPlayerVisible,
            doNothing
        );

        return new DecisionTree(root);
    }

    public void StartRunAwayCooldown()
    {
        runAwayCooldownTimer = runAwayCooldownDuration;
    }

    public void TransitionTo(NPCStateID id)
    {
        CurrentStateID = id;
    }

    public void SetAnimatorSpeed(float speed)
    {
        animator?.SetFloat("Speed", speed);
    }

    public void TriggerAttackAnimation()
    {
        animator?.SetTrigger("Attack");
    }

    public void MoveToward(Vector3 target)
    {
        MoveDirectly(target);
    }

    public void PursuePlayer()
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 playerVel = pRb != null ? pRb.linearVelocity : Vector3.zero;

        float savedSpeed = maxSpeed;
        maxSpeed *= combatSpeedMultiplier;

        Vector3 force = SteeringBehaviours.Pursuit(
            transform.position, rb.linearVelocity,
            player.position, playerVel,
            maxSpeed);

        ApplySteering(force);
        maxSpeed = savedSpeed;
    }

    public void EvadePlayer(Vector3? safeDestination = null)
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 playerVel = pRb != null ? pRb.linearVelocity : Vector3.zero;

        Vector3 evadeForce = SteeringBehaviours.Evade(
            transform.position, rb.linearVelocity,
            player.position, playerVel,
            maxSpeed);

        if (safeDestination.HasValue)
        {
            Vector3 seekForce = SteeringBehaviours.Seek(
                transform.position, rb.linearVelocity,
                safeDestination.Value, maxSpeed);

            ApplySteering(evadeForce * 0.7f + seekForce * 0.3f);
        }
        else
        {
            ApplySteering(evadeForce);
        }
    }

    public bool ReachedPosition(Vector3 target, float tolerance = 0.55f)
    {
        Vector3 flat1 = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flat2 = new Vector3(target.x, 0, target.z);
        return Vector3.Distance(flat1, flat2) <= tolerance;
    }

    public AttackState GetAttackState() => attackState;

    private IState GetStateInstance(NPCStateID id) => id switch
    {
        NPCStateID.Patrol => (IState)patrolState,
        NPCStateID.Idle => idleState,
        NPCStateID.RunAway => runAwayState,
        NPCStateID.Attack => attackState,
        _ => patrolState
    };

    private void OnDrawGizmos()
    {
        if (enemyType != EnemyType.Guard) return;
        if (attackState == null) return;
        if (CurrentStateID != NPCStateID.Attack) return;

        Vector3 lastPos = attackState.LastKnownPosition;

        Gizmos.color = attackState.IsSearchingLastPos
            ? new Color(1f, 0.3f, 0f, 1f)   // naranja: esta buscando ahí
            : new Color(1f, 1f, 0f, 0.8f);   // amarillo: persiguiendo, posición desactualizada

        Gizmos.DrawSphere(lastPos, 0.35f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.up * 1f, lastPos + Vector3.up * 0.35f);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(lastPos + Vector3.up * 0.8f, $"Última pos\n{name}");
#endif
    }
}