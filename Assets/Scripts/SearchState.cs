using UnityEngine;

public class SearchState : IState
{
    private readonly NPCController npc;
    private Vector3 searchTarget;
    private float searchTimer;
    private bool arrived;

    private const float SearchDuration = 4f;
    private const float ArrivalTolerance = 0.8f;

    public SearchState(NPCController npc) => this.npc = npc;

    public void OnEnter()
    {
        searchTarget = npc.LastKnownPlayerPosition;
        searchTimer = 0f;
        arrived = false;
        Debug.Log($"[{npc.name}] → SEARCH");
    }

    public void OnUpdate()
    {
        if (npc.PlayerVisible)
        {
            npc.TransitionTo(NPCStateID.Attack);
            return;
        }

        if (!arrived)
        {
            npc.NavigateTo(searchTarget);
            npc.SetAnimatorSpeed(npc.Velocity.magnitude / npc.maxSpeed);

            if (npc.ReachedPosition(searchTarget, ArrivalTolerance))
            {
                arrived = true;
                npc.StopAgent();
                npc.SetAnimatorSpeed(0f);
                Debug.Log($"[{npc.name}] Llegó a última posición. Buscando...");
            }
        }
        else
        {
            searchTimer += Time.deltaTime;
            npc.transform.Rotate(Vector3.up, 55f * Time.deltaTime);

            if (searchTimer >= SearchDuration)
            {
                Debug.Log($"[{npc.name}] Búsqueda agotada → PATROL");
                npc.TransitionTo(NPCStateID.Patrol);
            }
        }
    }

    public void OnExit()
    {
        npc.StopAgent();
        npc.SetAnimatorSpeed(0f);
    }
}