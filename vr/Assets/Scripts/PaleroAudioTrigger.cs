using UnityEngine;

public class PaleroAudio : MonoBehaviour
{
    public AudioSource audioPalero;
    public GameObject textoPalero;

    private bool sonidoReproducido = false;

    void Start()
    {
        // Asegura que el texto esté oculto al iniciar
        if (textoPalero != null)
            textoPalero.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            if (!sonidoReproducido)
            {
                audioPalero.Play();
                sonidoReproducido = true;
            }

            textoPalero.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Barca"))
        {
            textoPalero.SetActive(false);
        }
    }
}
