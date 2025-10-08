using UnityEngine;

// Si ya tienes esta interfaz en otro archivo, borra esta copia.


[RequireComponent(typeof(Rigidbody))]
public class BuoyancyPD : MonoBehaviour
{
    [Header("Agua")]
    [Tooltip("Objeto que tiene un componente que implementa IWaterHeightProvider")]
    public Transform waterProviderObject;
    private IWaterHeightProvider water;

    [Header("Altura / Flotación (PD)")]
    [Tooltip("Ajuste fino de la línea de flotación")]
    public float surfaceOffset = 0f;
    [Tooltip("Kp vertical (fuerza para seguir la altura)")]
    public float followStrength = 500f;     // 450–650
    [Tooltip("Kd vertical (amortiguación de la altura)")]
    public float followDamping = 100f;      // 80–120
    [Tooltip("Tope absoluto de fuerza vertical (anti-picos)")]
    public float maxUpForce = 5000f;

    [Header("Orientación / Estabilidad (PD)")]
    [Tooltip("Kp orientación (enderezar)")]
    public float alignStrength = 14f;       // 12–16
    [Tooltip("Kd orientación")]
    public float alignDamping = 6f;        // 4–8
    [Tooltip("Cuánto seguir la pendiente del agua (0..1)")]
    [Range(0, 1)] public float slopeFollow = 0.35f;
    [Tooltip("Separación para muestrear la normal")]
    public float normalSampleDist = 0.6f;
    [Tooltip("Máximo rolido/cabeceo permitido (grados)")]
    public float maxTiltDeg = 25f;
    [Tooltip("Tope de velocidad angular global (grados/seg)")]
    public float maxAngVelDeg = 120f;

    [Header("Arrastre extra")]
    [Tooltip("Freno horizontal para quitar 'patinaje'")]
    public float lateralDrag = 2.0f;        // 1.2–2.5

    [Header("Centro de masa")]
    [Tooltip("Offset vertical inicial para bajar el centro de masa")]
    public float centerOfMassOffsetY = -0.22f;

    [Header("Filtros / Anti-jitter")]
    [Tooltip("Suavizado de altura (0..1)")]
    public float heightSmoothing = 0.15f;
    [Tooltip("Zona muerta de altura (metros)")]
    public float heightDeadzone = 0.01f;    // 1 cm
    [Tooltip("Suavizado de la normal (0..1)")]
    public float normalSmoothing = 0.12f;

    // --- internos ---
    Rigidbody rb;
    Vector3 smoothUp = Vector3.up;
    float hFiltered = float.NaN;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (waterProviderObject != null)
            water = waterProviderObject.GetComponent<IWaterHeightProvider>();

        if (water == null)
            Debug.LogError("[BuoyancyPD] No encontré IWaterHeightProvider en Water Provider Object.");
    }

    void Start()
    {
        // Coloca el centro de masa un poco más bajo para mayor estabilidad
        rb.centerOfMass += new Vector3(0f, centerOfMassOffsetY, 0f);
    }

    void FixedUpdate()
    {
        if (water == null) return;

        float t = Time.time;
        Vector3 c = transform.position;

        // ---------- ALTURA ----------
        float h = water.GetHeight(c, t) + surfaceOffset;

        // Filtro de altura para evitar ruido de la superficie
        if (float.IsNaN(hFiltered)) hFiltered = h;
        hFiltered = Mathf.Lerp(hFiltered, h, heightSmoothing);

        // Error de altura con zona muerta
        float yErrRaw = hFiltered - c.y;
        float yError = Mathf.Abs(yErrRaw) < heightDeadzone ? 0f : yErrRaw;

        // PD vertical + compensación de peso
        float vy = rb.linearVelocity.y;
        float upAccel = followStrength * yError - followDamping * vy + Physics.gravity.magnitude;

        // Tope de seguridad a la aceleración total
        upAccel = Mathf.Clamp(upAccel, -40f, 40f);
        Vector3 upForce = Vector3.up * Mathf.Clamp(upAccel * rb.mass, -maxUpForce, maxUpForce);

        // Drag lateral para quitar "patinaje"
        Vector3 v = rb.linearVelocity;
        Vector3 horizV = new Vector3(v.x, 0f, v.z);
        Vector3 horizDragF = -horizV * lateralDrag * rb.mass;

        rb.AddForce(upForce + horizDragF, ForceMode.Force);

        // ---------- ORIENTACIÓN ----------
        // Muestra alturas en +X y +Z para estimar normal
        float hx = water.GetHeight(c + Vector3.right * normalSampleDist, t);
        float hz = water.GetHeight(c + Vector3.forward * normalSampleDist, t);

        Vector3 dx = new Vector3(normalSampleDist, hx - h, 0f);
        Vector3 dz = new Vector3(0f, hz - h, normalSampleDist);

        Vector3 waterNormal = Vector3.Cross(dz, dx).normalized;
        smoothUp = Vector3.Slerp(smoothUp, waterNormal, normalSmoothing);

        Vector3 desiredUp = Vector3.Slerp(Vector3.up, smoothUp, Mathf.Clamp01(slopeFollow));

        // Proyecta el forward para no introducir yaw (solo roll/pitch)
        Vector3 desiredFwd = Vector3.ProjectOnPlane(transform.forward, desiredUp).normalized;
        if (desiredFwd.sqrMagnitude < 1e-4f)
            desiredFwd = Vector3.ProjectOnPlane(transform.right, desiredUp).normalized;

        Quaternion desiredRot = Quaternion.LookRotation(desiredFwd, desiredUp);

        // Error rotacional → eje/ángulo
        Quaternion qErr = desiredRot * Quaternion.Inverse(transform.rotation);
        qErr.ToAngleAxis(out float angDeg, out Vector3 axis);
        if (angDeg > 180f) { angDeg = 360f - angDeg; axis = -axis; }

        // Si el tilt supera el máximo, mete un enderezado extra (roll/pitch)
        float tiltDeg = Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.up, desiredUp), -1f, 1f)) * Mathf.Rad2Deg;
        if (tiltDeg > maxTiltDeg)
        {
            float extra = (tiltDeg - maxTiltDeg) / maxTiltDeg; // 0..1
            Vector3 noYawAxis = Vector3.ProjectOnPlane(axis, Vector3.up).normalized;
            rb.AddTorque(noYawAxis * (alignStrength * 50f * extra), ForceMode.Force);
        }

        // PD de orientación principal
        Vector3 angVel = rb.angularVelocity;
        Vector3 torque = axis * (angDeg * Mathf.Deg2Rad * alignStrength) - angVel * alignDamping;
        rb.AddTorque(torque * rb.mass, ForceMode.Force);

        // Límite de velocidad angular global (anti-spin)
        float maxAng = maxAngVelDeg * Mathf.Deg2Rad;
        if (rb.angularVelocity.magnitude > maxAng)
            rb.angularVelocity = rb.angularVelocity.normalized * maxAng;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Gizmo de la normal deseada para depurar
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + smoothUp * 1.5f);
    }
#endif
}
