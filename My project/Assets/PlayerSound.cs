using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioSource audioSource;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ���콺 ���� ��ư Ŭ��
        {
            audioSource.Play();
        }
    }
}