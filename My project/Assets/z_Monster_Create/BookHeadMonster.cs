using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster: MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // 소리를 감지하는 AIHearing 객체

        private bool soundDetectedGo; // 소리가 감지되었는지 여부
        private Vector3 targetGo; // 추격할 목표의 위치
        private bool isChasing = false; // 추격 중인지 여부

        [SerializeField] private bool chaseTarget = true; // 추격 기능 활성화 여부
        private NavMeshAgent navMeshAgent; // 네비게이션 메쉬 에이전트 (이동 AI)
        private Animator animator; // 애니메이터 컨트롤러
        public AudioSource AiVoice; // AI의 소리 출력 (추격할 때 재생)

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // 배회 범위
        [SerializeField] private float patrolWaitTime = 3f; // 배회 중 대기 시간
        private Vector3 patrolTarget; // 배회할 목표 위치
        private bool isPatrolling = false; // 배회 중인지 여부
        private float attackRange = 4f;

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트 가져오기
            animator = GetComponent<Animator>(); // Animator 가져오기
            StartCoroutine(PatrolRoutine()); // 배회 루틴 시작
        }

        void Update()
        {
            // 소리 감지 여부 확인
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            if (soundDetectedGo && chaseTarget)
            {
                if (!AiVoice.isPlaying)
                {
                    AiVoice.Play(); // 소리 감지 시 AI 소리 재생
                }

                if (targetGo != Vector3.zero)
                {
                    isChasing = true; // 추격 시작
                    isPatrolling = false; // 배회 중단

                    float distance = Vector3.Distance(transform.position, targetGo);
                    if (distance < attackRange) // 공격 범위 내에 있을 경우
                    {
                        Attack(); // 공격 실행
                    }
                    else
                    {
                        // 목표를 향해 이동
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);
                        navMeshAgent.SetDestination(targetGo);
                    }
                    // 목표를 향해 이동
                    navMeshAgent.SetDestination(targetGo);
                }
            }
            else if (isChasing)
            {
                // 목표를 놓쳤다면 idle 상태로 복귀
                isChasing = false;
                animator.SetBool("run", false);
                animator.SetBool("idle", true);
            }
            else if (!isPatrolling)
            {
                StartCoroutine(PatrolRoutine());
            }
            /* 디버깅용 
            if (soundDetectedGo && chaseTarget)
            {
                Debug.Log("소리 감지됨 + 추격 활성화됨");

                if (targetGo != Vector3.zero)
                {
                    float distance = Vector3.Distance(transform.position, targetGo);
                    Debug.Log("플레이어까지 거리: " + distance);

                    if (distance < attackRange)
                    {
                        Debug.Log("공격 범위에 들어옴!");
                        Attack(); // 근데 안 나왔지?
                    }
                    else
                    {
                        Debug.Log("아직 공격 범위 밖임");
                    }
                }
            }
            */


        }

        // 배회 루틴 (랜덤한 위치로 이동)
        private IEnumerator PatrolRoutine()
        {
            isPatrolling = true;
            while (!isChasing)
            {
                patrolTarget = GetRandomNavMeshPosition(); // 랜덤한 위치 설정
                navMeshAgent.SetDestination(patrolTarget);

                // 애니메이션을 'walk'로 설정
                animator.SetBool("walk", true);
                animator.SetBool("run", false);
                animator.SetBool("idle", false);

                // 목적지에 도착할 때까지 대기
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                // 도착하면 'idle' 애니메이션 실행 후 대기
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);
                yield return new WaitForSeconds(patrolWaitTime);
            }
            isPatrolling = false;
        }

        // NavMesh 내에서 랜덤 위치 반환
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position; // 현재 위치에서 랜덤 방향으로 설정

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // 유효한 위치 반환
            }
            return transform.position; // 실패하면 현재 위치 반환
        }
        // 공격 실행 함수
        void Attack()
        {
            
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

            navMeshAgent.SetDestination(transform.position); // 이동 멈춤
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack"); // 트리거 초기화 후
            animator.SetTrigger("attack");  // 다시 실행

            StartCoroutine(ResumeAfterAttack());
        }

        // 공격 애니메이션이 끝난 후 이동 재개
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(1.5f); // 공격 애니메이션 길이에 맞게 조정
            navMeshAgent.isStopped = false; // 이동 활성화
        }

    }
}
