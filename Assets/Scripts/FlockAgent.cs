using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineOfSight))]
public class FlockAgent : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float maxForce = 8f;

    private Rigidbody rb;
    private LineOfSight los;
    private StateMachine fsm;

    private FlockNormalState normalState;
    private FlockScatterState scatterState;

    public Vector3 Velocity => rb.linearVelocity;
    public bool CanSeePlayer { get; private set; }
    public LayerMask ObstacleMask => los != null ? los.ObstacleMask : default;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        los = GetComponent<LineOfSight>();
        if (los.ObstacleMask.value == 0)
            Debug.LogWarning($"[FlockAgent] {name}: LineOfSight.obsMask está vacía. El Obstacle Avoidance en Scatter no funcionará.");

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        normalState = new FlockNormalState(this);
        scatterState = new FlockScatterState(this);

        fsm = new StateMachine();
        fsm.ChangeState(normalState);
    }

    private void Update()
    {
        if (FlockManager.Instance == null) return;

        CanSeePlayer = los.HasLOS(FlockManager.Instance.Player);

        fsm.ChangeState(FlockManager.Instance.PlayerSpotted ? (IState)scatterState : normalState);
        fsm.Update();
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

        const float checkDistance = 2f;

        Vector3 fleeDir = agent.transform.position - fm.Player.position;
        fleeDir.y = 0f;
        fleeDir.Normalize();

        LayerMask mask = agent.ObstacleMask;
        if (mask.value != 0 && Physics.Raycast(agent.transform.position, fleeDir, checkDistance, mask))
        {
            Vector3 right = Vector3.Cross(Vector3.up, fleeDir);
            right.y = 0f;
            right.Normalize();

            bool rightClear = !Physics.Raycast(agent.transform.position, right,  checkDistance, mask);
            bool leftClear  = !Physics.Raycast(agent.transform.position, -right, checkDistance, mask);

            if      (rightClear) fleeDir = right;
            else if (leftClear)  fleeDir = -right;
            // else: corner cerrado — mantener flee original
        }

        Vector3 flee = fleeDir * agent.moveSpeed * 2f;
        Vector3 sep  = fm.GetSeparation(agent) * fm.separationWeight;

        agent.ApplyForce(flee + sep);
    }

    public void OnExit() { }
}
