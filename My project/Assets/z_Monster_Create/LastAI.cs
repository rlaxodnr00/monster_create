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
        private AIHearing hearingScript;
        private bool isAttacking = false;

        void Start()
        {
            Debug.Log("Start");
            hearingScript = AIHearing.GetComponent<AIHearing>();
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
            soundDetectedGo = hearingScript.soundDetected;
            targetGo = hearingScript.targetObj;

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
            else if (!isPatrolling && patrolCoroutine == null)
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
            Debug.Log("StopCHsasing");
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
            Debug.Log("PatrolRoutine");
            isPatrolling = true;
            patrolCoroutine = null;

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
                float timer = 0f;
                while (!navMeshAgent.pathPending &&
                       navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance &&
                       navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                       timer < 5f)
                {
                    timer += Time.deltaTime;
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
            Debug.Log("GetSafeRandomPatrolPosition");
            // ���� ��ġ�� �ʹ� ����� ��ġ�� ����
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

        // NavMesh ���� ���� ��ġ ��ȯ �Լ�
        private Vector3 GetRandomNavMeshPosition()
        {
            Debug.Log("GetRandomNavMeshPosition");
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position; // ���� ��ġ ���� ���� ����

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
            {
                return hit.position; // ��ȿ�� ��ġ ��ȯ
            }
            Debug.LogWarning("��ȿ�� NavMesh ��ġ�� ã�� ����. ���� ��ġ�� ��ü.");
            return transform.position; // ���� �� ���� ��ġ ��ȯ
        }

        // ���� �Լ� (�ִϸ��̼� Ʈ���ſ� �̵� �ߴ� ����)
        void Attack()
        {
            Debug.Log("Attack");
            // �̹� ���� ���̸� �ߺ� ����
            if (isAttacking) return;

            isAttacking = true;
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
            Debug.Log("ResumeAfterAttack");
            yield return new WaitForSeconds(2f); // �ִϸ��̼� �ð�

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

        // �浹����
        private void OnTriggerEnter(Collider other)
        {
            // �浹 ����� player��
            if (other.CompareTag("Player"))
            {
                Debug.Log("collider player");
                float distance = Vector3.Distance(transform.position, other.transform.position);

                if (distance < attackRange)
                {
                    // �÷��̾� ������ �ٶ󺸵��� ȸ��
                    Vector3 direction = (other.transform.position - transform.position).normalized;
                    direction.y = 0f; // ���� ȸ�� ���� (ȸ�� �� ����)
                    // ���� ���� ������ ������ ���� �õ�
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        transform.rotation = lookRotation;
                    }
                    Attack();
                    chaseTimer = 0f; // �߰� ���� �� Ÿ�̸� �ʱ�ȭ
                }
            }
        }

        private IEnumerator ResumePatrolAfterDelay(float delay)
        {
            Debug.Log("ResumePatrolAfterDelay");
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
