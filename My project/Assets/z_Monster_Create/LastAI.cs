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
        [SerializeField] private float patrolWaitTime = 3f; // �� �������� ��� �ð�
        private Vector3 patrolTarget; // ��ȸ �� �̵��� ��ǥ ��ġ
        private bool isPatrolling = false; // ���� ��ȸ ������ ����
        private float attackRange = 4f; // ���� ���� �Ÿ�

        // �߰� ���� ��Ÿ�� ���� ����
        private float voiceCooldown = 5f; // ��Ÿ�� ���� (5��)
        private float lastVoiceTime = -Mathf.Infinity; // ������ ���� ��� ����

        // �߰� ���� �ð� ���� (Ÿ�� ���� ���ϸ� �߰� �ߴ�)
        private float chaseTimeout = 10f; // �߰� �ִ� ���� �ð�
        private float chaseTimer = 0f; // �߰� ��� �ð�

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // �̵� ������Ʈ ��������
            animator = GetComponent<Animator>(); // �ִϸ����� ������Ʈ ��������
            StartCoroutine(PatrolRoutine()); // �������ڸ��� ��ȸ ��ƾ ����
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
                StartCoroutine(PatrolRoutine());
            }

            // �̵� ���¿� �ִϸ��̼� ���¸� ��ġ��Ű�� ���� ���� ó��
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
                }
            }
        }

        // �߰��� �����ϰ� idle ���·� ��ȯ
        private void StopChasing()
        {
            isChasing = false;
            chaseTimer = 0f;
            lastVoiceTime = -Mathf.Infinity; // ��Ÿ�� �ʱ�ȭ

            navMeshAgent.isStopped = true; // �̵� ���� ����

            animator.SetBool("run", false);
            animator.SetBool("walk", false); // Ȥ�� walk �ɷ����� ���� ������
            animator.SetBool("idle", true);
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
                animator.SetBool("idle", true);

                yield return new WaitForSeconds(patrolWaitTime);
            }

            isPatrolling = false;
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
            // �̹� ���� ���̶�� �ߺ� ���� ����
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

            navMeshAgent.SetDestination(transform.position); // �̵� ����
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack"); // Ʈ���� �ʱ�ȭ
            animator.SetTrigger("attack");   // ���� Ʈ���� ����

            StartCoroutine(ResumeAfterAttack()); // ���� �� �̵� �簳
        }

        // ���� �ִϸ��̼��� ���� �� �ٽ� �̵�
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(1.5f); // ���� �ִϸ��̼� �ð���ŭ ���
            navMeshAgent.isStopped = false; // �̵� �ٽ� ���
        }
    }
}
