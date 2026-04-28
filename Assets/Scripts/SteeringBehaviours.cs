using UnityEngine;

public static class SteeringBehaviours
{
    public static Vector3 Seek(Vector3 pos, Vector3 vel, Vector3 target, float maxSpeed)
    {
        Vector3 desired = (target - pos).normalized * maxSpeed;
        return desired - vel;
    }

    public static Vector3 Flee(Vector3 pos, Vector3 vel, Vector3 target, float maxSpeed)
    {
        Vector3 desired = (pos - target).normalized * maxSpeed;
        return desired - vel;
    }

    public static Vector3 Pursuit(
        Vector3 pos, Vector3 vel,
        Vector3 targetPos, Vector3 targetVel,
        float maxSpeed)
    {
        Vector3 toTarget = targetPos - pos;
        float lookAhead = toTarget.magnitude / (maxSpeed + targetVel.magnitude + 0.001f);
        lookAhead = Mathf.Min(lookAhead, 2.0f);

        Vector3 futurePos = targetPos + targetVel * lookAhead;
        return Seek(pos, vel, futurePos, maxSpeed);
    }

    public static Vector3 Evade(
        Vector3 pos, Vector3 vel,
        Vector3 threatPos, Vector3 threatVel,
        float maxSpeed)
    {
        Vector3 toThreat = threatPos - pos;
        float lookAhead = toThreat.magnitude / (maxSpeed + threatVel.magnitude + 0.001f);
        lookAhead = Mathf.Min(lookAhead, 2.0f);

        Vector3 futurePos = threatPos + threatVel * lookAhead;
        return Flee(pos, vel, futurePos, maxSpeed);
    }

    public static Vector3 ObstacleAvoidance(
        Vector3 pos,
        Vector3 forward,
        float detectionLength,
        float detectionRadius,
        LayerMask obstacleLayer,
        float maxSpeed)
    {
        if (forward == Vector3.zero) return Vector3.zero;

        Vector3 avoidance = Vector3.zero;
        float avoidStrength = maxSpeed * 3f;
        Vector3 castOrigin = pos + Vector3.up * 0.5f;

        Vector3 perpendicular = Vector3.Cross(Vector3.up, forward).normalized;

        if (Physics.SphereCast(castOrigin, detectionRadius, forward,
                out RaycastHit cHit, detectionLength, obstacleLayer))
        {
            float urgency = 1f - cHit.distance / detectionLength;

            float side = Vector3.Dot(cHit.normal, perpendicular);
            Vector3 avoidDir = side >= 0f ? perpendicular : -perpendicular;

            avoidance += avoidDir * avoidStrength * urgency;
        }

        float shortLen = detectionLength * 0.75f;
        float sideRad = detectionRadius * 0.6f;

        Vector3 leftDir = Quaternion.Euler(0, -40f, 0) * forward;
        if (Physics.SphereCast(castOrigin, sideRad, leftDir,
                out RaycastHit lHit, shortLen, obstacleLayer))
        {
            float urgency = 1f - lHit.distance / shortLen;
            avoidance += perpendicular * avoidStrength * urgency * 0.7f;
        }

        Vector3 rightDir = Quaternion.Euler(0, 40f, 0) * forward;
        if (Physics.SphereCast(castOrigin, sideRad, rightDir,
                out RaycastHit rHit, shortLen, obstacleLayer))
        {
            float urgency = 1f - rHit.distance / shortLen;
            avoidance -= perpendicular * avoidStrength * urgency * 0.7f;
        }

        avoidance.y = 0f;
        return avoidance;
    }
}