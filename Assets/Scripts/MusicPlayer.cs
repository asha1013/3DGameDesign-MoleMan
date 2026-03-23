using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class MusicPlayer : MonoBehaviour
{
public AudioClip trackOne;
[UnityEngine.Range(0f, 1f)] public float trackOneVolume = 1f;
public AudioClip trackTwo;
[UnityEngine.Range(0f, 1f)] public float trackTwoVolume = 1f;
public AudioClip bossStinger;
public AudioClip bossLoop;
public AudioSource musicSource;
public bool playOnStart=true;

 public static MusicPlayer instance;
public static MusicPlayer Instance => instance;

    public void Start()
    {
        if (playOnStart) StartCoroutine(FadeIn(1,.5f));
    }

    public IEnumerator FadeOut(float fadeTime)
    {
        float timer = 0f;
        float startVolume = musicSource.volume;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }



    public IEnumerator FadeIn(int index, float fadeTime, bool looping=true)
    {
        float vol = 1f;
        if (index==1){musicSource.clip = trackOne; vol=trackOneVolume;}
        else if (index==2){musicSource.clip = trackTwo; vol=trackTwoVolume;}
        musicSource.loop = looping;
        musicSource.volume = 0f;
        musicSource.Play();

        float timer = 0f;        

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, vol, timer / fadeTime);
            yield return null;
        }

        musicSource.volume = vol;
    }





}
