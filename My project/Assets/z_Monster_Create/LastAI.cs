using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class lastAI : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // 소리 감지 컴포넌트가 붙은 오브젝트

        private bool soundDetectedGo; // 소리 감지 여부
        private Vector3 targetGo; // 감지된 소리의 위치 (플레이어 위치)
        private bool isChasing = false; // 추격 중 여부

        [SerializeField] private bool chaseTarget = true; // 추격 기능 활성화 여부
        private NavMeshAgent navMeshAgent; // 몬스터 이동 제어용 NavMeshAgent
        private Animator animator; // 애니메이션 제어용 Animator
        public AudioSource AiVoice; // 추격 시작 시 재생되는 AI 사운드

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // 배회 범위
        [SerializeField] private float patrolWaitTime = 1.5f; // 각 지점에서 대기 시간
        private Vector3 patrolTarget; // 배회 중 이동할 목표 위치
        private bool isPatrolling = false; // 현재 배회 중인지 여부
        private float attackRange = 3.5f; // 공격 가능 거리

        // 추격 사운드 쿨타임 관련 변수
        private float voiceCooldown = 5f; // 쿨타임 설정 (5초)
        private float lastVoiceTime = -Mathf.Infinity; // 마지막 사운드 재생 시점

        // 추격 실패 시간 제한 (타겟 도달 못하면 추격 중단)
        private float chaseTimeout = 10f; // 추격 최대 지속 시간
        private float chaseTimer = 0f; // 추격 경과 시간

        private Coroutine patrolCoroutine = null;

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            /* // 디버깅용
            if (navMeshAgent == null)
                Debug.LogError(" NavMeshAgent가 없습니다!");
            if (animator == null)
                Debug.LogError(" Animator가 없습니다!");
            */

            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }

        void Update()
        {
            // AIHearing으로부터 소리 감지 여부와 목표 위치 받아오기
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            // 소리가 감지되고 추격 가능할 경우
            if (soundDetectedGo && chaseTarget)
            {
                // 사운드 쿨타임 체크
                if (Time.time - lastVoiceTime >= voiceCooldown)
                {
                    AiVoice.Play(); // 추격 시작 사운드 재생
                    lastVoiceTime = Time.time; // 마지막 재생 시간 갱신
                }

                // 타겟 위치가 유효할 경우
                if (targetGo != Vector3.zero)
                {
                    isChasing = true;
                    isPatrolling = false; // 배회 중단

                    float distance = Vector3.Distance(transform.position, targetGo);

                    if (distance < attackRange)
                    {
                        // 공격 가능 범위에 들어오면 공격 시도
                        Attack();
                        chaseTimer = 0f; // 추격 성공 → 타이머 초기화
                    }
                    else
                    {
                        // 아직 공격 범위가 아니면 계속 추격
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.SetDestination(targetGo); // 타겟 위치로 이동

                        chaseTimer += Time.deltaTime;

                        // 일정 시간 이상 따라잡지 못하면 추격 포기
                        if (chaseTimer > chaseTimeout)
                        {
                            StopChasing(); // 추격 종료
                        }
                    }
                }
            }
            // 이전에 추격 중이었지만 이제는 대상이 없을 경우
            else if (isChasing)
            {
                StopChasing(); // 추격 중단 처리
            }
            // 아무 상태도 아닐 경우 배회 루틴 시작
            else if (!isPatrolling)
            {
                patrolCoroutine = StartCoroutine(PatrolRoutine());
                // StartCoroutine(PatrolRoutine());
            }

            // 이동 상태와 애니메이션 상태를 일치시키기 위한 보조 처리
            if (!isChasing && !animator.GetBool("run"))
            {
                float speed = navMeshAgent.velocity.magnitude;
                float distance = navMeshAgent.remainingDistance;

                if (speed > 0.1f && navMeshAgent.hasPath)
                {
                    animator.SetBool("walk", true);
                    animator.SetBool("idle", false);
                }
                else if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    animator.SetBool("walk", false);
                    animator.SetBool("idle", true);
                    // 목표 지점이 너무 가까워서 멈춰있는 상태 방지
                    if (patrolCoroutine == null)
                    {
                        patrolCoroutine = StartCoroutine(PatrolRoutine());
                    }
                }
            }
        }

        // 추격을 종료하고 idle 상태로 전환
        private void StopChasing()
        {
            isChasing = false;
            isPatrolling = false;
            chaseTimer = 0f;
            lastVoiceTime = -Mathf.Infinity;

            navMeshAgent.isStopped = true;

            animator.SetBool("run", false);
            animator.SetBool("walk", false);
            animator.SetBool("idle", true);
                        
            // 일정 시간 후 다시 배회 시작
            StartCoroutine(ResumePatrolAfterDelay(1f));
        }

        // 배회 루틴 (랜덤 위치로 이동하고 잠시 대기)
        private IEnumerator PatrolRoutine()
        {
            isPatrolling = true;

            while (!isChasing)
            {
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                // 목적지 설정 직후 walk 애니메이션 ON
                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
                animator.SetBool("run", false);

                // 목적지 도착까지 대기
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                {
                    yield return null;
                }

                // 도착했으면 이동 멈추고 idle 애니메이션
                navMeshAgent.ResetPath();
                animator.SetBool("walk", false);
                //animator.SetBool("idle", true);

                yield return new WaitForSeconds(patrolWaitTime);
            }

            isPatrolling = false;
        }
        private Vector3 GetSafeRandomPatrolPosition()
        {
            // 현재 위치와 너무 가까운 위치는 제외
            Vector3 randomPosition;
            int maxAttempts = 5;
            int attempts = 0;

            do
            {
                randomPosition = GetRandomNavMeshPosition();
                attempts++;
            } while (Vector3.Distance(transform.position, randomPosition) < 1.5f && attempts < maxAttempts);

            return randomPosition;
        }

        // NavMesh 상의 랜덤 위치 반환 함수
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position; // 현재 위치 기준 방향 설정

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // 유효한 위치 반환
            }
            return transform.position; // 실패 시 현재 위치 반환
        }

        // 공격 함수 (애니메이션 트리거와 이동 중단 포함)
        void Attack()
        {
            //Debug.Log(">> 공격 시작");
            // 이미 공격 중이면 중복 방지
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

            // 현재 위치에서 멈춤
            navMeshAgent.SetDestination(transform.position);
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // 공격 후 복귀
            StartCoroutine(ResumeAfterAttack());
        }

        // 공격 애니메이션이 끝난 후 다시 이동
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(2f); // 애니메이션 시간

            navMeshAgent.isStopped = false;

            if (!isChasing)
            {
                patrolTarget = GetSafeRandomPatrolPosition();
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
            }
        }

        // 충돌감지
        private void OnTriggerEnter(Collider other)
        {
            // 충돌 대상이 player면
            if (other.CompareTag("Player"))
            {
                float distance = Vector3.Distance(transform.position, other.transform.position);

                if (distance < attackRange)
                {
                    // 공격 가능 범위에 들어오면 공격 시도
                    Attack();
                    chaseTimer = 0f; // 추격 성공 → 타이머 초기화
                }
            }
        }
        private IEnumerator ResumePatrolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 기존 루틴 중복 방지
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }

            navMeshAgent.isStopped = false;
            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }
    }
}
