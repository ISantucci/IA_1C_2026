using System.Collections.Generic;
using UnityEngine;

public static class RouletteWheelSelector
{
    public static int Select(IList<float> weights)
    {
        if (weights == null || weights.Count == 0) return -1;

        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
            total += Mathf.Max(0f, weights[i]);

        if (total <= 0f) return Random.Range(0, weights.Count);

        float spin = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += Mathf.Max(0f, weights[i]);
            if (spin <= cumulative) return i;
        }

        return weights.Count - 1;
    }

    public static int SelectClosest(Vector3 origin, IList<Vector3> candidates)
    {
        var weights = new List<float>(candidates.Count);
        foreach (var c in candidates)
        {
            float dist = Vector3.Distance(origin, c);
            weights.Add(dist < 0.01f ? 1000f : 1f / dist);
        }
        return Select(weights);
    }

    public static int SelectFarthestFrom(Vector3 threat, IList<Vector3> candidates)
    {
        var weights = new List<float>(candidates.Count);
        foreach (var c in candidates)
            weights.Add(Vector3.Distance(threat, c));
        return Select(weights);
    }
}