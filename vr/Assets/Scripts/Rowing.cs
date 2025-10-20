using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR; // h�pticos Quest/OpenXR

public class RowingGestureBoostPlus : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody boatRb;
    public Transform leftHand;        // Anchor del Left Hand Controller
    public Transform rightHand;       // Anchor del Right Hand Controller
    public Transform waterProvider;   // Objeto con SimpleGerstnerWater (opcional)
    private IWaterHeightProvider water; // Debe existir un interfaz IWaterHeightProvider en tu proyecto

    [Header("Gesto tipo Switch (con Grip)")]
    public bool requireGrip = true;
    public InputActionProperty leftGrip;   // mapear a <XRController>{LeftHand}/grip
    public InputActionProperty rightGrip;  // mapear a <XRController>{RightHand}/grip
    [Range(0f, 1f)] public float gripThreshold = 0.3f;

    [Header("Detecci�n de brazada")]
    public float strokeMinBackSpeed = 1.5f; // m/s m�nimo moviendo la mano hacia atr�s
    public float strokeCooldown = 0.28f;    // segundos entre impulsos por mano
    public float waterBand = 0.18f;         // margen vertical para �cerca del agua�
    public float maxSpeed = 4.0f;           // tope de velocidad horizontal del bote

    [Header("�ngulo de pala (opcional)")]
    public bool requireBladeAngle = true;
    [Tooltip("Grados m�ximos entre hand.right y el eje lateral del bote (pala casi vertical).")]
    public float bladeMaxAngleDeg = 35f;

    [Header("Impulso")]
    public float boostImpulse = 1.25f;           // magnitud del empuj�n
    public float yawTorque = 0.6f;               // gui�ada por asimetr�a de brazada
    public float impulseScaleAtHighSpeed = 0.6f; // reduce impulso si ya vas r�pido

    [Header("Feedback")]
    public bool haptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;
    public AudioSource splashSource;        // opcional
    public AudioClip splashClip;            // opcional
    public ParticleSystem splashLeft;       // opcional
    public ParticleSystem splashRight;      // opcional

    // Internos
    private Vector3 lPrev, rPrev;
    private Vector3 lVel, rVel;
    private float tL, tR;

    void Awake()
    {
        if (!boatRb) boatRb = GetComponent<Rigidbody>();
        if (waterProvider) water = waterProvider.GetComponent<IWaterHeightProvider>();
    }

    void OnEnable()
    {
        var lg = leftGrip.action; if (lg != null) lg.Enable();
        var rg = rightGrip.action; if (rg != null) rg.Enable();
    }

    void OnDisable()
    {
        var lg = leftGrip.action; if (lg != null) lg.Disable();
        var rg = rightGrip.action; if (rg != null) rg.Disable();
    }

    void Start()
    {
        if (leftHand) lPrev = leftHand.position;
        if (rightHand) rPrev = rightHand.position;

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

        // Velocidades de manos (mundo)
        if (leftHand)
        {
            lVel = (leftHand.position - lPrev) / Time.fixedDeltaTime;
            lPrev = leftHand.position;
            TryStroke(leftHand, lVel, ref tL, now, true);
        }
        if (rightHand)
        {
            rVel = (rightHand.position - rPrev) / Time.fixedDeltaTime;
            rPrev = rightHand.position;
            TryStroke(rightHand, rVel, ref tR, now, false);
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

    void TryStroke(Transform hand, Vector3 handVel, ref float lastTime, float now, bool isLeft)
    {
        // Gate por Grip
        if (requireGrip)
        {
            var act = isLeft ? leftGrip.action : rightGrip.action;
            if (act == null) return;
            float g = act.ReadValue<float>();
            if (g < gripThreshold) return;
        }

        // �Cerca de la superficie del agua?
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

        // Componente �hacia atr�s� respecto al forward del bote
        float backSpeed = Vector3.Dot(handVel, -transform.forward);
        if (backSpeed < strokeMinBackSpeed) return;

        // �ngulo de pala (mano.right ~ lateral del bote)
        if (requireBladeAngle)
        {
            float ang = Vector3.Angle(hand.right, transform.right);
            if (ang > bladeMaxAngleDeg) return;
        }

        // Cooldown
        if ((now - lastTime) < strokeCooldown) return;
        lastTime = now;

        // Escala por velocidad actual
        float horiz = new Vector3(boatRb.linearVelocity.x, 0f, boatRb.linearVelocity.z).magnitude;
        float speedFactor = Mathf.Lerp(1f, impulseScaleAtHighSpeed,
            Mathf.InverseLerp(0.5f * maxSpeed, maxSpeed, horiz));

        // Impulso + torque
        boatRb.AddForce(transform.forward * (boostImpulse * speedFactor), ForceMode.VelocityChange);
        boatRb.AddTorque(Vector3.up * (isLeft ? +yawTorque : -yawTorque) * speedFactor, ForceMode.VelocityChange);

        // Feedback
        DoHaptics(isLeft);
        DoSplash(isLeft, hand.position);
    }

    void DoHaptics(bool left)
    {
        if (!haptics) return;

        var node = left ? XRNode.LeftHand : XRNode.RightHand;
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid) return;

        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
    }

    void DoSplash(bool left, Vector3 pos)
    {
        // Sonido
        if (splashSource && splashClip)
        {
            splashSource.transform.position = pos;
            splashSource.PlayOneShot(splashClip);
        }
        // Part�culas
        var ps = left ? splashLeft : splashRight;
        if (ps)
        {
            ps.transform.position = pos;
            ps.Play();
        }
    }
}
