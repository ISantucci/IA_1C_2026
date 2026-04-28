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

        float avoidMag = avoidance.magnitude;

        float seekScale = avoidMag > 0.1f
            ? Mathf.Clamp01(1f - avoidMag / (maxForce * 1.5f))
            : 1f;

        Vector3 totalForce = desiredForce * seekScale + avoidance * 2f;
        totalForce = Vector3.ClampMagnitude(totalForce, maxForce);

        Vector3 acceleration = totalForce / Mathf.Max(mass, 0.01f);

        Vector3 newVel = rb.linearVelocity + acceleration * Time.deltaTime;
        newVel.y = 0f;
        newVel = Vector3.ClampMagnitude(newVel, maxSpeed);

        rb.linearVelocity = newVel;

        if (rb.linearVelocity.sqrMagnitude > 0.05f)
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Quaternion targetRot = Quaternion.LookRotation(flatVel);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * 10f
            );
        }
    }

    public void StopAgent()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}