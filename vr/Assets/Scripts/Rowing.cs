using System.Collections;
using UnityEngine;

// Meta / OVR: gesto con "deslizado" (glide) + drag de agua
public class RowingGestureOVRGlide : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody boatRb;
    public Transform leftHand;   // LeftControllerAnchor (Meta rig)
    public Transform rightHand;  // RightControllerAnchor

    [Header("Gesto (detección)")]
    public bool requireGrip = true;
    [Range(0f, 1f)] public float gripThreshold = 0.3f;
    public float strokeMinBackSpeed = 1.3f; // m/s hacia atrás
    public float strokeCooldown = 0.25f;    // s entre impulsos por mano
    public bool requireBladeAngle = false;
    public float bladeMaxAngleDeg = 35f;

    [Header("Glide (deslizamiento)")]
    [Tooltip("Energía que aportas por brazada (se acumula y se consume como inercia).")]
    public float strokeEnergy = 3.2f;       // más alto = más 'empuje' acumulado
    [Tooltip("Constante de tiempo del deslizamiento (s): más alto = se disipa más lento.")]
    public float glideTimeConstant = 0.9f;  // 0.7–1.2 da buen feeling
    [Tooltip("Energía de guiñada por brazada (acumulada y decae).")]
    public float yawEnergy = 0.9f;
    public float yawTimeConstant = 0.6f;

    [Header("Resistencia del agua (horizontal)")]
    [Tooltip("Drag lineal ~v (amortigua velocidades bajas).")]
    public float waterLinearDrag = 0.4f;
    [Tooltip("Drag cuadrático ~v|v| (amortigua velocidades altas).")]
    public float waterQuadraticDrag = 0.25f;

    [Header("Límites")]
    public float maxSpeed = 4.2f; // tope de velocidad horizontal

    [Header("Feedback")]
    public bool haptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;
    public AudioSource splashSource;   // opcional
    public AudioClip splashClip;       // opcional

    // Internos
    private float tL, tR;
    private float glideForward; // "thrust" acumulado que se aplica cada FixedUpdate
    private float glideYaw;     // torque acumulado que se aplica cada FixedUpdate

    void Awake()
    {
        if (!boatRb) boatRb = GetComponent<Rigidbody>();
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
        float dt = Time.fixedDeltaTime;

        // 1) Detectar gesto con velocidades de los controladores (OVR)
        if (leftHand)
        {
            Vector3 lvWorld = leftHand.TransformVector(
                OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch));
            TryStroke(lvWorld, ref tL, now, isLeft: true);
        }
        if (rightHand)
        {
            Vector3 rvWorld = rightHand.TransformVector(
                OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch));
            TryStroke(rvWorld, ref tR, now, isLeft: false);
        }

        // 2) Aplicar "glide" (aceleración y torque que decaen exponencialmente)
        if (glideForward != 0f)
            boatRb.AddForce(transform.forward * glideForward, ForceMode.Acceleration);
        if (glideYaw != 0f)
            boatRb.AddTorque(Vector3.up * glideYaw, ForceMode.Acceleration);

        float kF = Mathf.Exp(-dt / Mathf.Max(0.05f, glideTimeConstant));
        float kY = Mathf.Exp(-dt / Mathf.Max(0.05f, yawTimeConstant));
        glideForward *= kF;
        glideYaw *= kY;

        // 3) Resistencia del agua (solo componente horizontal)
        Vector3 v = boatRb.linearVelocity;
        Vector3 hv = new Vector3(v.x, 0f, v.z);
        if (hv.sqrMagnitude > 0.0001f)
        {
            Vector3 drag = -(waterLinearDrag * hv + waterQuadraticDrag * hv.normalized * hv.sqrMagnitude);
            boatRb.AddForce(new Vector3(drag.x, 0f, drag.z), ForceMode.Acceleration);
        }

        // 4) Tapa de velocidad horizontal (suave)
        float speed = hv.magnitude;
        if (speed > maxSpeed)
        {
            Vector3 clamp = hv.normalized * maxSpeed;
            boatRb.linearVelocity = new Vector3(clamp.x, v.y, clamp.z);
        }
    }

    void TryStroke(Vector3 handVelWorld, ref float lastTime, float now, bool isLeft)
    {
        // Gate por grip (gatillo lateral)
        if (requireGrip)
        {
            float g = isLeft
                ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch)
                : OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if (g < gripThreshold) return;
        }

        // Mano moviéndose "hacia atrás" respecto al forward del bote
        float backSpeed = Vector3.Dot(handVelWorld, -transform.forward);
        if (backSpeed < strokeMinBackSpeed) return;

        // (Opcional) Ángulo de pala más "vertical"
        if (requireBladeAngle)
        {
            // aproximación: usa mano derecha/izquierda del mando si existen; si no, omite
            // (si necesitas exactitud, pasa los transforms de mano y calcula Angle con hand.right)
        }

        // Cooldown
        if ((now - lastTime) < strokeCooldown) return;
        lastTime = now;

        // Escala por velocidad actual para no sobrepotenciar en altas velocidades
        Vector3 hv = new Vector3(boatRb.linearVelocity.x, 0f, boatRb.linearVelocity.z);
        float speed = hv.magnitude;
        float highSpeedAtten = Mathf.InverseLerp(0.5f * maxSpeed, maxSpeed, speed);
        float speedFactor = Mathf.Lerp(1f, 0.6f, highSpeedAtten);

        // 👉 En lugar de un golpe instantáneo, acumulamos "energía de deslizamiento"
        glideForward += strokeEnergy * speedFactor;
        glideYaw += (isLeft ? +1f : -1f) * yawEnergy * speedFactor;

        // Feedback
        if (haptics) StartCoroutine(HapticPulse(isLeft));
        if (splashSource && splashClip) splashSource.PlayOneShot(splashClip);
    }

    IEnumerator HapticPulse(bool left)
    {
        var c = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        OVRInput.SetControllerVibration(0.5f, hapticAmplitude, c);
        yield return new WaitForSeconds(hapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, c);
    }
}