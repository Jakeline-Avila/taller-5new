using UnityEngine;

public class BarequeroAudio : MonoBehaviour
{
    public AudioSource audioBarequero;
    public GameObject textoBarequero;

    private bool sonidoReproducido = false;

    void Start()
    {
        // Asegura que el texto esté oculto al iniciar
        if (textoBarequero != null)
            textoBarequero.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            // Reproduce el sonido solo una vez
            if (!sonidoReproducido)
            {
                audioBarequero.Play();
                sonidoReproducido = true;
            }

            // Muestra el texto
            if (textoBarequero != null)
                textoBarequero.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            // Oculta el texto al alejarse
            if (textoBarequero != null)
                textoBarequero.SetActive(false);
        }
    }
}
