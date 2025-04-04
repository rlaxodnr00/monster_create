using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSoundDetect.Extra
{
    public class BookHeadMonster_controller : MonoBehaviour
    {
        // AIHearing 스크립트를 가진 오브젝트를 드래그하여 연결
        [Tooltip("AIHearing 스크립트를 가진 오브젝트를 여기에 드래그하세요")]
        [SerializeField]
        private GameObject AIHearing;

        // 소리를 감지했는지 여부
        private bool soundDetectedGo;

        // 소리의 위치 (이동할 목표 위치)
        private Vector3 targetGo;

        // 소리를 추적할지 여부 (true면 추적, false면 무시)
        [SerializeField]
        private bool chaseTarget = true;

        // 네비메시 에이전트 컴포넌트 (이동을 담당)
        private NavMeshAgent navMeshAgent;

        // AI가 소리를 감지하고 말할 때 재생할 오디오 소스
        public AudioSource AiVoice;

        void Start()
        {
            // NavMeshAgent 컴포넌트를 가져옴 (이동 제어용)
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            // AIHearing 스크립트에서 소리 감지 여부를 가져옴
            soundDetectedGo = AIHearing.GetComponent<AIHearing>().soundDetected;

            // 소리를 감지했고, 추적하도록 설정되어 있으면
            if (soundDetectedGo && chaseTarget)
            {
                // 소리 감지 시 음성 재생
                AiVoice.Play();

                // 소리 발생 위치(타겟 위치)를 가져옴
                targetGo = AIHearing.GetComponent<AIHearing>().targetObj;

                // 타겟 위치가 null이 아니라면
                if (targetGo != null)
                {
                    // 타겟을 바라보게 회전
                    transform.LookAt(targetGo);

                    // 타겟 위치로 이동하도록 NavMeshAgent에 명령
                    navMeshAgent.SetDestination(targetGo);
                }
            }
        }
    }
}
