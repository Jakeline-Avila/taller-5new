using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelReset : MonoBehaviour
{
    [Header("Barca a reiniciar")]
    public Rigidbody boatRigidbody;
    public Transform boatRoot;
    public Transform startPoint;

    [Header("Modo de reinicio")]
    public bool reloadScene = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si el objeto que entr� es la barca
        if (other.attachedRigidbody == boatRigidbody)
        {
            if (reloadScene)
            {
                // Reinicia la escena completa
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                // Solo resetea posici�n y f�sica de la barca
                boatRigidbody.linearVelocity = Vector3.zero;
                boatRigidbody.angularVelocity = Vector3.zero;
                boatRoot.SetPositionAndRotation(startPoint.position, startPoint.rotation);
            }
        }
    }
}
