using System.Collections;
using UnityEngine;

// Versión Meta/Oculus (OVR). Solo gesto; el agua es opcional.
public class RowingGestureOVR : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody boatRb;
    public Transform leftHand;   // Asigna LeftControllerAnchor
    public Transform rightHand;  // Asigna RightControllerAnchor

    [Header("Detección por gesto")]
    public bool requireGrip = true;         // exige apretar gatillo lateral
    [Range(0f, 1f)] public float gripThreshold = 0.3f;
    public float strokeMinBackSpeed = 1.3f; // m/s moviendo la mano hacia atrás
    public float strokeCooldown = 0.25f;    // s entre impulsos por mano
    public float maxSpeed = 4.0f;           // tope vel horizontal

    [Header("Ángulo de pala (opcional)")]
    public bool requireBladeAngle = false;  // desactívalo para probar
    public float bladeMaxAngleDeg = 35f;    // hand.right ~ transform.right

    [Header("Impulso")]
    public float boostImpulse = 1.4f;       // fuerza por brazada
    public float yawTorque = 0.6f;          // giro por brazo
    public float impulseScaleAtHighSpeed = 0.6f;

    [Header("Chequeo de agua (opcional)")]
    public bool useWaterCheck = false;      // ← PONLO EN FALSE para ignorar el agua
    public Transform waterProvider;         // opcional si usas agua
    public float waterBand = 0.18f;
    private IWaterHeightProvider water;

    [Header("Feedback")]
    public bool haptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;
    public AudioSource splashSource;   // opcional
    public AudioClip splashClip;       // opcional

    // internos
    private float tL, tR;

    void Awake()
    {
        if (!boatRb) boatRb = GetComponent<Rigidbody>();
        if (useWaterCheck && waterProvider) water = waterProvider.GetComponent<IWaterHeightProvider>();

        if (boatRb)
        {
            boatRb.interpolation = RigidbodyInterpolation.Interpolate;
            boatRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void FixedUpdate()
    {
        if (!boatRb) return;
        float now = Time.time;

        // Velocidades de controladores (OVR → local tracking → a mundo)
        if (leftHand)
        {
            Vector3 lvLocal = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            Vector3 lvWorld = leftHand.TransformVector(lvLocal);
            TryStroke(leftHand, lvWorld, ref tL, now, true);
        }
        if (rightHand)
        {
            Vector3 rvLocal = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            Vector3 rvWorld = rightHand.TransformVector(rvLocal);
            TryStroke(rightHand, rvWorld, ref tR, now, false);
        }

        // Limitar velocidad horizontal
        Vector3 v = boatRb.linearVelocity;
        Vector3 hv = new Vector3(v.x, 0f, v.z);
        if (hv.magnitude > maxSpeed)
        {
            Vector3 clamp = hv.normalized * maxSpeed;
            boatRb.linearVelocity = new Vector3(clamp.x, v.y, clamp.z);
        }
    }

    void TryStroke(Transform hand, Vector3 handVelWorld, ref float lastTime, float now, bool isLeft)
    {
        // Grip (gatillo lateral)
        if (requireGrip)
        {
            float g = isLeft
                ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch)
                : OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if (g < gripThreshold) return;
        }

        // (Opcional) Agua
        if (useWaterCheck)
        {
            bool nearWater = true;
            if (water != null)
            {
                float h = water.GetHeight(hand.position, now);
                float dy = hand.position.y - h;
                nearWater = Mathf.Abs(dy) <= waterBand || dy < 0f;
            }
            else if (waterProvider != null)
            {
                float dy = hand.position.y - waterProvider.position.y;
                nearWater = Mathf.Abs(dy) <= waterBand || dy < 0f;
            }
            if (!nearWater) return;
        }

        // Mano moviéndose "hacia atrás" respecto al forward del bote
        float backSpeed = Vector3.Dot(handVelWorld, -transform.forward);
        if (backSpeed < strokeMinBackSpeed) return;

        // Ángulo de pala (opcional)
        if (requireBladeAngle)
        {
            float ang = Vector3.Angle(hand.right, transform.right);
            if (ang > bladeMaxAngleDeg) return;
        }

        // Cooldown
        if ((now - lastTime) < strokeCooldown) return;
        lastTime = now;

        // Reduce impulso si ya vas rápido
        float horiz = new Vector3(boatRb.linearVelocity.x, 0f, boatRb.linearVelocity.z).magnitude;
        float speedFactor = Mathf.Lerp(1f, impulseScaleAtHighSpeed,
            Mathf.InverseLerp(0.5f * maxSpeed, maxSpeed, horiz));

        // Empujón + torque
        boatRb.AddForce(transform.forward * (boostImpulse * speedFactor), ForceMode.VelocityChange);
        boatRb.AddTorque(Vector3.up * (isLeft ? +yawTorque : -yawTorque) * speedFactor, ForceMode.VelocityChange);

        // Hápticos y splash opcional
        if (haptics) StartCoroutine(HapticPulse(isLeft));
        if (splashSource && splashClip) { splashSource.PlayOneShot(splashClip); }
    }

    IEnumerator HapticPulse(bool left)
    {
        var c = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        OVRInput.SetControllerVibration(0.5f, hapticAmplitude, c);
        yield return new WaitForSeconds(hapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, c);
    }
}
