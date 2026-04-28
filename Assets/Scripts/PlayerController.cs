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
    private bool gameOver;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;
    }

    private void Update()
    {
        if (gameOver) return;
        CheckGoal();
    }

    private void FixedUpdate()
    {
        if (gameOver) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float h = 0f;
        float v = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h = -1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h = 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v = -1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v = 1f;

        Vector3 input = new Vector3(h, 0f, v);

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * v + camRight * h).normalized;

            rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            animator?.SetFloat("Speed", rb.linearVelocity.magnitude);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
            animator?.SetFloat("Speed", 0f);
        }
    }

    private void CheckGoal()
    {
        if (pointB == null) return;
        if (Vector3.Distance(transform.position, pointB.position) <= goalRadius)
        {
            gameOver = true;
            GameManager.Instance?.OnPlayerWin();
        }
    }

    public void SetGameOver() => gameOver = true;
}