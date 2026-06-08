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

    // 3 factores por waypoint, todos dinámicos:
    // Factor 1 — recencia de visita (dinámico: crece con el tiempo sin visitar)
    // Factor 2 — distancia inversa  (dinámico: cambia al moverse el NPC)
    // Factor 3 — ruido aleatorio    (dinámico: distinto en cada llamada)
    public static int SelectWithVisitHistory(
        Vector3 origin,
        IList<Vector3> candidates,
        IList<float> lastVisitTimes)
    {
        var weights = new List<float>(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            float dist = Vector3.Distance(origin, candidates[i]);
            float wDist = dist < 0.01f ? 1000f : 1f / dist;
            float wRecency = Mathf.Clamp((Time.time - lastVisitTimes[i]) / 10f, 0.1f, 3f);
            float wNoise = Random.Range(0.5f, 1.5f);
            weights.Add(wDist * wRecency * wNoise);
        }
        return Select(weights);
    }
}