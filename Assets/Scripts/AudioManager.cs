using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip introBGM;
    public AudioClip normalBGM;

    void Start()
    {
        audioSource.clip = introBGM;
        audioSource.loop = false;
        audioSource.Play();

        float waitTime = Mathf.Min(introBGM.length, 3f);
        Invoke("SwitchMusic", waitTime);


    }
    void SwitchMusic()
    {
        audioSource.clip = normalBGM;
        audioSource.loop = true;
        audioSource.Play();
    }

}
