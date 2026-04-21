using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    [Header("Parámetros de visión")]
    public float viewDistance = 12f;
    [Range(0f, 360f)]
    public float fieldOfViewAngle = 110f;
    public LayerMask obstacleLayer;
    public Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);

    public bool HasLOS(Transform target)
    {
        if (target == null) return false;

        Vector3 eyePos = transform.position + eyeOffset;
        Vector3 targetPos = target.position + Vector3.up * 1.0f;
        Vector3 dir = targetPos - eyePos;
        float dist = dir.magnitude;

        if (dist > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > fieldOfViewAngle * 0.5f) return false;

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dist, obstacleLayer))
            return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + eyeOffset;
        float halfFOV = fieldOfViewAngle * 0.5f;

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        Gizmos.color = Color.cyan;
        Vector3 leftBound = Quaternion.Euler(0, -halfFOV, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, halfFOV, 0) * transform.forward;
        Gizmos.DrawLine(eyePos, eyePos + leftBound * viewDistance);
        Gizmos.DrawLine(eyePos, eyePos + rightBound * viewDistance);
        Gizmos.DrawLine(eyePos, eyePos + transform.forward * viewDistance);
    }
}