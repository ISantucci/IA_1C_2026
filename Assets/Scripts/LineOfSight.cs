using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    public Transform reference;
    public float range = 12f;
    public float angle = 110f;
    public LayerMask obsMask;

    public bool HasLOS(Transform target)
    {
        if (target == null) return false;

        return CheckRange(target)
            && CheckAngle(target)
            && CheckView(target);
    }

    public bool CheckRange(Transform target)
    {
        float distanceToTarget = (target.position - Origin).sqrMagnitude;
        return distanceToTarget <= range * range;
    }

    public bool CheckAngle(Transform target)
    {
        Vector3 dirToTarget = target.position - Origin;
        float angleToTarget = Vector3.Angle(dirToTarget, Forward);
        return angleToTarget <= angle / 2f;
    }

    public bool CheckView(Transform target)
    {
        Vector3 dirToTarget = target.position - Origin;
        return !Physics.Raycast(Origin, dirToTarget.normalized, dirToTarget.magnitude, obsMask);
    }

    private Vector3 Origin
    {
        get
        {
            if (reference == null) return transform.position;
            return reference.position;
        }
    }

    private Vector3 Forward
    {
        get
        {
            if (reference == null) return transform.forward;
            return reference.forward;
        }
    }

    private void OnDrawGizmos()
    {
        Color myColor = Color.blue;
        myColor.a = 0.5f;
        Gizmos.color = myColor;
        Gizmos.DrawWireSphere(Origin, range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(Origin, Quaternion.Euler(0, angle / 2f, 0) * Forward * range);
        Gizmos.DrawRay(Origin, Quaternion.Euler(0, -angle / 2f, 0) * Forward * range);
    }
}