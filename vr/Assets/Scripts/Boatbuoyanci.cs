using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatBuoyancy : MonoBehaviour
{
    [Header("Agua")]
    public Transform waterProviderObject; // arrastra el objeto con SimpleGerstnerWater
    IWaterHeightProvider water;

    [Header("Puntos de flotación (4–8)")]
    public Transform[] floatPoints;

    [Header("Fuerzas")]
    public float buoyancyStrength = 12f; // fuerza por metro
    public float maxDepth = 0.6f;        // inmersión para fuerza máxima
    public float damping = 3.5f;         // amortiguación de la velocidad local
    public float waterDrag = 0.25f;      // rozamiento horizontal extra

    [Header("Estabilidad")]
    public float uprightTorque = 2.0f;   // torque para enderezar
    public float slopeFollow = 0.7f;     // cuánto seguir la pendiente del agua (0..1)
    public float slopeSampleDist = 0.7f; // distancia de muestreo para calcular normal del agua

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (waterProviderObject) water = waterProviderObject.GetComponent<IWaterHeightProvider>();
    }

    void FixedUpdate()
    {
        if (water == null || floatPoints == null || floatPoints.Length == 0) return;

        float t = Time.time;
        float submergedSum = 0f;

        foreach (var p in floatPoints)
        {
            Vector3 wp = p.position;
            float waterH = water.GetHeight(wp, t);
            float depth = waterH - wp.y; // >0 = bajo el agua

            if (depth > 0f)
            {
                float ratio = Mathf.Clamp01(depth / maxDepth);

                // Empuje
                Vector3 uplift = Vector3.up * (buoyancyStrength * ratio);

                // Amortiguación local
                Vector3 vel = rb.GetPointVelocity(wp);
                Vector3 damp = -vel * damping;

                // Arrastre horizontal
                Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);
                Vector3 drag = -horizVel * waterDrag;

                rb.AddForceAtPosition(uplift + damp + drag, wp, ForceMode.Acceleration);
                submergedSum += ratio;
            }
        }

        // --- Alinearse con la pendiente de la "superficie" (estimada) ---
        if (slopeFollow > 0f)
        {
            // Muestras en +X y +Z para calcular gradiente (normal aproximada)
            Vector3 c = transform.position;
            float hC = water.GetHeight(c, t);
            float hX = water.GetHeight(c + Vector3.right * slopeSampleDist, t);
            float hZ = water.GetHeight(c + Vector3.forward * slopeSampleDist, t);

            Vector3 dx = new Vector3(slopeSampleDist, hX - hC, 0f);
            Vector3 dz = new Vector3(0f, hZ - hC, slopeSampleDist);

            Vector3 normal = Vector3.Cross(dz, dx).normalized; // normal “agua”
            Vector3 desiredUp = Vector3.Slerp(Vector3.up, normal, Mathf.Clamp01(slopeFollow));

            // Torque para alinear el "up" del barco hacia desiredUp
            Vector3 torque = Vector3.Cross(transform.up, desiredUp) * uprightTorque;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
}
