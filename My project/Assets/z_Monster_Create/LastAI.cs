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

        

        [Header("AI Patrol Settings")]
        [SerializeField] private float patrolRadius = 10f; // ��ȸ ����
        [SerializeField] private float patrolWaitTime = 1.5f; // ��ȸ ���� ���� �� ��� �ð�

        private NavMeshAgent navMeshAgent; // �׺���̼� �̵� ó����
        private Animator animator; // �ִϸ��̼� �����


        // --------------------[�߼Ҹ�] -----------------

        [Tooltip("Monster foot step")]
        [SerializeField] private AudioClip[] walkSounds; // �ȱ� ���� 5��
        [SerializeField] private AudioClip[] runSounds;  // �޸��� ���� 5��
        [SerializeField] private AudioSource movementAudioSource; // ���带 ����� AudioSource
        
        private float footstepDelay = 0.6f;                 // �߼Ҹ� �� ����
        private float lastFootstepTime = 0f;

        // --------------------[���� �Ҹ�]--------------------

        [Header("Monseter attack sound")]
        [SerializeField] private AudioClip attackSound;           // ���� ����
        [SerializeField] private AudioSource attackAudioSource;   // ���� ����� ����� �ҽ�

        public AudioSource monseterVoice; // �߰� ���� �� ����Ǵ� ����

        // --------------------[���� �ӵ�]------------------

        [Header("AI Movement Speeds")]
        [SerializeField] private float walkSpeed = 2f; // �ȴ� �ӵ�
        [SerializeField] private float runSpeed = 4f;  // �ٴ� �ӵ�

        

        // --------------------[���� ����]--------------------

        private bool soundDetectedGo; // �Ҹ� ���� ����
        private Vector3 targetGo; // ������ �Ҹ��� ��ġ (�÷��̾� ��ġ)
        private bool isChasing = false; // ���� �߰� ������ ����
        private bool isPatrolling = false; // ���� ��ȸ ������ ����
        private bool isAttacking = false; // ���� ���� ������ ����

        private Vector3 patrolTarget; // ��ȸ ��ǥ ��ġ
        private Coroutine patrolCoroutine = null; // ��ȸ ��ƾ �����

        private float attackRange = 3.3f; // ���� ���� �Ÿ�

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

            // ȸ�� ����
            navMeshAgent.updateRotation = true; // �ڵ� ȸ�� Ȱ��ȭ
            navMeshAgent.angularSpeed = 180f;   // ȸ�� �ӵ� (�⺻���� 120, �ʹ� ������ ������ ȸ����)

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
                    monseterVoice.Play();
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

                        navMeshAgent.speed = runSpeed;
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
            TryPlayFootstep(); // �߼Ҹ�
        }

        // --------------------[�߰� ���� ó��]--------------------

        private void StopChasing()
        {
            Debug.Log("StopChasing");

            isChasing = false;
            chaseTimer = 0f;
            lastVoiceTime = -Mathf.Infinity;

            navMeshAgent.isStopped = true;

            animator.SetBool("run", false);
            animator.SetBool("walk", false);
            animator.SetBool("idle", true);

            // 1�� �� ��ȸ ��ƾ ����
            StartCoroutine(ResumePatrolAfterDelay(1f));
            navMeshAgent.isStopped = false;
        }


        // --------------------[��ȸ ��ƾ]--------------------

        private IEnumerator PatrolRoutine()
        {
            navMeshAgent.speed = walkSpeed;
            isPatrolling = true;

            while (!isChasing)
            {
                Debug.Log("PatrolRoutine ���� ��");

                patrolTarget = GetSafeRandomPatrolPosition();
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(patrolTarget);

                animator.SetBool("walk", true);
                animator.SetBool("idle", false);
                animator.SetBool("run", false);

                float waitTimer = 0f;
                bool reached = false;

                while (!reached && waitTimer < 10f)
                {
                    if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                    {
                        reached = true;
                    }

                    waitTimer += Time.deltaTime;
                    yield return null;
                }

                navMeshAgent.ResetPath();
                animator.SetBool("walk", false);
                animator.SetBool("idle", true);

                yield return new WaitForSeconds(patrolWaitTime);
            }

            isPatrolling = false;
            patrolCoroutine = null;
        }


        // �ʹ� ����� ��ġ�� ���ؼ� ���� ��ġ ����
        private Vector3 GetSafeRandomPatrolPosition()
        {
            Debug.Log("GetSafeRandomPatrolPosition");

            Vector3 randomPosition;
            int maxAttempts = 30;
            int attempts = 0;

            do
            {
                randomPosition = GetRandomNavMeshPosition();
                attempts++;
            } while (Vector3.Distance(transform.position, randomPosition) < 2f && attempts < maxAttempts);

            Debug.Log($"���õ� ��ȸ ��ġ: {randomPosition} (�õ� Ƚ��: {attempts})");

            return randomPosition;
        }

        // NavMesh ���� ��ȿ�� ���� ��ġ ��ȯ
        private Vector3 GetRandomNavMeshPosition()
        {
            Debug.Log("GetRandomNavMeshPosition");

            for (int i = 0; i < 30; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                randomDirection += transform.position;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius * 2f, NavMesh.AllAreas))
                {
                    Debug.DrawRay(hit.position, Vector3.up * 2, Color.green, 1.0f); // ���� ��ġ �ð�ȭ
                    return hit.position;
                }
            }

            Debug.LogWarning("��ȿ�� NavMesh ��ġ�� 30ȸ �õ������� ã�� ���߽��ϴ�. ���� ��ġ ��ȯ.");
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

            // ���� ���
            if (attackSound != null && attackAudioSource != null)
            {
                attackAudioSource.clip = attackSound;
                attackAudioSource.Play();
            }

            // �ִϸ��̼��� ����Ǹ� ������ ó��
            Invoke("DealDamage", 0.5f); // 0.5�� �� �������� �ִ� �Լ� ȣ�� (�ִϸ��̼��� Ÿ�ֿ̹� ���缭)

            StartCoroutine(ResumeAfterAttack());
        }

        // ���� �� ���� ����
        private IEnumerator ResumeAfterAttack()
        {
            Debug.Log("ResumeAfterAttack");

            navMeshAgent.speed = walkSpeed;
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

            if (!isChasing && patrolCoroutine == null)
            {
                patrolCoroutine = StartCoroutine(PatrolRoutine());
            }
        }


        // �ȴ� �Ҹ�
        private void PlayWalkSound()
        {
            if (walkSounds.Length == 0 || movementAudioSource.isPlaying) return;

            int index = Random.Range(0, walkSounds.Length);
            movementAudioSource.clip = walkSounds[index];
            movementAudioSource.Play();
        }

        // �ٴ� �Ҹ�
        private void PlayRunSound()
        {
            if (runSounds.Length == 0 || movementAudioSource.isPlaying) return;

            int index = Random.Range(0, runSounds.Length);
            movementAudioSource.clip = runSounds[index];
            movementAudioSource.Play();
        }

        // �߼Ҹ� ���� ����
        private void TryPlayFootstep()
        {
            if (Time.time - lastFootstepTime < footstepDelay) return;

            if (animator.GetBool("walk") && !isChasing)
                PlayWalkSound();
            else if (animator.GetBool("run") && isChasing)
                PlayRunSound();

            lastFootstepTime = Time.time;
        }

        private void DealDamage()
        {
            // ������ ���� ������ ���� �������� �ִ� �ڵ�
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, 1.5f);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {   
                    /* �÷��̾� ����
                    PlayerHealth player = hitCollider.GetComponent<PlayerHealth>();
                    if (player != null)
                    {
                        player.TakeDamage(10); // ��: �÷��̾ �������� �ִ� �Լ�
                    }
                    */
                }
            }
        }


    }
}
