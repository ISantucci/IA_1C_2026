using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager Instance { get; private set; }

    [Header("Spawn")]
    public GameObject flockAgentPrefab;
    public int agentCount = 8;
    public float spawnRadius = 4f;
    [Tooltip("Altura de vuelo fija de los drones (la Y queda congelada en el Rigidbody del FlockAgent).")]
    public float spawnHeight = 2f;
    [Tooltip("Punto desde el que spawnean los drones. Si está vacío, usa la posición de este objeto.")]
    [SerializeField] private Transform spawnCenter;

    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Pesos")]
    public float separationWeight = 1.8f;
    public float cohesionWeight = 1.0f;
    public float alignmentWeight = 1.0f;

    [Header("Radios")]
    public float separationRadius = 1.5f;
    public float neighborRadius = 5f;

    [Header("Deteccion")]
    [SerializeField] private float playerSpottedMemoryTime = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool forceScatterDebug = false;

    public List<FlockAgent> Agents { get; private set; } = new();
    public bool PlayerSpotted { get; private set; }
    public Transform Player { get; private set; }

    private float _lastTimePlayerSeen = -999f;

    private void Awake()
    {
        Instance = this;
        if (player == null)
            player = Object.FindFirstObjectByType<PlayerController>()?.transform;
        Player = player;

        if (Player == null)
            Debug.LogWarning("[FlockManager] Player es null. Asignarlo en el Inspector (campo Referencias > Player).");
    }

    private void Start()
    {
        if (flockAgentPrefab == null)
        {
            Debug.LogWarning("[FlockManager] flockAgentPrefab no está asignado. No se spawnearán agentes.");
            return;
        }
        if (agentCount <= 0)
        {
            Debug.LogWarning("[FlockManager] agentCount es 0 o negativo. No se spawnearán agentes.");
            return;
        }

        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

        for (int i = 0; i < agentCount; i++)
        {
            Vector3 pos = center + Random.insideUnitSphere * spawnRadius;
            pos.y = spawnHeight;
            var go = Instantiate(flockAgentPrefab, pos, Quaternion.identity);
            var agent = go.GetComponent<FlockAgent>();
            if (agent != null)
            {
                Agents.Add(agent);
            }
            else
            {
                Debug.LogError($"[FlockManager] El prefab '{flockAgentPrefab.name}' NO tiene el componente FlockAgent. " +
                               "Por eso los agentes no se mueven. Revisá el prefab asignado.", go);
            }
        }

        if (debugLogs)
            Debug.Log($"[FlockManager] Agentes registrados: {Agents.Count}/{agentCount}");
    }

    private void Update()
    {
        if (forceScatterDebug)
        {
            PlayerSpotted = true;
            return;
        }

        bool anySeesPlayer = false;
        foreach (var a in Agents)
        {
            if (a.CanSeePlayer) { anySeesPlayer = true; break; }
        }

        if (anySeesPlayer)
            _lastTimePlayerSeen = Time.time;

        bool previousSpotted = PlayerSpotted;
        PlayerSpotted = anySeesPlayer || (Time.time - _lastTimePlayerSeen <= playerSpottedMemoryTime);

        if (debugLogs && PlayerSpotted != previousSpotted)
            Debug.Log($"[FlockManager] PlayerSpotted cambió a: {PlayerSpotted}");
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
    /// <summary>
    /// Centro promedio del flock (excluyendo opcionalmente un agente). Si no hay otros
    /// agentes, devuelve la posición del FlockManager. Usado como destino de fallback
    /// para un boid aislado que no tiene waypoints donde patrullar.
    /// </summary>
    public Vector3 GetFlockCenter(FlockAgent exclude = null)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var a in Agents)
        {
            if (a == null || a == exclude) continue;
            sum += a.transform.position;
            count++;
        }
        return count > 0 ? sum / count : transform.position;
    }

    public int CountNeighborsNear(FlockAgent agent, float radius)
    {
        int count = 0;
        foreach (var other in Agents)
        {
            if (other == agent) continue;
            float dist = Vector3.Distance(agent.transform.position, other.transform.position);
            if (dist <= radius) count++;
        }
        return count;
    }
}