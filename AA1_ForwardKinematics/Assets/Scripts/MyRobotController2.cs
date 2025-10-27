using UnityEngine;

//[ExecuteAlways]
public class MyRobotController2 : MonoBehaviour
{
    // --- Joints (esferas visuales) ---
    public GameObject joint1Sphere;   // base/hombro
    public GameObject joint2Sphere;   // codo
    public GameObject joint3Sphere;   // muñeca
    public GameObject endEffector;    // efector final (punta)

    // --- Segments (cubs visuals) ---
    public GameObject segment1Cube;
    public GameObject segment2Cube;
    public GameObject segment3Cube;

    // --- Longitudes (|D_i|) ---
    [Header("Longitudes")]
    public float segment1Length = 2f;
    public float segment2Length = 1.5f;
    public float segment3Length = 1f;

    // --- Estado articular (objetivo) en grados ---
    // Convention: igual que tu script #2 (W baja pitch; S sube pitch)
    [Header("Ángulos objetivo (deg)")]
    public float joint1YawTarget = 0f;    // rotación Y global
    public float joint1PitchTarget = 0f;  // rotación X global
    public float joint2Target = 0f;       // codo (X local)
    public float joint3Target = 0f;       // muñeca (X local)

    // --- Estado articular (suavizado) ---
    float joint1Yaw, joint1Pitch, joint2, joint3;

    [Header("Entrada y Suavizado")]
    public float rotationSpeed = 50f;     // deg/s
    [Tooltip("Tiempo aproximado para alcanzar el objetivo (s). 0 = instantáneo")]
    public float smoothTime = 0.1f;       // segundos (suavizado exponencial)

    [Header("Límites articulares (deg)")]
    public Vector2 pitchLimits = new Vector2(-40f, 40f);
    public Vector2 elbowLimits = new Vector2(-120f, 120f);
    public Vector2 wristLimits = new Vector2(-120f, 120f);

    void Start()
    {
        // inicializa estado suavizado con los objetivos
        joint1Yaw = joint1YawTarget;
        joint1Pitch = joint1PitchTarget;
        joint2 = joint2Target;
        joint3 = joint3Target;
    }

    void Update()
    {
        HandleInput();
        ApplyLimits();
        SmoothAngles(Time.deltaTime);

        ComputeForwardKinematics(); // posiciona esferas y efector
        UpdateSegments();           // orienta cubos entre esferas
    }

    void HandleInput()
    {
        float dt = Time.deltaTime;

        // --- Yaw (Y global) ---
        if (Input.GetKey(KeyCode.A)) joint1YawTarget -= rotationSpeed * dt;
        if (Input.GetKey(KeyCode.D)) joint1YawTarget += rotationSpeed * dt;

        // --- Pitch (X global) ---
        // Mantiene la convención de tu Script #2 (W disminuye, S aumenta)
        if (Input.GetKey(KeyCode.W)) joint1PitchTarget -= rotationSpeed * dt;
        if (Input.GetKey(KeyCode.S)) joint1PitchTarget += rotationSpeed * dt;

        // --- Codo (X local del 2º segmento) ---
        if (Input.GetKey(KeyCode.Q)) joint2Target += rotationSpeed * dt;
        if (Input.GetKey(KeyCode.E)) joint2Target -= rotationSpeed * dt;

        // --- Muñeca (X local del 3º segmento) ---
        if (Input.GetKey(KeyCode.R)) joint3Target += rotationSpeed * dt;
        if (Input.GetKey(KeyCode.F)) joint3Target -= rotationSpeed * dt;
    }

    void ApplyLimits()
    {
        // Usa tu clamp propio
        joint1PitchTarget = LerpLib.Retallar(joint1PitchTarget, pitchLimits.x, pitchLimits.y);
        joint2Target = LerpLib.Retallar(joint2Target, elbowLimits.x, elbowLimits.y);
        joint3Target = LerpLib.Retallar(joint3Target, wristLimits.x, wristLimits.y);
    }

    void SmoothAngles(float dt)
    {
        if (smoothTime <= 0f)
        {
            joint1Yaw = joint1YawTarget;
            joint1Pitch = joint1PitchTarget;
            joint2 = joint2Target;
            joint3 = joint3Target;
            return;
        }

        // Suavizado exponencial independiente por articulación:
        // t = 1 - exp(-dt / tau)
        float t = 1f - Mathf.Exp(-dt / smoothTime);

        joint1Yaw = LerpLib.LerpAngle(joint1Yaw, joint1YawTarget, t);
        joint1Pitch = LerpLib.LerpAngle(joint1Pitch, joint1PitchTarget, t);
        joint2 = LerpLib.LerpAngle(joint2, joint2Target, t);
        joint3 = LerpLib.LerpAngle(joint3, joint3Target, t);
    }

    void ComputeForwardKinematics()
    {
        // --- Base fija en el origen del mapa ---
        // Si prefieres otra base, cambia este vector:
        Vector3 P0 = new Vector3(0f, 0f, 0f);

        // --- Rotación acumulada del 1er segmento ---
        // Yaw (Y global) + Pitch (X global). Mantenemos tu convención de producto:
        var qZ = QuaternionLib.DesDeEixAngle(Vector3.forward, 0f);
        var qX = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
        var qY = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
        Quaternion rot1 = QuaternionLib.Producte(qY, QuaternionLib.Producte(qX, qZ));

        // IMPORTANTE: la cadena ahora se extiende a lo largo de +Y (Vector3.up)
        Vector3 P1 = P0 + (rot1 * Vector3.up) * segment1Length;

        // Rotación del 2º segmento = rot1 * rot_local_codo (alrededor de X local)
        Quaternion colzeLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
        Quaternion rot2 = QuaternionLib.Producte(rot1, colzeLocal);

        Vector3 P2 = P1 + (rot2 * Vector3.up) * segment2Length;

        // Rotación del 3º segmento = rot2 * rot_local_muñeca (alrededor de X local)
        Quaternion canellLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
        Quaternion rot3 = QuaternionLib.Producte(rot2, canellLocal);

        Vector3 P3 = P2 + (rot3 * Vector3.up) * segment3Length;

        // Colocar esferas/joints
        joint1Sphere.transform.position = P0;
        joint2Sphere.transform.position = P1;
        joint3Sphere.transform.position = P2;
        endEffector.transform.position = P3;

        // Orientar el end effector con la rotación acumulada
        endEffector.transform.rotation = rot3;
    }

    void UpdateSegments()
    {
        PositionSegment(segment1Cube, joint1Sphere, joint2Sphere);
        PositionSegment(segment2Cube, joint2Sphere, joint3Sphere);
        PositionSegment(segment3Cube, joint3Sphere, endEffector);
    }

    void PositionSegment(GameObject segment, GameObject startJoint, GameObject endJoint)
    {
        Vector3 a = startJoint.transform.position;
        Vector3 b = endJoint.transform.position;
        Vector3 dir = b - a;

        // Centro y longitud
        segment.transform.position = (a + b) * 0.5f;
        float len = dir.magnitude;

        if (dir.sqrMagnitude > 1e-12f)
        {
            // Up estable. Puedes cambiar a Vector3.forward si alguna vez dir up y quieres evitar colinealidad.
            segment.transform.rotation = QuaternionLib.LookRotation(dir, Vector3.up);
        }

        // Escala en Z para que el cubo abarque de joint a joint
        Vector3 s = segment.transform.localScale;
        s.z = len;
        segment.transform.localScale = s;
    }
}