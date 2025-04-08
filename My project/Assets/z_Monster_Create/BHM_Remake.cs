using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BH_Remake : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // �Ҹ� ���� ��ũ��Ʈ�� �پ��ִ� ������Ʈ

        private Vector3 targetGo; // ������ �Ҹ� ��ġ
        private bool isChasing = false; // �߰� ������ ����

        [SerializeField] private bool chaseTarget = true; // �߰� ���� ����
        private NavMeshAgent navMeshAgent; // ����Ƽ ������̼� ������Ʈ
        private Animator animator; // �ִϸ����� ������Ʈ
        public AudioSource AiVoice; // �Ҹ� ���� �� ���� ����� ����� �ҽ�

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 30f; // ��ȸ �ݰ�
        [SerializeField] private float minimumMoveDistance = 5f; // �ּ� �̵� �Ÿ� (�ʹ� ����� ���� ����)
        private Vector3 patrolTarget; // ���� ��ȸ ��ǥ ����
        private bool isPatrolling = false; // ��ȸ ������ ����
        private float attackRange = 3.2f; // ���� ����

        private float soundMemoryDuration = 5f; // �Ҹ� ���� �� ��� ���� �ð�
        private float soundMemoryTimer = 0f; // ���� ��� Ÿ�̸�

        [Header("Footstep Sounds")]
        public AudioClip[] walkFootstepClips; // �ȴ� �߼Ҹ� ���
        public AudioClip[] runFootstepClips; // �ٴ� �߼Ҹ� ���
        public float walkFootstepInterval = 0.6f; // �ȱ� �߼Ҹ� ����
        public float runFootstepInterval = 0.4f; // �޸��� �߼Ҹ� ����

        private float footstepTimer = 0f; // ���� �߼Ҹ����� ���� �ð�
        private AudioSource footstepAudio; // �߼Ҹ��� ����� �ҽ�

        private float chaseTime = 0f; // ���� �߰� �ð�
        private float maxChaseTime = 10f; // �ִ� �߰� ���� �ð�

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // �߼Ҹ��� ����� �ҽ� �ʱ�ȭ
            footstepAudio = gameObject.AddComponent<AudioSource>();
            footstepAudio.playOnAwake = false;
            footstepAudio.spatialBlend = 1f; // 3D ����� ����

            StartCoroutine(PatrolRoutine()); // ���� �� ��ȸ ��ƾ ����
        }

        void Update()
        {
            // ���� �����ӿ��� �Ҹ� ���� Ȯ��
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

                // �÷��̾ ���� ���� ���� ���� ���
                if (distance < attackRange)
                {
                    // ���� ���°� �ƴϸ� ���� �õ�
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                    {
                        Attack();
                    }
                }
                else
                {
                    // ���� ���ε� ���� ������ ��� ��� �� ���� ����ϰ� �ٽ� �߰�
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                    {
                        // ���� �ִϸ��̼� ���߿� �߰� ��ȯ
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

        // �߰� ���� �� ���� �ð� ���
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

        // ��ȸ ��ƾ �ڷ�ƾ
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
                    // Ȯ�������� �߰��� ���߱� (�ڿ������� ���̵���)
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

                // ���� �� ��� ���
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;

                float randomIdleTime = Random.Range(2f, 4f);
                yield return new WaitForSeconds(randomIdleTime);
            }

            isPatrolling = false;
        }

        // �ֺ��� ��ȿ�� ���� ��ġ ��ȯ
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

        // ���� ó�� �Լ�
        void Attack()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            navMeshAgent.SetDestination(transform.position); // ���� ��ġ ����
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // �ִϸ��̼� ���� Resume �� ��: �Ÿ� üũ�� ���� �߰� �簳��
        }

        // ���� �� ��� ��� �� ���̵�
        private IEnumerator ResumeAfterAttack()
        {
            float idleTime = Random.Range(0.5f, 1.2f);
            yield return new WaitForSeconds(idleTime);

            animator.SetBool("idle", true);
            yield return new WaitForSeconds(0.5f);

            navMeshAgent.isStopped = false;
            animator.SetBool("idle", false);
        }

        // �߼Ҹ� ��� ó��
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