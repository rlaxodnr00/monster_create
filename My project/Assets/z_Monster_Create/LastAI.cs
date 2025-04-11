using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class lastAI : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // �Ҹ� ���� ������Ʈ�� ���� ������Ʈ

        private bool soundDetectedGo; // �Ҹ� ���� ����
        private Vector3 targetGo; // ������ �Ҹ��� ��ġ (�÷��̾� ��ġ)
        private bool isChasing = false; // �߰� �� ����

        [SerializeField] private bool chaseTarget = true; // �߰� ��� Ȱ��ȭ ����
        private NavMeshAgent navMeshAgent; // ���� �̵� ����� NavMeshAgent
        private Animator animator; // �ִϸ��̼� ����� Animator
        public AudioSource AiVoice; // �߰� ���� �� ����Ǵ� AI ����

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // ��ȸ ����
        [SerializeField] private float patrolWaitTime = 1.5f; // �� �������� ��� �ð�
        private Vector3 patrolTarget; // ��ȸ �� �̵��� ��ǥ ��ġ
        private bool isPatrolling = false; // ���� ��ȸ ������ ����
        private float attackRange = 3.5f; // ���� ���� �Ÿ�

        // �߰� ���� ��Ÿ�� ���� ����
        private float voiceCooldown = 5f; // ��Ÿ�� ���� (5��)
        private float lastVoiceTime = -Mathf.Infinity; // ������ ���� ��� ����

        // �߰� ���� �ð� ���� (Ÿ�� ���� ���ϸ� �߰� �ߴ�)
        private float chaseTimeout = 10f; // �߰� �ִ� ���� �ð�
        private float chaseTimer = 0f; // �߰� ��� �ð�

        private Coroutine patrolCoroutine = null;

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            /* // ������
            if (navMeshAgent == null)
                Debug.LogError(" NavMeshAgent�� �����ϴ�!");
            if (animator == null)
                Debug.LogError(" Animator�� �����ϴ�!");
            */

            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }

        void Update()
        {
            // AIHearing���κ��� �Ҹ� ���� ���ο� ��ǥ ��ġ �޾ƿ���
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            // �Ҹ��� �����ǰ� �߰� ������ ���
            if (soundDetectedGo && chaseTarget)
            {
                // ���� ��Ÿ�� üũ
                if (Time.time - lastVoiceTime >= voiceCooldown)
                {
                    AiVoice.Play(); // �߰� ���� ���� ���
                    lastVoiceTime = Time.time; // ������ ��� �ð� ����
                }

                // Ÿ�� ��ġ�� ��ȿ�� ���
                if (targetGo != Vector3.zero)
                {
                    isChasing = true;
                    isPatrolling = false; // ��ȸ �ߴ�

                    float distance = Vector3.Distance(transform.position, targetGo);

                    if (distance < attackRange)
                    {
                        // ���� ���� ������ ������ ���� �õ�
                        Attack();
                        chaseTimer = 0f; // �߰� ���� �� Ÿ�̸� �ʱ�ȭ
                    }
                    else
                    {
                        // ���� ���� ������ �ƴϸ� ��� �߰�
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.SetDestination(targetGo); // Ÿ�� ��ġ�� �̵�

                        chaseTimer += Time.deltaTime;

                        // ���� �ð� �̻� �������� ���ϸ� �߰� ����
                        if (chaseTimer > chaseTimeout)
                        {
                            StopChasing(); // �߰� ����
                        }
                    }
                }
            }
            // ������ �߰� ���̾����� ������ ����� ���� ���
            else if (isChasing)
            {
                StopChasing(); // �߰� �ߴ� ó��
            }
            // �ƹ� ���µ� �ƴ� ��� ��ȸ ��ƾ ����
            else if (!isPatrolling)
            {
                patrolCoroutine = StartCoroutine(PatrolRoutine());
                // StartCoroutine(PatrolRoutine());
            }

            // �̵� ���¿� �ִϸ��̼� ���¸� ��ġ��Ű�� ���� ���� ó��
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
                    // ��ǥ ������ �ʹ� ������� �����ִ� ���� ����
                    if (patrolCoroutine == null)
                    {
                        patrolCoroutine = StartCoroutine(PatrolRoutine());
                    }
                }
            }
        }

        // �߰��� �����ϰ� idle ���·� ��ȯ
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
                        
            // ���� �ð� �� �ٽ� ��ȸ ����
            StartCoroutine(ResumePatrolAfterDelay(1f));
        }

        // ��ȸ ��ƾ (���� ��ġ�� �̵��ϰ� ��� ���)
        private IEnumerator PatrolRoutine()
        {
            isPatrolling = true;

            while (!isChasing)
            {
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                // ������ ���� ���� walk �ִϸ��̼� ON
                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
                animator.SetBool("run", false);

                // ������ �������� ���
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                {
                    yield return null;
                }

                // ���������� �̵� ���߰� idle �ִϸ��̼�
                navMeshAgent.ResetPath();
                animator.SetBool("walk", false);
                //animator.SetBool("idle", true);

                yield return new WaitForSeconds(patrolWaitTime);
            }

            isPatrolling = false;
        }
        private Vector3 GetSafeRandomPatrolPosition()
        {
            // ���� ��ġ�� �ʹ� ����� ��ġ�� ����
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

        // NavMesh ���� ���� ��ġ ��ȯ �Լ�
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position; // ���� ��ġ ���� ���� ����

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // ��ȿ�� ��ġ ��ȯ
            }
            return transform.position; // ���� �� ���� ��ġ ��ȯ
        }

        // ���� �Լ� (�ִϸ��̼� Ʈ���ſ� �̵� �ߴ� ����)
        void Attack()
        {
            //Debug.Log(">> ���� ����");
            // �̹� ���� ���̸� �ߺ� ����
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

            // ���� ��ġ���� ����
            navMeshAgent.SetDestination(transform.position);
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // ���� �� ����
            StartCoroutine(ResumeAfterAttack());
        }

        // ���� �ִϸ��̼��� ���� �� �ٽ� �̵�
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(2f); // �ִϸ��̼� �ð�

            navMeshAgent.isStopped = false;

            if (!isChasing)
            {
                patrolTarget = GetSafeRandomPatrolPosition();
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
            }
        }

        // �浹����
        private void OnTriggerEnter(Collider other)
        {
            // �浹 ����� player��
            if (other.CompareTag("Player"))
            {
                float distance = Vector3.Distance(transform.position, other.transform.position);

                if (distance < attackRange)
                {
                    // ���� ���� ������ ������ ���� �õ�
                    Attack();
                    chaseTimer = 0f; // �߰� ���� �� Ÿ�̸� �ʱ�ȭ
                }
            }
        }
        private IEnumerator ResumePatrolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // ���� ��ƾ �ߺ� ����
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
