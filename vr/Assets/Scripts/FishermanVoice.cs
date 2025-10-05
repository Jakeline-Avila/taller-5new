using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class FishermanVoice : MonoBehaviour
{
    [Header("Clips que puede decir")]
    public AudioClip[] voiceClips;

    [Header("Tiempos entre frases")]
    public float minDelay = 8f;
    public float maxDelay = 15f;

    AudioSource source;
    Coroutine talkRoutine;
    bool isTalking;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.spatialBlend = 1f;  // 3D
        source.playOnAwake = false;
    }

    public void SetTalking(bool value)
    {
        if (value == isTalking) return;
        isTalking = value;

        if (isTalking)
        {
            if (talkRoutine != null) StopCoroutine(talkRoutine);
            talkRoutine = StartCoroutine(TalkLoop());
        }
        else
        {
            if (talkRoutine != null) StopCoroutine(talkRoutine);
            talkRoutine = null;
            source.Stop();
        }
    }

    IEnumerator TalkLoop()
    {
        // Pequeño delay inicial para que no hable instantáneo al montarse
        yield return new WaitForSeconds(Random.Range(1.0f, 2.5f));

        while (isTalking)
        {
            if (voiceClips != null && voiceClips.Length > 0)
            {
                source.clip = voiceClips[Random.Range(0, voiceClips.Length)];
                source.Play();
            }
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }
}
