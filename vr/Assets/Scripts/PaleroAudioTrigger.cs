using UnityEngine;

public class PaleroAudio : MonoBehaviour
{
    [Header("Zonas de detección")]
    public Collider zonaLejana;
    public Collider zonaCercana;

    [Header("Audios")]
    public AudioSource audioLejano;   // "¡Primo, primo, acércate!"
    public AudioSource audioCercano;  // "Ahora sí, lo que quiere"

    private bool sonidoLejanoReproducido = false;
    private bool sonidoCercanoReproducido = false;

    private void OnTriggerEnter(Collider other)
    {
        // Solo responde si el objeto tiene el tag "Barca" y un Rigidbody
        if (other.CompareTag("Barca") && other.attachedRigidbody != null)
        {
            // Si entra a la zona lejana
            if (other == zonaLejana && !sonidoLejanoReproducido)
            {
                audioLejano.Play();
                sonidoLejanoReproducido = true;
            }

            // Si entra a la zona cercana
            if (other == zonaCercana && !sonidoCercanoReproducido)
            {
                audioCercano.Play();
                sonidoCercanoReproducido = true;
            }
        }
    }
}
