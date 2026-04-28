using UnityEngine;

    public class IdleState : IState
    {
        private readonly NPCController npc;

        public IdleState(NPCController npc) => this.npc = npc;

        public void OnEnter()
        {
            npc.IdleTimer = 0f;
            npc.IsIdlePending = false;
            npc.StopAgent();
            npc.SetAnimatorSpeed(0f);
            Debug.Log($"[{npc.name}] → IDLE ({npc.idleDuration}s)");
        }

        public void OnUpdate()
        {
            npc.IdleTimer += Time.deltaTime;

            if (npc.IdleTimer >= npc.idleDuration && !npc.PlayerVisible)
            {
                npc.TransitionTo(NPCStateID.Patrol);
            }
        }

        public void OnExit()
        {
            Debug.Log($"[{npc.name}] Idle terminado. Reanudando...");
        }
    }
