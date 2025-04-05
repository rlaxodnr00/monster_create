using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource audioSource;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭
        {
            audioSource.Play();
        }
    }
}