using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class SteeringAgent : MonoBehaviour
{
    [Header("Movimiento")]
    public float maxSpeed = 4f;
    public float maxForce = 10f;
    public float mass = 1f;

    [Header("Obstacle Avoidance")]
    public float avoidanceDistance = 4f;
    public float avoidanceRadius = 0.5f;
    public LayerMask obstacleLayer;

    protected Rigidbody rb;

    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;
    }

    protected void MoveDirectly(Vector3 destination)
    {
        Vector3 force = SteeringBehaviours.Seek(
            transform.position,
            rb.linearVelocity,
            destination,
            maxSpeed
        );

        ApplySteering(force);
    }

    protected void ApplySteering(Vector3 desiredForce)
    {
        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        Vector3 avoidance = SteeringBehaviours.ObstacleAvoidance(
            transform.position,
            moveDir,
            avoidanceDistance,
            avoidanceRadius,
            obstacleLayer,
            maxSpeed
        );

        Vector3 totalForce = desiredForce + avoidance;

        totalForce = Vector3.ClampMagnitude(totalForce, maxForce);

        Vector3 acceleration = totalForce / Mathf.Max(mass, 0.01f);

        Vector3 newVel = rb.linearVelocity + acceleration * Time.deltaTime;
        newVel.y = 0f;
        newVel = Vector3.ClampMagnitude(newVel, maxSpeed);

        rb.linearVelocity = newVel;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (flatVel.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatVel);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * 8f
            );
        }
    }
    public void StopAgent()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}