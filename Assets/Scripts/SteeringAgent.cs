using UnityEngine;
using UnityEngine.AI;

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
    protected NavMeshAgent navAgent;

    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        if (navAgent != null)
        {
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
            navAgent.speed = 0f;
        }
    }

    // Navega usando NavMesh para calcular el camino,
    // luego aplica steering hacia el siguiente punto de la ruta
    protected void NavigateTo(Vector3 destination)
    {
        if (navAgent == null || !navAgent.isOnNavMesh)
        {
            MoveDirectly(destination);
            return;
        }

        navAgent.SetDestination(destination);
        navAgent.nextPosition = transform.position;

        // Usar el siguiente punto del path calculado por NavMesh
        // en lugar de ir directo al destino final
        Vector3 steerTarget = GetNavMeshSteerTarget();
        Vector3 force = SteeringBehaviours.Seek(
            transform.position, rb.linearVelocity, steerTarget, maxSpeed);

        ApplySteeringForce(force);
    }

    // Movimiento directo sin NavMesh (fallback o para distancias cortas)
    protected void MoveDirectly(Vector3 destination)
    {
        Vector3 force = SteeringBehaviours.Seek(
            transform.position, rb.linearVelocity, destination, maxSpeed);
        ApplySteering(force);
    }

    // Devuelve el próximo waypoint del path de NavMesh
    // Si está muy cerca del siguiente punto, avanza al siguiente del path
    private Vector3 GetNavMeshSteerTarget()
    {
        if (navAgent.path == null || navAgent.path.corners.Length == 0)
            return navAgent.destination;

        Vector3[] corners = navAgent.path.corners;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 flat1 = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flat2 = new Vector3(corners[i].x, 0, corners[i].z);

            if (Vector3.Distance(flat1, flat2) > 0.6f)
                return corners[i];
        }

        return corners[corners.Length - 1];
    }

    protected void ApplySteering(Vector3 desiredForce)
    {
        Vector3 moveDir = rb.linearVelocity.sqrMagnitude > 0.01f
            ? rb.linearVelocity.normalized
            : transform.forward;

        Vector3 avoidance = SteeringBehaviours.ObstacleAvoidance(
            transform.position, moveDir,
            avoidanceDistance, avoidanceRadius,
            obstacleLayer, maxSpeed
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
                transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    private void ApplySteeringForce(Vector3 desiredForce)
    {
        Vector3 totalForce = Vector3.ClampMagnitude(desiredForce, maxForce);
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
                transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    public void StopAgent()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (navAgent != null && navAgent.isOnNavMesh)
            navAgent.ResetPath();
    }
}