using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class lastAI : MonoBehaviour
    {
        // --------------------[참조 및 설정 변수]--------------------

        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // 소리 감지를 담당하는 스크립트가 붙은 오브젝트
        private AIHearing hearingScript; // 해당 오브젝트에서 가져온 AIHearing 컴포넌트

        [SerializeField] private bool chaseTarget = true; // 추격 기능 활성화 여부

        public AudioSource AiVoice; // 추격 시작 시 재생되는 사운드

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // 배회 범위
        [SerializeField] private float patrolWaitTime = 1.5f; // 배회 지점 도착 후 대기 시간

        private NavMeshAgent navMeshAgent; // 네비게이션 이동 처리용
        private Animator animator; // 애니메이션 제어용

        // --------------------[상태 변수]--------------------

        private bool soundDetectedGo; // 소리 감지 여부
        private Vector3 targetGo; // 감지된 소리의 위치 (플레이어 위치)
        private bool isChasing = false; // 현재 추격 중인지 여부
        private bool isPatrolling = false; // 현재 배회 중인지 여부
        private bool isAttacking = false; // 현재 공격 중인지 여부

        private Vector3 patrolTarget; // 배회 목표 위치
        private Coroutine patrolCoroutine = null; // 배회 루틴 저장용

        private float attackRange = 3.5f; // 공격 가능 거리

        // --------------------[추격 사운드 쿨타임]--------------------

        private float voiceCooldown = 5f; // 추격 사운드 재생 간격
        private float lastVoiceTime = -Mathf.Infinity; // 마지막 추격 사운드 재생 시간

        // --------------------[추격 실패 조건]--------------------

        private float chaseTimeout = 10f; // 추격 지속 시간 제한
        private float chaseTimer = 0f; // 현재 추격 경과 시간

        // --------------------[초기화]--------------------

        void Start()
        {
            Debug.Log("Start");

            // 컴포넌트 가져오기
            hearingScript = AIHearing.GetComponent<AIHearing>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // 배회 루틴 시작
            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }

        // --------------------[매 프레임 처리]--------------------

        void Update()
        {
            // 소리 감지 상태 및 위치 받아오기
            soundDetectedGo = hearingScript.soundDetected;
            targetGo = hearingScript.targetObj;

            // 소리를 감지했을 때
            if (soundDetectedGo && chaseTarget)
            {
                // 쿨타임 내에서 사운드 재생
                if (Time.time - lastVoiceTime >= voiceCooldown)
                {
                    AiVoice.Play();
                    lastVoiceTime = Time.time;
                }

                if (targetGo != Vector3.zero)
                {
                    isChasing = true;
                    isPatrolling = false;

                    float distance = Vector3.Distance(transform.position, targetGo);

                    if (distance < attackRange)
                    {
                        // 공격 범위 안에 있을 경우
                        Attack();
                        chaseTimer = 0f;
                    }
                    else
                    {
                        // 추격 중 애니메이션 설정 및 이동
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.SetDestination(targetGo);
                        chaseTimer += Time.deltaTime;

                        if (chaseTimer > chaseTimeout)
                        {
                            // 추격 시간 초과 시 중단
                            StopChasing();
                        }
                    }
                }
            }
            else if (isChasing)
            {
                // 추격 중이었지만 더 이상 추격 대상 없음
                StopChasing();
            }
            else if (!isPatrolling && patrolCoroutine == null)
            {
                // 아무것도 안하고 있다면 다시 배회 시작
                patrolCoroutine = StartCoroutine(PatrolRoutine());
            }

            // 걷기 애니메이션 유지 보조
            if (!isChasing && !animator.GetBool("run"))
            {
                float speed = navMeshAgent.velocity.magnitude;

                if (speed > 0.1f && navMeshAgent.hasPath)
                {
                    animator.SetBool("walk", true);
                    animator.SetBool("idle", false);
                }
                else if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    animator.SetBool("walk", false);
                    animator.SetBool("idle", true);

                    if (patrolCoroutine == null)
                        patrolCoroutine = StartCoroutine(PatrolRoutine());
                }
            }
        }

        // --------------------[추격 종료 처리]--------------------

        private void StopChasing()
        {
            Debug.Log("StopCHsasing");
            isChasing = false;
            isPatrolling = false;
            chaseTimer = 0f;
            lastVoiceTime = -Mathf.Infinity;

            navMeshAgent.isStopped = true;

            animator.SetBool("run", false);
            animator.SetBool("walk", false);
            animator.SetBool("idle", true);

            // 잠깐 대기 후 배회 재개
            StartCoroutine(ResumePatrolAfterDelay(1f));
        }

        // --------------------[배회 루틴]--------------------

        private IEnumerator PatrolRoutine()
        {
            Debug.Log("PatrolRoutine");

            isPatrolling = true;
            patrolCoroutine = null;

            while (!isChasing)
            {
                // 랜덤 배회 위치 설정
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
                animator.SetBool("run", false);

                float timer = 0f;

                // 목표 도달까지 대기
                while (!navMeshAgent.pathPending &&
                       navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance &&
                       navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                       timer < 5f)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                navMeshAgent.ResetPath();
                animator.SetBool("walk", false);

                yield return new WaitForSeconds(patrolWaitTime);
            }

            isPatrolling = false;
        }

        // 너무 가까운 위치는 피해서 랜덤 위치 설정
        private Vector3 GetSafeRandomPatrolPosition()
        {
            Debug.Log("GetSafeRandomPatrolPosition");

            Vector3 randomPosition;
            int maxAttempts = 10;
            int attempts = 0;

            do
            {
                randomPosition = GetRandomNavMeshPosition();
                attempts++;
            } while (Vector3.Distance(transform.position, randomPosition) < 2f && attempts < maxAttempts);

            return randomPosition;
        }

        // NavMesh 상의 유효한 랜덤 위치 반환
        private Vector3 GetRandomNavMeshPosition()
        {
            Debug.Log("GetRandomNavMeshPosition");

            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
                return hit.position;

            Debug.LogWarning("유효한 NavMesh 위치를 찾지 못함. 현재 위치로 대체.");
            return transform.position;
        }

        // --------------------[공격 처리]--------------------

        void Attack()
        {
            Debug.Log("Attack");

            if (isAttacking) return;

            isAttacking = true;

            navMeshAgent.SetDestination(transform.position);
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            StartCoroutine(ResumeAfterAttack());
        }

        // 공격 후 상태 복귀
        private IEnumerator ResumeAfterAttack()
        {
            Debug.Log("ResumeAfterAttack");

            yield return new WaitForSeconds(2f);

            isAttacking = false;
            navMeshAgent.isStopped = false;

            if (!isChasing)
            {
                patrolTarget = GetSafeRandomPatrolPosition();
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
            }
        }

        // --------------------[충돌 감지 - 근접 공격 조건]--------------------

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("collider player");

                float distance = Vector3.Distance(transform.position, other.transform.position);

                if (distance < attackRange)
                {
                    Vector3 direction = (other.transform.position - transform.position).normalized;
                    direction.y = 0f;

                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        transform.rotation = lookRotation;
                    }

                    Attack();
                    chaseTimer = 0f;
                }
            }
        }

        // 배회 재개 지연 처리
        private IEnumerator ResumePatrolAfterDelay(float delay)
        {
            Debug.Log("ResumePatrolAfterDelay");

            yield return new WaitForSeconds(delay);

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
