using UnityEngine;

public class BarequeroAudio : MonoBehaviour
{
    [Header("Zonas de detección")]
    public Collider zonaLejana;
    public Collider zonaCercana;

    [Header("Audios")]
    public AudioSource audioLejano;   // "¡Primo, primo, acércate!"
    public AudioSource audioCercano;  // "Ahora sí, lo que quiere"

    [Header("Texto")]
    public GameObject textoBarequero;

    private bool sonidoLejanoReproducido = false;
    private bool sonidoCercanoReproducido = false;

    private void Start()
    {
        if (textoBarequero != null)
            textoBarequero.SetActive(false);
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

                if (textoBarequero != null)
                    textoBarequero.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            if (other == zonaCercana && textoBarequero != null)
                textoBarequero.SetActive(false);
        }
    }
}
