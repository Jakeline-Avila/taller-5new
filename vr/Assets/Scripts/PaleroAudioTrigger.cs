using UnityEngine;

public class PaleroAudio : MonoBehaviour
{
    [Header("Zonas de detección")]
    public Collider zonaLejana;
    public Collider zonaCercana;

    [Header("Audios")]
    public AudioSource audioLejano;   // "¡Primo, primo, acércate!"
    public AudioSource audioCercano;  // "Ahora sí, lo que quiere"

    [Header("Texto")]
    public GameObject textoPalero;

    private bool sonidoLejanoReproducido = false;
    private bool sonidoCercanoReproducido = false;

    private void Start()
    {
        if (textoPalero != null)
            textoPalero.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            // Si la barca entra en la zona lejana
            if (other == zonaLejana && !sonidoLejanoReproducido)
            {
                audioLejano.Play();
                sonidoLejanoReproducido = true;
            }

            // Si la barca entra en la zona cercana
            if (other == zonaCercana && !sonidoCercanoReproducido)
            {
                audioCercano.Play();
                sonidoCercanoReproducido = true;

                if (textoPalero != null)
                    textoPalero.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            if (other == zonaCercana && textoPalero != null)
                textoPalero.SetActive(false);
        }
    }
}
