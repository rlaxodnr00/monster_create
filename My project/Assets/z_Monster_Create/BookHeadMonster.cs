using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster : MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // �Ҹ� ���� ����� ���� ������Ʈ (AIHearing ��ũ��Ʈ�� �پ� ����)

        private bool soundDetectedGo; // �Ҹ� ���� ����
        private Vector3 targetGo; // �Ҹ��� �߻��� ��ǥ ��ġ
        private bool isChasing = false; // ���� �߰� ������ ����

        [SerializeField] private bool chaseTarget = true; // �߰� ��� ��� ���� (��Ȱ��ȭ ����)
        private NavMeshAgent navMeshAgent; // ����Ƽ ������̼� ������Ʈ (�̵� ����)
        private Animator animator; // �ִϸ��̼� ����� ������Ʈ
        public AudioSource AiVoice; // AI�� �߰� �� ���� �Ҹ�

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // ��ȸ�� �ݰ�
        //[SerializeField] private float patrolWaitTime = 3f; // �� �������� ����ϴ� �ð�
        private Vector3 patrolTarget; // ��ȸ �� �̵��� ��ǥ ��ġ
        private bool isPatrolling = false; // ���� ��ȸ ������ ����
        private float attackRange = 4f; // ���� ��Ÿ�

        void Start()
        {
            // ������Ʈ �ʱ�ȭ
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // ��ȸ ��ƾ ����
            StartCoroutine(PatrolRoutine());
        }

        void Update()
        {
            // AIHearing���� �Ҹ� ���� ���� �� ��� ��ġ ��������
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            // �Ҹ� ���� & �߰� ����� ���� ���� ���
            if (soundDetectedGo && chaseTarget)
            {
                // AI ���� ���
                if (!AiVoice.isPlaying)
                    AiVoice.Play();

                // ��ȿ�� ��ġ�� ������ ���
                if (targetGo != Vector3.zero)
                {
                    isChasing = true;      // �߰� ����
                    isPatrolling = false;  // ��ȸ �ߴ�

                    float distance = Vector3.Distance(transform.position, targetGo);
                    if (distance < attackRange)
                    {
                        Attack(); // ��Ÿ� ���� ���� ����
                    }
                    else
                    {
                        // �޸��� �ִϸ��̼�
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.isStopped = false;
                        navMeshAgent.SetDestination(targetGo); // ��� ��ġ�� �̵�
                    }
                }
            }
            // �߰� ���̾��µ� �� �̻� ����� ������ ���߱�
            else if (isChasing)
            {
                isChasing = false;

                animator.SetBool("run", false);
                animator.SetBool("idle", true);

                navMeshAgent.isStopped = true; // ������Ʈ ����
                navMeshAgent.ResetPath();      // �̵� ��� �ʱ�ȭ
            }
            // �߰� ���� �ƴϰ� ��ȸ ���� �ƴ� ���, ��ȸ ����
            else
            {
                if (!isPatrolling)
                {
                    isPatrolling = true;
                    StartCoroutine(PatrolRoutine());
                }

                // idle ������ �� ������ �̵� ���� �� ��� �ʱ�ȭ
                if (animator.GetBool("idle"))
                {
                    if (!navMeshAgent.isStopped)
                        navMeshAgent.isStopped = true;

                    if (navMeshAgent.hasPath)
                        navMeshAgent.ResetPath();
                }
            }
        }

        // ��ȸ ��ƾ (���������� ���� ��ġ�� �̵�)
        private IEnumerator PatrolRoutine()
        {
            while (!isChasing) // �߰� ���� �ƴ� ���� ����
            {
                // ������ ��ġ ��� �� �̵� ����
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                // �ȱ� �ִϸ��̼� ����
                animator.SetBool("walk", true);
                animator.SetBool("run", false);
                animator.SetBool("idle", false);

                // �������� ������ ������ ���
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                // ���� �� idle ���� ��ȯ �� ��� ���
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);

                // �̵� ���߰� ��� �ʱ�ȭ
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;

                float randomIdleTime = Random.Range(1f, 2f); // 1�� ~ 3�� ���� ���� �ð�
                yield return new WaitForSeconds(randomIdleTime);
            }

            isPatrolling = false; // ��ƾ ���� �� ��ȸ ���� false��
        }

        // NavMesh �ȿ��� ������ ��ġ ����
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // ��ȿ�� ��ġ ��ȯ
            }

            return transform.position; // ���� �� ���� ��ġ ��ȯ
        }

        // ���� ���� �Լ�
        void Attack()
        {
            // ���� ���� �ִϸ��̼��� ��� ���̸� �ߺ� ����
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                return;

            // �̵� ���߱�
            navMeshAgent.SetDestination(transform.position);
            navMeshAgent.isStopped = true;

            // Ʈ���� �ʱ�ȭ �� �ٽ� �����Ͽ� �ִϸ��̼� ���
            animator.ResetTrigger("attack");
            animator.SetTrigger("attack");

            // ���� �ð� �� �̵� �簳
            StartCoroutine(ResumeAfterAttack());
        }

        // ���� �� �̵� �簳 ��ƾ
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(1.5f); // ���� �ִϸ��̼� ���̸�ŭ ���
            navMeshAgent.isStopped = false;
        }
    }
}
