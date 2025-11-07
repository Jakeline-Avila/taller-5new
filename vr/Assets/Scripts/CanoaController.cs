using UnityEngine;
using UnityEngine.SceneManagement; //  Necesario para manejar escenas

public class CanoaController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Configuración de escena")]
    public string nombreEscenaSiguiente = "EscenaSiguiente"; //  Nombre de la escena a cargar
    public float retrasoCambio = 2f; // opcional: segundos antes del cambio

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Volteo"))
        {
            VoltearCanoa();
            CambiarEscenaConRetraso();
        }
    }

    private void VoltearCanoa()
    {
        // Rotación instantánea
        transform.Rotate(0, 0, 180f);

        // O puedes usar física:
        // rb.AddTorque(transform.right * 500f, ForceMode.Impulse);
    }

    private void CambiarEscenaConRetraso()
    {
        // Llama al método de cambio después de un retraso opcional
        Invoke(nameof(CambiarEscena), retrasoCambio);
    }

    private void CambiarEscena()
    {
        SceneManager.LoadScene(nombreEscenaSiguiente);
    }
}
