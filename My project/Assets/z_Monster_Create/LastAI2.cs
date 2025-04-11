using AiSoundDetect;
using UnityEngine;
using UnityEngine.AI;

public class LastAI2 : MonoBehaviour
{
    enum State { Idle, Walk, Run, Attack }

    public Animator anim;
    public NavMeshAgent agent;
    public AIHearing hearing;
    public float attackRange = 3.5f;
    public float idleDelayAfterAttack = 1.5f;

    private Vector3 walkTarget;
    private Vector3 soundPosition;
    private State currentState;
    private float stateTimer;

    void Start()
    {
        SetState(State.Walk);
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    SetState(State.Walk);
                }
                break;

            case State.Walk:
                Patrol();

                if (hearing.HearSound(out soundPosition))
                {
                    SetState(State.Run);
                }
                break;

            case State.Run:
                agent.SetDestination(soundPosition);

                if (Vector3.Distance(transform.position, soundPosition) <= attackRange)
                {
                    SetState(State.Attack);
                }

                // 일정 시간 추격했지만 못 따라가면 Idle
                if (agent.remainingDistance < 0.1f && !agent.pathPending)
                {
                    SetState(State.Idle, 1f); // 1초 대기 후 Walk
                }
                break;

            case State.Attack:
                agent.ResetPath();
                transform.LookAt(soundPosition); // 공격 방향 설정
                anim.SetTrigger("Attack");

                SetState(State.Idle, idleDelayAfterAttack);
                break;
        }
    }

    void Patrol()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            // 랜덤한 위치 선택
            Vector3 randomDirection = Random.insideUnitSphere * 5f;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    void SetState(State newState, float idleDelay = 0f)
    {
        currentState = newState;
        anim.ResetTrigger("Attack");

        switch (newState)
        {
            case State.Idle:
                agent.ResetPath();
                stateTimer = idleDelay;
                anim.Play("Idle");
                break;

            case State.Walk:
                anim.Play("Walk");
                break;

            case State.Run:
                anim.Play("Run");
                break;

            case State.Attack:
                anim.Play("Attack");
                break;
        }
    }
}
