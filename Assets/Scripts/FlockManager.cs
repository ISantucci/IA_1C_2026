using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager Instance { get; private set; }

    [Header("Spawn")]
    public GameObject flockAgentPrefab;
    public int agentCount = 8;
    public float spawnRadius = 4f;

    [Header("Pesos")]
    public float separationWeight = 1.8f;
    public float cohesionWeight = 1.0f;
    public float alignmentWeight = 1.0f;

    [Header("Radios")]
    public float separationRadius = 1.5f;
    public float neighborRadius = 5f;

    public List<FlockAgent> Agents { get; private set; } = new();
    public bool PlayerSpotted { get; private set; }
    public Transform Player { get; private set; }

    private void Awake()
    {
        Instance = this;
        Player = Object.FindFirstObjectByType<PlayerController>()?.transform;
    }

    private void Start()
    {
        for (int i = 0; i < agentCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = transform.position.y;
            var go = Instantiate(flockAgentPrefab, pos, Quaternion.identity);
            var agent = go.GetComponent<FlockAgent>();
            if (agent != null) Agents.Add(agent);
        }
    }

    private void Update()
    {
        PlayerSpotted = false;
        foreach (var a in Agents)
        {
            if (a.CanSeePlayer) { PlayerSpotted = true; break; }
        }
    }

    public Vector3 GetSeparation(FlockAgent agent)
    {
        Vector3 force = Vector3.zero;
        int count = 0;
        foreach (var other in Agents)
        {
            if (other == agent) continue;
            float dist = Vector3.Distance(agent.transform.position, other.transform.position);
            if (dist < separationRadius && dist > 0f)
            {
                force += (agent.transform.position - other.transform.position).normalized / dist;
                count++;
            }
        }
        return count > 0 ? force / count : Vector3.zero;
    }

    public Vector3 GetCohesion(FlockAgent agent)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var other in Agents)
        {
            if (other == agent) continue;
            float dist = Vector3.Distance(agent.transform.position, other.transform.position);
            if (dist < neighborRadius) { center += other.transform.position; count++; }
        }
        return count > 0 ? ((center / count) - agent.transform.position).normalized : Vector3.zero;
    }

    public Vector3 GetAlignment(FlockAgent agent)
    {
        Vector3 avg = Vector3.zero;
        int count = 0;
        foreach (var other in Agents)
        {
            if (other == agent) continue;
            float dist = Vector3.Distance(agent.transform.position, other.transform.position);
            if (dist < neighborRadius) { avg += other.Velocity; count++; }
        }
        return count > 0 ? (avg / count).normalized : Vector3.zero;
    }
}