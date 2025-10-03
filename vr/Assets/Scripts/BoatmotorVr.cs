using UnityEngine;
using UnityEngine.InputSystem;

public class BoatMotorVR : MonoBehaviour
{
    [Header("Input (XR Input System)")]
    public InputActionProperty moveStick;  // Vector2: left controller primary2DAxis
    public InputActionProperty turnStick;  // Vector2 o X del right controller (opcional)

    [Header("Fuerzas")]
    public float thrustAccel = 6f;     // empuje adelante/atr�s (m/s^2)
    public float strafeAccel = 0f;     // opcional, mover lateral
    public float turnAccel = 2.5f;     // torque yaw
    public float maxSpeed = 3.5f;      // l�mite de velocidad

    [Header("Estabilidad extra")]
    public float angularDamping = 0.8f;   // amortigua rotaci�n residual

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (moveStick.action != null) moveStick.action.Enable();
        if (turnStick.action != null) turnStick.action.Enable();
    }
    void OnDisable()
    {
        if (moveStick.action != null) moveStick.action.Disable();
        if (turnStick.action != null) turnStick.action.Disable();
    }

    void FixedUpdate()
    {
        Vector2 m = moveStick.action != null ? moveStick.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 t = turnStick.action != null ? turnStick.action.ReadValue<Vector2>() : Vector2.zero;

        // Adelante/atr�s con el eje Y del stick izquierdo
        if (maxSpeed <= 0f || rb.linearVelocity.magnitude < maxSpeed)
        {
            Vector3 fwd = transform.forward * (m.y * thrustAccel);
            Vector3 str = transform.right * (m.x * strafeAccel);
            rb.AddForce(fwd + str, ForceMode.Acceleration);
        }

        // Girar con X del stick derecho (o usa X del izquierdo si no tienes turn separado)
        float turnInput = (turnStick.action != null ? t.x : m.x);
        rb.AddTorque(Vector3.up * turnInput * turnAccel, ForceMode.Acceleration);

        // Amortiguaci�n angular suave
        rb.AddTorque(-rb.angularVelocity * angularDamping, ForceMode.Acceleration);
    }
}