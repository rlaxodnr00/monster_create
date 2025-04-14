using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class lastAI : MonoBehaviour
    {
        // --------------------[���� �� ���� ����]--------------------

        [Tooltip("Drag AIHearing script object here")]
        [SerializeField] private GameObject AIHearing; // �Ҹ� ������ ����ϴ� ��ũ��Ʈ�� ���� ������Ʈ
        private AIHearing hearingScript; // �ش� ������Ʈ���� ������ AIHearing ������Ʈ

        [SerializeField] private bool chaseTarget = true; // �߰� ��� Ȱ��ȭ ����

        public AudioSource AiVoice; // �߰� ���� �� ����Ǵ� ����

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // ��ȸ ����
        [SerializeField] private float patrolWaitTime = 1.5f; // ��ȸ ���� ���� �� ��� �ð�

        private NavMeshAgent navMeshAgent; // �׺���̼� �̵� ó����
        private Animator animator; // �ִϸ��̼� �����

        // --------------------[���� ����]--------------------

        private bool soundDetectedGo; // �Ҹ� ���� ����
        private Vector3 targetGo; // ������ �Ҹ��� ��ġ (�÷��̾� ��ġ)
        private bool isChasing = false; // ���� �߰� ������ ����
        private bool isPatrolling = false; // ���� ��ȸ ������ ����
        private bool isAttacking = false; // ���� ���� ������ ����

        private Vector3 patrolTarget; // ��ȸ ��ǥ ��ġ
        private Coroutine patrolCoroutine = null; // ��ȸ ��ƾ �����

        private float attackRange = 3.5f; // ���� ���� �Ÿ�

        // --------------------[�߰� ���� ��Ÿ��]--------------------

        private float voiceCooldown = 5f; // �߰� ���� ��� ����
        private float lastVoiceTime = -Mathf.Infinity; // ������ �߰� ���� ��� �ð�

        // --------------------[�߰� ���� ����]--------------------

        private float chaseTimeout = 10f; // �߰� ���� �ð� ����
        private float chaseTimer = 0f; // ���� �߰� ��� �ð�

        // --------------------[�ʱ�ȭ]--------------------

        void Start()
        {
            Debug.Log("Start");

            // ������Ʈ ��������
            hearingScript = AIHearing.GetComponent<AIHearing>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // ��ȸ ��ƾ ����
            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }

        // --------------------[�� ������ ó��]--------------------

        void Update()
        {
            // �Ҹ� ���� ���� �� ��ġ �޾ƿ���
            soundDetectedGo = hearingScript.soundDetected;
            targetGo = hearingScript.targetObj;

            // �Ҹ��� �������� ��
            if (soundDetectedGo && chaseTarget)
            {
                // ��Ÿ�� ������ ���� ���
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
                        // ���� ���� �ȿ� ���� ���
                        Attack();
                        chaseTimer = 0f;
                    }
                    else
                    {
                        // �߰� �� �ִϸ��̼� ���� �� �̵�
                        animator.SetBool("walk", false);
                        animator.SetBool("run", true);
                        animator.SetBool("idle", false);

                        navMeshAgent.SetDestination(targetGo);
                        chaseTimer += Time.deltaTime;

                        if (chaseTimer > chaseTimeout)
                        {
                            // �߰� �ð� �ʰ� �� �ߴ�
                            StopChasing();
                        }
                    }
                }
            }
            else if (isChasing)
            {
                // �߰� ���̾����� �� �̻� �߰� ��� ����
                StopChasing();
            }
            else if (!isPatrolling && patrolCoroutine == null)
            {
                // �ƹ��͵� ���ϰ� �ִٸ� �ٽ� ��ȸ ����
                patrolCoroutine = StartCoroutine(PatrolRoutine());
            }

            // �ȱ� �ִϸ��̼� ���� ����
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

        // --------------------[�߰� ���� ó��]--------------------

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

            // ��� ��� �� ��ȸ �簳
            StartCoroutine(ResumePatrolAfterDelay(1f));
        }

        // --------------------[��ȸ ��ƾ]--------------------

        private IEnumerator PatrolRoutine()
        {
            Debug.Log("PatrolRoutine");

            isPatrolling = true;
            patrolCoroutine = null;

            while (!isChasing)
            {
                // ���� ��ȸ ��ġ ����
                patrolTarget = GetRandomNavMeshPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
                animator.SetBool("run", false);

                float timer = 0f;

                // ��ǥ ���ޱ��� ���
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

        // �ʹ� ����� ��ġ�� ���ؼ� ���� ��ġ ����
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

        // NavMesh ���� ��ȿ�� ���� ��ġ ��ȯ
        private Vector3 GetRandomNavMeshPosition()
        {
            Debug.Log("GetRandomNavMeshPosition");

            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
                return hit.position;

            Debug.LogWarning("��ȿ�� NavMesh ��ġ�� ã�� ����. ���� ��ġ�� ��ü.");
            return transform.position;
        }

        // --------------------[���� ó��]--------------------

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

        // ���� �� ���� ����
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

        // --------------------[�浹 ���� - ���� ���� ����]--------------------

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

        // ��ȸ �簳 ���� ó��
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
