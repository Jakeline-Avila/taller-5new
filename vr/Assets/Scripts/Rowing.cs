using System.Collections;
using UnityEngine;

// Gesto de canoa con referencia al torso (HMD) – Meta/OVR
public class CanoeStrokeOVRGlide_HeadRef : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody boatRb;
    public Transform leftHand;    // LeftControllerAnchor
    public Transform rightHand;   // RightControllerAnchor
    public Transform head;        // CenterEyeAnchor (HMD)  ← REFERENCIA DEL TORSO

    [Header("Gate")]
    public bool requireGrip = true;
    [Range(0f, 1f)] public float gripThreshold = 0.3f;
    public float cooldown = 0.22f;

    [Header("Detección del gesto (canoa, relativo al torso)")]
    public float minForwardReach = 0.28f;   // mano adelantada respecto al HMD
    public float minPullDistance = 0.40f;   // recorrido hacia el pecho
    public float minLateralOffset = 0.12f;  // mano al costado, no frente al pecho
    public float minBackSpeed = 1.1f;       // velocidad mínima del jalón

    [Header("Glide (deslizamiento)")]
    public float strokeEnergy = 3.4f;
    public float glideTimeConstant = 1.0f;
    public float yawEnergy = 0.8f;
    public float yawTimeConstant = 0.7f;

    [Header("Resistencia del agua (horizontal)")]
    public float waterLinearDrag = 0.4f;
    public float waterQuadraticDrag = 0.25f;

    [Header("Límites")]
    public float maxSpeed = 4.2f;

    [Header("Feedback")]
    public bool haptics = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.08f;

    enum Phase { Idle, Reach, Pull }
    struct HandState { public Phase phase; public Vector3 prevPos; public float backAccum; public float lastStrokeT; }
    HandState L, R;
    float glideFwd, glideYaw;

    void Awake()
    {
        if (!boatRb) boatRb = GetComponent<Rigidbody>();
        if (boatRb)
        {
            boatRb.interpolation = RigidbodyInterpolation.Interpolate;
            boatRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        if (leftHand) L.prevPos = leftHand.position;
        if (rightHand) R.prevPos = rightHand.position;
    }

    void FixedUpdate()
    {
        float now = Time.time, dt = Time.fixedDeltaTime;
        if (!boatRb || !head) return;

        // Base del torso: forward del HMD proyectado al plano horizontal
        Vector3 torsoFwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (torsoFwd.sqrMagnitude < 1e-4f) torsoFwd = transform.forward; // fallback
        Vector3 torsoRight = Vector3.Cross(Vector3.up, torsoFwd).normalized;

        if (leftHand) ProcessHand(leftHand, ref L, true, now, torsoFwd, torsoRight);
        if (rightHand) ProcessHand(rightHand, ref R, false, now, torsoFwd, torsoRight);

        // Glide (aceleración/torque que decaen)
        if (glideFwd != 0f) boatRb.AddForce(transform.forward * glideFwd, ForceMode.Acceleration);
        if (glideYaw != 0f) boatRb.AddTorque(Vector3.up * glideYaw, ForceMode.Acceleration);

        float kF = Mathf.Exp(-dt / Mathf.Max(0.05f, glideTimeConstant));
        float kY = Mathf.Exp(-dt / Mathf.Max(0.05f, yawTimeConstant));
        glideFwd *= kF; glideYaw *= kY;

        // Drag horizontal
        Vector3 v = boatRb.linearVelocity; Vector3 hv = new Vector3(v.x, 0f, v.z);
        if (hv.sqrMagnitude > 1e-5f)
        {
            Vector3 drag = -(waterLinearDrag * hv + waterQuadraticDrag * hv.normalized * hv.sqrMagnitude);
            boatRb.AddForce(new Vector3(drag.x, 0f, drag.z), ForceMode.Acceleration);
        }

        // Tope de velocidad
        float speed = hv.magnitude;
        if (speed > maxSpeed)
        {
            Vector3 clamp = hv.normalized * maxSpeed;
            boatRb.linearVelocity = new Vector3(clamp.x, v.y, clamp.z);
        }
    }

    void ProcessHand(Transform hand, ref HandState S, bool isLeft, float now, Vector3 torsoFwd, Vector3 torsoRight)
    {
        // Velocidad del mando (OVR) en mundo
        Vector3 vLocal = OVRInput.GetLocalControllerVelocity(isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
        Vector3 vWorld = hand.TransformVector(vLocal);

        // Grip (gatillo lateral)
        if (requireGrip)
        {
            float g = isLeft
                ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch)
                : OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            if (g < gripThreshold) { S.phase = Phase.Idle; S.backAccum = 0f; S.prevPos = hand.position; return; }
        }

        // Posición de la mano respecto al torso (proyectada al plano horizontal)
        Vector3 toHand = hand.position - head.position;
        float forward = Vector3.Dot(toHand, torsoFwd);   // +adelante del torso
        float side = Vector3.Dot(toHand, torsoRight); // -izq / +der

        switch (S.phase)
        {
            case Phase.Idle:
                if (Mathf.Abs(side) >= minLateralOffset && forward >= minForwardReach && (now - S.lastStrokeT) >= cooldown)
                    S.phase = Phase.Reach;
                S.backAccum = 0f;
                break;

            case Phase.Reach:
                float backSpeed = Vector3.Dot(vWorld, -torsoFwd); // jalón hacia el pecho
                if (backSpeed >= minBackSpeed) { S.phase = Phase.Pull; S.backAccum = 0f; }
                else if (!(Mathf.Abs(side) >= minLateralOffset && forward >= minForwardReach))
                    S.phase = Phase.Idle;
                break;

            case Phase.Pull:
                // recorrido hacia el torso en el plano horizontal
                Vector3 delta = hand.position - S.prevPos;
                float dz = Vector3.Dot(-delta, torsoFwd); // positivo si va hacia el torso
                if (dz > 0f) S.backAccum += dz;

                if (S.backAccum >= minPullDistance)
                {
                    Stroke(isLeft); S.lastStrokeT = now; S.phase = Phase.Idle; S.backAccum = 0f;
                }
                break;
        }

        S.prevPos = hand.position;
    }

    void Stroke(bool isLeft)
    {
        // Atenuar si ya vamos muy rápido
        Vector3 hv = new Vector3(boatRb.linearVelocity.x, 0f, boatRb.linearVelocity.z);
        float atten = Mathf.InverseLerp(0.5f * maxSpeed, maxSpeed, hv.magnitude);
        float speedFactor = Mathf.Lerp(1f, 0.6f, atten);

        glideFwd += strokeEnergy * speedFactor;
        // Giro por asimetría (izq = proa a derecha)
        glideYaw += (isLeft ? +1f : -1f) * yawEnergy * speedFactor;

        if (haptics) StartCoroutine(HapticPulse(isLeft));
    }

    IEnumerator HapticPulse(bool left)
    {
        var c = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        OVRInput.SetControllerVibration(0.5f, hapticAmplitude, c);
        yield return new WaitForSeconds(hapticDuration);
        OVRInput.SetControllerVibration(0f, 0f, c);
    }
}