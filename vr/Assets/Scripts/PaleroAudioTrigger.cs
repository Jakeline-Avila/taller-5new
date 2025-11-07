using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayAudioOnBoatTrigger : MonoBehaviour
{
    public AudioSource audioSource;
    public string boatTag = "Barca";
    public bool playOnce = true;

    bool played;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (playOnce && played) return;
        // Asegura que el que entra sea la barca y tenga Rigidbody
        if (!other.attachedRigidbody) return;
        if (!other.CompareTag(boatTag)) return;

        audioSource?.Play();
        played = true;
    }
}
