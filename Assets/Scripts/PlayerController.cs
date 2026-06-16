using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 12f;

    [Header("Objetivo")]
    public Transform pointB;
    public float goalRadius = 1.8f;

    private Rigidbody rb;
    private Animator animator;
    private StateMachine fsm;

    private PlayerIdleState idleState;
    private PlayerMoveState moveState;
    private PlayerDeadState deadState;

    public Vector2 MoveInput { get; private set; }
    public bool IsDead { get; private set; }
    public Rigidbody Rb => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezePositionY;

        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        deadState = new PlayerDeadState(this);

        fsm = new StateMachine();
        fsm.ChangeState(idleState);
    }

    private void Update()
    {
        if (IsDead) return;
        ReadInput();
        UpdateFSM();
        CheckGoal();
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float h = 0f, v = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1f;
        MoveInput = new Vector2(h, v);
    }

    private void UpdateFSM()
    {
        fsm.ChangeState(MoveInput.sqrMagnitude > 0.01f ? (IState)moveState : idleState);
        fsm.Update();
    }

    private void CheckGoal()
    {
        if (pointB == null) return;
        if (Vector3.Distance(transform.position, pointB.position) <= goalRadius)
            GameManager.Instance?.OnPlayerWin();
    }

    public void SetAnimatorSpeed(float s) => animator?.SetFloat("Speed", s);

    public void SetGameOver()
    {
        IsDead = true;
        rb.linearVelocity = Vector3.zero;
        fsm.ChangeState(deadState);
        fsm.Update();
    }
}

public class PlayerIdleState : IState
{
    private readonly PlayerController p;
    public PlayerIdleState(PlayerController p) => this.p = p;
    public void OnEnter() => p.SetAnimatorSpeed(0f);
    public void OnUpdate() => p.Rb.linearVelocity = new Vector3(0f, p.Rb.linearVelocity.y, 0f);
    public void OnExit() { }
}

public class PlayerMoveState : IState
{
    private readonly PlayerController p;
    public PlayerMoveState(PlayerController p) => this.p = p;
    public void OnEnter() { }

    public void OnUpdate()
    {
        Vector3 camF = Camera.main.transform.forward;
        Vector3 camR = Camera.main.transform.right;
        camF.y = 0f; camF.Normalize();
        camR.y = 0f; camR.Normalize();

        Vector3 dir = (camF * p.MoveInput.y + camR * p.MoveInput.x).normalized;
        p.Rb.linearVelocity = new Vector3(dir.x * p.moveSpeed, p.Rb.linearVelocity.y, dir.z * p.moveSpeed);

        Quaternion rot = Quaternion.LookRotation(dir);
        p.transform.rotation = Quaternion.Slerp(p.transform.rotation, rot, p.rotationSpeed * Time.deltaTime);
        p.SetAnimatorSpeed(p.Rb.linearVelocity.magnitude / p.moveSpeed);
    }

    public void OnExit() { }
}

public class PlayerDeadState : IState
{
    private readonly PlayerController p;
    public PlayerDeadState(PlayerController p) => this.p = p;
    public void OnEnter()
    {
        p.Rb.linearVelocity = Vector3.zero;
        p.SetAnimatorSpeed(0f);
        Debug.Log("[Player] → DEAD");
    }
    public void OnUpdate() { }
    public void OnExit() { }
}