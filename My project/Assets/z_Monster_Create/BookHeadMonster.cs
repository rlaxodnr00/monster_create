using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // 소리 감지 기능을 가진 오브젝트 (AIHearing 스크립트가 붙어 있음)

        private bool soundDetectedGo; // 소리 감지 여부
        private Vector3 targetGo; // 소리가 발생한 목표 위치
        private bool isChasing = false; // 현재 추격 중인지 여부

        [SerializeField] private bool chaseTarget = true; // 추격 기능 사용 여부 (비활성화 가능)
        private NavMeshAgent navMeshAgent; // 유니티 내비게이션 에이전트 (이동 제어)
        private Animator animator; // 애니메이션 제어용 컴포넌트
        public AudioSource AiVoice; // AI가 추격 시 내는 소리

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // 배회할 반경
        //[SerializeField] private float patrolWaitTime = 3f; // 한 지점에서 대기하는 시간
        private Vector3 patrolTarget; // 배회 시 이동할 목표 위치
        private bool isPatrolling = false; // 현재 배회 중인지 여부
        private float attackRange = 4f; // 공격 사거리

        void Start()
        {
            // 컴포넌트 초기화
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // 배회 루틴 시작
            StartCoroutine(PatrolRoutine());
        }

        void Update()
        {
            // AIHearing에서 소리 감지 여부 및 대상 위치 가져오기
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            // 소리 감지 & 추격 기능이 켜져 있을 경우
            if (soundDetectedGo && chaseTarget)
            {
                // AI 음성 재생
                if (!AiVoice.isPlaying)
                    AiVoice.Play();

                // 유효한 위치가 감지된 경우
                if (targetGo != Vector3.zero)
                {
                    isChasing = true;      // 추격 시작
                    isPatrolling = false;  // 배회 중단

                    float distance = Vector3.Distance(transform.position, targetGo);
                    if (distance < attackRange)
                    {
                        Attack(); // 사거리 내면 공격 실행
                    }
                    else
                    {
                        // 달리는 애니메이션
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(targetGo); // 대상 위치로 이동
                    }
                }
            }
            // 추격 중이었는데 더 이상 대상이 없으면 멈추기
            else if (isChasing)
            {
                isChasing = false;

                animator.SetBool("run", false);
                animator.SetBool("idle", true);

                navMeshAgent.isStopped = true; // 에이전트 정지
                navMeshAgent.ResetPath();      // 이동 경로 초기화
            }
            // 추격 중이 아니고 배회 중이 아닐 경우, 배회 시작
            else
            {
                if (!isPatrolling)
                {
                    isPatrolling = true;
                    StartCoroutine(PatrolRoutine());
                }

                // idle 상태일 땐 강제로 이동 정지 및 경로 초기화
                if (animator.GetBool("idle"))
                {
                    if (!navMeshAgent.isStopped)
                        navMeshAgent.isStopped = true;

                    if (navMeshAgent.hasPath)
                        navMeshAgent.ResetPath();
                }
            }
        }

        // 배회 루틴 (지속적으로 랜덤 위치로 이동)
        private IEnumerator PatrolRoutine()
        {
            while (!isChasing) // 추격 중이 아닐 때만 실행
            {
                // 랜덤한 위치 계산 및 이동 시작
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                // 걷기 애니메이션 설정
                animator.SetBool("walk", true);
                animator.SetBool("run", false);
                animator.SetBool("idle", false);

                // 목적지에 도착할 때까지 대기
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                // 도착 후 idle 상태 전환 및 잠시 대기
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);

                // 이동 멈추고 경로 초기화
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;

                float randomIdleTime = Random.Range(1f, 2f); // 1초 ~ 3초 사이 랜덤 시간
                yield return new WaitForSeconds(randomIdleTime);
            }

            isPatrolling = false; // 루틴 종료 시 배회 상태 false로
        }

        // NavMesh 안에서 랜덤한 위치 선택
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // 유효한 위치 반환
            }

            return transform.position; // 실패 시 현재 위치 반환
        }

        // 공격 실행 함수
        void Attack()
        {
            // 현재 공격 애니메이션이 재생 중이면 중복 방지
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            // 이동 멈추기
            navMeshAgent.SetDestination(transform.position);
            navMeshAgent.isStopped = true;

            // 트리거 초기화 후 다시 설정하여 애니메이션 재생
            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // 일정 시간 후 이동 재개
            StartCoroutine(ResumeAfterAttack());
        }

        // 공격 후 이동 재개 루틴
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(1.5f); // 공격 애니메이션 길이만큼 대기
            navMeshAgent.isStopped = false;
        }
    }
}
