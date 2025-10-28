using UnityEngine;

[ExecuteAlways]
public class MyRobotController : MonoBehaviour
{
    [Header("Base del brazo (no parent)")]
    public Transform baseTarget; // Arrastra aquí tu Cube/anchor

    // --- Joints (esferas visuales) ---
    public GameObject joint1Sphere;   // hombro
    public GameObject joint2Sphere;   // codo
    public GameObject joint3Sphere;   // muñeca
    public GameObject endEffector;    // efector final

    // --- Segments (cubos visuales) ---
    public GameObject segment1Cube;
    public GameObject segment2Cube;
    public GameObject segment3Cube;

    // --- Longitudes ---
    [Header("Longitudes")]
    public float segment1Length = 5f;
    public float segment2Length = 4f;
    public float segment3Length = 2f;

    // --- Ángulos objetivo (deg) ---
    [Header("Ángulos objetivo (deg)")]
    public float joint1YawTarget = 0f;    // Y global (hombro)
    public float joint1PitchTarget = 0f;  // X global (hombro)
    public float joint2Target = 0f;       // X local (codo)
    public float joint3Target = 0f;       // X local (muñeca)

    // --- Estado suavizado (runtime) ---
    float joint1Yaw, joint1Pitch, joint2, joint3;

    // --- Control / Suavizado ---
    [Header("Entrada y Suavizado")]
    public float rotationSpeed = 50f;     // deg/s para flechas
    [Tooltip("Tiempo característico del filtro exponencial (s). 0 = sin suavizado")]
    public float smoothTime = 0.1f;

    // --- Límites (puedes ajustar en el Inspector) ---
    [Header("Límites (deg)")]
    public Vector2 yawLimits = new Vector2(-180f, 180f);
    public Vector2 pitchLimits = new Vector2(-40f, 40f);
    public Vector2 elbowLimits = new Vector2(-120f, 120f);
    public Vector2 wristLimits = new Vector2(-120f, 120f);

    // --- Selección de joint con 1-3 ---
    [Header("Selección de Joint")]
    [SerializeField] int selectedJoint = 1; // 1=hombro, 2=codo, 3=muñeca

    void OnEnable()
    {
        SnapRuntimeToTargets();
        RecomputeAndDraw_EditorSafe();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            SnapRuntimeToTargets();
            RecomputeAndDraw_EditorSafe();
        }
    }

    void OnValidate()
    {
        ApplyLimits();               // respeta límites al tocar sliders
        if (!Application.isPlaying)  // en edición, muestra directamente objetivos
            SnapRuntimeToTargets();
        RecomputeAndDraw_EditorSafe();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            // En edición: seguir a baseTarget y actualizar pose
            RecomputeAndDraw_EditorSafe();
            return;
        }

        HandleSelectionKeys();  // 1/2/3
        HandleArrowInput();     // flechas según joint seleccionado

        ApplyLimits();
        SmoothAngles(Time.deltaTime);

        ComputeForwardKinematics();
        UpdateSegments();
    }

    // ---------- Nuevo esquema de entrada ----------
    void HandleSelectionKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedJoint = 1; // hombro (Y+X)
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedJoint = 2; // codo (X)
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedJoint = 3; // muñeca (X);
    }

    void HandleArrowInput()
    {
        float dt = Time.deltaTime;
        float step = rotationSpeed * dt;

        switch (selectedJoint)
        {
            case 1: // Hombro: ←→ yaw, ↑↓ pitch
                if (Input.GetKey(KeyCode.LeftArrow)) joint1YawTarget -= step;
                if (Input.GetKey(KeyCode.RightArrow)) joint1YawTarget += step;
                if (Input.GetKey(KeyCode.UpArrow)) joint1PitchTarget += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint1PitchTarget -= step;
                break;

            case 2: // Codo: ↑↓ flexión X
                if (Input.GetKey(KeyCode.UpArrow)) joint2Target += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint2Target -= step;
                break;

            case 3: // Muñeca: ↑↓ flexión X
                if (Input.GetKey(KeyCode.UpArrow)) joint3Target += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint3Target -= step;
                break;
        }
    }

    // ---------- Límites / Suavizado ----------
    void ApplyLimits()
    {
        joint1YawTarget   = LerpLib.Retallar(joint1YawTarget,   yawLimits.x,   yawLimits.y);
        joint1PitchTarget = LerpLib.Retallar(joint1PitchTarget, pitchLimits.x, pitchLimits.y);
        joint2Target = LerpLib.Retallar(joint2Target, elbowLimits.x, elbowLimits.y);
        joint3Target = LerpLib.Retallar(joint3Target, wristLimits.x, wristLimits.y);
    }

    void SmoothAngles(float dt)
    {
        if (smoothTime <= 0f)
        {
            SnapRuntimeToTargets();
            return;
        }

        float t = 1f - Mathf.Exp(-dt / smoothTime);
        joint1Yaw = LerpLib.LerpAngle(joint1Yaw, joint1YawTarget, t);
        joint1Pitch = LerpLib.LerpAngle(joint1Pitch, joint1PitchTarget, t);
        joint2 = LerpLib.LerpAngle(joint2, joint2Target, t);
        joint3 = LerpLib.LerpAngle(joint3, joint3Target, t);
    }

    void SnapRuntimeToTargets()
    {
        joint1Yaw = joint1YawTarget;
        joint1Pitch = joint1PitchTarget;
        joint2 = joint2Target;
        joint3 = joint3Target;
    }

    // ---------- Editor-safe update ----------
    void RecomputeAndDraw_EditorSafe()
    {
        if (!Application.isPlaying)
        {
            // En edición, dibuja la pose objetivo directamente
            joint1Yaw = joint1YawTarget;
            joint1Pitch = joint1PitchTarget;
            joint2 = joint2Target;
            joint3 = joint3Target;
        }

        if (!joint1Sphere || !joint2Sphere || !joint3Sphere || !endEffector ||
            !segment1Cube || !segment2Cube || !segment3Cube)
            return;

        ComputeForwardKinematics();
        UpdateSegments();
    }

    // ---------- FK + segmentos ----------
    void ComputeForwardKinematics()
    {
        Vector3 P0 = baseTarget ? baseTarget.position : transform.position;

        var qZ = QuaternionLib.DesDeEixAngle(Vector3.forward, 0f);
        var qX = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
        var qY = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
        Quaternion rot1 = QuaternionLib.Producte(qY, QuaternionLib.Producte(qX, qZ));

        Vector3 P1 = P0 + (rot1 * Vector3.up) * segment1Length;

        Quaternion colzeLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
        Quaternion rot2 = QuaternionLib.Producte(rot1, colzeLocal);
        Vector3 P2 = P1 + (rot2 * Vector3.up) * segment2Length;

        Quaternion canellLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
        Quaternion rot3 = QuaternionLib.Producte(rot2, canellLocal);
        Vector3 P3 = P2 + (rot3 * Vector3.up) * segment3Length;

        joint1Sphere.transform.position = P0;
        joint2Sphere.transform.position = P1;
        joint3Sphere.transform.position = P2;
        endEffector.transform.position = P3;

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

        segment.transform.position = (a + b) * 0.5f;
        float len2 = dir.sqrMagnitude;

        if (len2 > 1e-12f)
            segment.transform.rotation = QuaternionLib.LookRotation(dir, Vector3.up);

        Vector3 s = segment.transform.localScale;
        s.z = Mathf.Sqrt(len2);
        segment.transform.localScale = s;
    }
}