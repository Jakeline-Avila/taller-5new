using UnityEngine;

public class BoatMountZone : MonoBehaviour
{
    [Tooltip("Componente que hará hablar/callar al pescador")]
    public FishermanVoice fisherman;

    [Tooltip("Solo reaccionar a objetos con este tag (pon el tag Player al XR Origin)")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag))
            fisherman?.SetTalking(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag) || other.CompareTag(playerTag))
            fisherman?.SetTalking(false);
    }
}
