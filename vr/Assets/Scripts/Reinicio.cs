using UnityEngine;
using UnityEngine.SceneManagement;

public class Reinicio : MonoBehaviour
{
    [Header("Configuración de escena")]
    public string nombreEscenaSiguiente = "EscenaSiguiente"; // Nombre de la escena a cargar
    public float retrasoCambio = 2f; // Segundos antes del cambio (opcional)

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Volteo")) // Cambia el tag si lo necesitas
        {
            Invoke(nameof(CambiarEscena), retrasoCambio);
        }
    }

    private void CambiarEscena()
    {
        SceneManager.LoadScene(nombreEscenaSiguiente);
    }
}
