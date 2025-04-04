using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster_controller : MonoBehaviour
    {
        // AIHearing ��ũ��Ʈ�� ���� ������Ʈ�� �巡���Ͽ� ����
        [Tooltip("AIHearing ��ũ��Ʈ�� ���� ������Ʈ�� ���⿡ �巡���ϼ���")]
        [SerializeField]
        private GameObject AIHearing;

        // �Ҹ��� �����ߴ��� ����
        private bool soundDetectedGo;

        // �Ҹ��� ��ġ (�̵��� ��ǥ ��ġ)
        private Vector3 targetGo;

        // �Ҹ��� �������� ���� (true�� ����, false�� ����)
        [SerializeField]
        private bool chaseTarget = true;

        // �׺�޽� ������Ʈ ������Ʈ (�̵��� ���)
        private NavMeshAgent navMeshAgent;

        // AI�� �Ҹ��� �����ϰ� ���� �� ����� ����� �ҽ�
        public AudioSource AiVoice;

        void Start()
        {
            // NavMeshAgent ������Ʈ�� ������ (�̵� �����)
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            // AIHearing ��ũ��Ʈ���� �Ҹ� ���� ���θ� ������
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;

            // �Ҹ��� �����߰�, �����ϵ��� �����Ǿ� ������
            if (soundDetectedGo && chaseTarget)
            {
                // �Ҹ� ���� �� ���� ���
                AiVoice.Play();

                // �Ҹ� �߻� ��ġ(Ÿ�� ��ġ)�� ������
                targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

                // Ÿ�� ��ġ�� null�� �ƴ϶��
                if (targetGo != null)
                {
                    // Ÿ���� �ٶ󺸰� ȸ��
                    transform.LookAt(targetGo);

                    // Ÿ�� ��ġ�� �̵��ϵ��� NavMeshAgent�� ���
                    navMeshAgent.SetDestination(targetGo);
                }
            }
        }
    }
}
