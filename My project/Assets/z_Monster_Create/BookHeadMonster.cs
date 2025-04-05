using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster: MonoBehaviour
    {
        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // �Ҹ��� �����ϴ� AIHearing ��ü

        private bool soundDetectedGo; // �Ҹ��� �����Ǿ����� ����
        private Vector3 targetGo; // �߰��� ��ǥ�� ��ġ
        private bool isChasing = false; // �߰� ������ ����

        [SerializeField] private bool chaseTarget = true; // �߰� ��� Ȱ��ȭ ����
        private NavMeshAgent navMeshAgent; // �׺���̼� �޽� ������Ʈ (�̵� AI)
        private Animator animator; // �ִϸ����� ��Ʈ�ѷ�
        public AudioSource AiVoice; // AI�� �Ҹ� ��� (�߰��� �� ���)

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // ��ȸ ����
        [SerializeField] private float patrolWaitTime = 3f; // ��ȸ �� ��� �ð�
        private Vector3 patrolTarget; // ��ȸ�� ��ǥ ��ġ
        private bool isPatrolling = false; // ��ȸ ������ ����
        private float attackRange = 4f;

        void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>(); // NavMeshAgent ������Ʈ ��������
            animator = GetComponent<Animator>(); // Animator ��������
            StartCoroutine(PatrolRoutine()); // ��ȸ ��ƾ ����
        }

        void Update()
        {
            // �Ҹ� ���� ���� Ȯ��
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;
            targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

            if (soundDetectedGo && chaseTarget)
            {
                if (!AiVoice.isPlaying)
                {
                    AiVoice.Play(); // �Ҹ� ���� �� AI �Ҹ� ���
                }

                if (targetGo != Vector3.zero)
                {
                    isChasing = true; // �߰� ����
                    isPatrolling = false; // ��ȸ �ߴ�

                    float distance = Vector3.Distance(transform.position, targetGo);
                    if (distance < attackRange) // ���� ���� ���� ���� ���
                    {
                        Attack(); // ���� ����
                    }
                    else
                    {
                        // ��ǥ�� ���� �̵�
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);
                        navMeshAgent.SetDestination(targetGo);
                    }
                    // ��ǥ�� ���� �̵�
                    navMeshAgent.SetDestination(targetGo);
                }
            }
            else if (isChasing)
            {
                // ��ǥ�� ���ƴٸ� idle ���·� ����
                isChasing = false;
                animator.SetBool("run", false);
                animator.SetBool("idle", true);
            }
            else if (!isPatrolling)
            {
                StartCoroutine(PatrolRoutine());
            }
            /* ������ 
            if (soundDetectedGo && chaseTarget)
            {
                Debug.Log("�Ҹ� ������ + �߰� Ȱ��ȭ��");

                if (targetGo != Vector3.zero)
                {
                    float distance = Vector3.Distance(transform.position, targetGo);
                    Debug.Log("�÷��̾���� �Ÿ�: " + distance);

                    if (distance < attackRange)
                    {
                        Debug.Log("���� ������ ����!");
                        Attack(); // �ٵ� �� ������?
                    }
                    else
                    {
                        Debug.Log("���� ���� ���� ����");
                    }
                }
            }
            */


        }

        // ��ȸ ��ƾ (������ ��ġ�� �̵�)
        private IEnumerator PatrolRoutine()
        {
            isPatrolling = true;
            while (!isChasing)
            {
                patrolTarget = GetRandomNavMeshPosition(); // ������ ��ġ ����
                navMeshAgent.SetDestination(patrolTarget);

                // �ִϸ��̼��� 'walk'�� ����
                animator.SetBool("walk", true);
                animator.SetBool("run", false);
                animator.SetBool("idle", false);

                // �������� ������ ������ ���
                while (!navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                // �����ϸ� 'idle' �ִϸ��̼� ���� �� ���
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);
                yield return new WaitForSeconds(patrolWaitTime);
            }
            isPatrolling = false;
        }

        // NavMesh ������ ���� ��ġ ��ȯ
        private Vector3 GetRandomNavMeshPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position; // ���� ��ġ���� ���� �������� ����

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
            {
                return hit.position; // ��ȿ�� ��ġ ��ȯ
            }
            return transform.position; // �����ϸ� ���� ��ġ ��ȯ
        }
        // ���� ���� �Լ�
        void Attack()
        {
            
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

            navMeshAgent.SetDestination(transform.position); // �̵� ����
            navMeshAgent.isStopped = true;

            animator.ResetTrigger("attack"); // Ʈ���� �ʱ�ȭ ��
            animator.SetTrigger("attack");  // �ٽ� ����

            StartCoroutine(ResumeAfterAttack());
        }

        // ���� �ִϸ��̼��� ���� �� �̵� �簳
        private IEnumerator ResumeAfterAttack()
        {
            yield return new WaitForSeconds(1.5f); // ���� �ִϸ��̼� ���̿� �°� ����
            navMeshAgent.isStopped = false; // �̵� Ȱ��ȭ
        }

    }
}
