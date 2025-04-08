using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BH_Remake : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // 소리 감지 스크립트가 붙어있는 오브젝트

        private Vector3 targetGo; // 감지된 소리 위치
        private bool isChasing = false; // 추격 중인지 여부

        [SerializeField] private bool chaseTarget = true; // 추격 가능 여부
        private NavMeshAgent navMeshAgent; // 유니티 내비게이션 컴포넌트
        private Animator animator; // 애니메이터 컴포넌트
        public AudioSource AiVoice; // 소리 감지 시 음성 재생용 오디오 소스

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 30f; // 배회 반경
        [SerializeField] private float minimumMoveDistance = 5f; // 최소 이동 거리 (너무 가까운 곳은 제외)
        private Vector3 patrolTarget; // 다음 배회 목표 지점
        private bool isPatrolling = false; // 배회 중인지 여부
        private float attackRange = 3.2f; // 공격 범위

        private float soundMemoryDuration = 5f; // 소리 감지 후 기억 지속 시간
        private float soundMemoryTimer = 0f; // 현재 기억 타이머

        [Header("Footstep Sounds")]
        public AudioClip[] walkFootstepClips; // 걷는 발소리 목록
        public AudioClip[] runFootstepClips; // 뛰는 발소리 목록
        public float walkFootstepInterval = 0.6f; // 걷기 발소리 간격
        public float runFootstepInterval = 0.4f; // 달리기 발소리 간격

        private float footstepTimer = 0f; // 다음 발소리까지 남은 시간
        private AudioSource footstepAudio; // 발소리용 오디오 소스

        private float chaseTime = 0f; // 현재 추격 시간
        private float maxChaseTime = 10f; // 최대 추격 지속 시간

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // 발소리용 오디오 소스 초기화
            footstepAudio = gameObject.AddComponent<AudioSource>();
            footstepAudio.playOnAwake = false;
            footstepAudio.spatialBlend = 1f; // 3D 사운드로 설정

            StartCoroutine(PatrolRoutine()); // 시작 시 배회 루틴 시작
        }

        void Update()
        {
            // 현재 프레임에서 소리 감지 확인
            bool heardNow = AIHearing.GetComponent<AIHearing>().soundDetected;
            Vector3 heardPos = AIHearing.GetComponent<AIHearing>().targetObj;

            if (heardNow && heardPos != Vector3.zero)
            {
                targetGo = heardPos;
                soundMemoryTimer = soundMemoryDuration;
                isChasing = true;
                isPatrolling = false;
                chaseTime = 0f;

                if (!AiVoice.isPlaying)
                    AiVoice.Play();
            }

            if (soundMemoryTimer > 0f)
            {
                soundMemoryTimer -= Time.deltaTime;
            }
            else
            {
                isChasing = false;
            }

            if (isChasing && chaseTarget)
            {
                chaseTime += Time.deltaTime;

                float distance = Vector3.Distance(transform.position, targetGo);

                // 플레이어가 공격 범위 내에 들어올 경우
                if (distance < attackRange)
                {
                    // 공격 상태가 아니면 공격 시도
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                    {
                        Attack();
                    }
                }
                else
                {
                    // 공격 중인데 범위 밖으로 벗어난 경우 → 공격 취소하고 다시 추격
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                    {
                        // 공격 애니메이션 도중에 추격 전환
                        animator.ResetTrigger("attack");
                        animator.SetBool("idle", false);
                        animator.SetBool("run", true);
                    }

                    navMeshAgent.isStopped = false;
                    navMeshAgent.SetDestination(targetGo);

                    Vector3 direction = (targetGo - transform.position).normalized;
                    Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

                    animator.SetBool("run", true);
                    animator.SetBool("walk", false);
                    animator.SetBool("idle", false);
                }

                if (chaseTime >= maxChaseTime)
                {
                    isChasing = false;
                    chaseTime = 0f;
                    StartCoroutine(IdleAfterChase());
                }

                Debug.DrawLine(transform.position, targetGo, Color.red);
            }
            else if (!isChasing)
            {
                animator.SetBool("run", false);
                animator.SetBool("idle", true);
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();

                if (!isPatrolling)
                {
                    isPatrolling = true;
                    StartCoroutine(PatrolRoutine());
                }
            }

            float moveSpeed = navMeshAgent.velocity.magnitude;
            animator.SetFloat("Speed", moveSpeed);

            UpdateFootstepSound();
        }

        // 추격 실패 후 일정 시간 대기
        private IEnumerator IdleAfterChase()
        {
            animator.SetBool("run", false);
            animator.SetBool("idle", true);
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();

            yield return new WaitForSeconds(1f);

            isPatrolling = true;
            StartCoroutine(PatrolRoutine());
        }

        // 배회 루틴 코루틴
        private IEnumerator PatrolRoutine()
        {
            while (!isChasing)
            {
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("idle", false);
                animator.SetBool("walk", true);
                animator.SetBool("run", false);

                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.5f)
                {
                    // 확률적으로 중간에 멈추기 (자연스럽게 보이도록)
                    if (Random.value < 0.005f)
                    {
                        navMeshAgent.isStopped = true;
                        animator.SetBool("walk", false);
                        animator.SetBool("idle", true);
                        yield return new WaitForSeconds(Random.Range(1f, 2f));
                        navMeshAgent.isStopped = false;
                        animator.SetBool("idle", false);
                        animator.SetBool("walk", true);
                    }
                    yield return null;
                }

                // 도착 후 잠시 대기
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;

                float randomIdleTime = Random.Range(2f, 4f);
                yield return new WaitForSeconds(randomIdleTime);
            }

            isPatrolling = false;
        }

        // 주변의 유효한 랜덤 위치 반환
        private Vector3 GetRandomNavMeshPosition()
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius + transform.position;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    NavMeshPath path = new NavMeshPath();
                    navMeshAgent.CalculatePath(hit.position, path);

                    if (path.status == NavMeshPathStatus.PathComplete &&
                        Vector3.Distance(transform.position, hit.position) > minimumMoveDistance)
                    {
                        return hit.position;
                    }
                }
            }

            return transform.position;
        }

        // 공격 처리 함수
        void Attack()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            navMeshAgent.SetDestination(transform.position); // 현재 위치 고정
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // 애니메이션 이후 Resume 안 함: 거리 체크에 따라 추격 재개됨
        }

        // 공격 후 잠시 대기 후 재이동
        private IEnumerator ResumeAfterAttack()
        {
            float idleTime = Random.Range(0.5f, 1.2f);
            yield return new WaitForSeconds(idleTime);

            animator.SetBool("idle", true);
            yield return new WaitForSeconds(0.5f);

            navMeshAgent.isStopped = false;
            animator.SetBool("idle", false);
        }

        // 발소리 재생 처리
        private void UpdateFootstepSound()
        {
            if (navMeshAgent.isStopped || navMeshAgent.velocity.magnitude < 0.1f)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                AudioClip clip = null;

                if (animator.GetBool("walk") && walkFootstepClips.Length > 0)
                {
                    clip = walkFootstepClips[Random.Range(0, walkFootstepClips.Length)];
                    footstepTimer = walkFootstepInterval;
                }
                else if (animator.GetBool("run") && runFootstepClips.Length > 0)
                {
                    clip = runFootstepClips[Random.Range(0, runFootstepClips.Length)];
                    footstepTimer = runFootstepInterval;
                }

                if (clip != null && footstepAudio != null)
                {
                    footstepAudio.PlayOneShot(clip);
                }
            }
        }
    }
}