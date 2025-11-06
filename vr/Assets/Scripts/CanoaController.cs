using UnityEngine;

public class CanoaController : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Este método se llama cuando entra a un collider marcado como trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Volteo"))
        {
            VoltearCanoa();
        }
    }

    private void VoltearCanoa()
    {
        // Rotación instantánea
        transform.Rotate(0, 0, 180f);

        // O puedes usar física:
        // rb.AddTorque(transform.right * 500f, ForceMode.Impulse);
    }
}
