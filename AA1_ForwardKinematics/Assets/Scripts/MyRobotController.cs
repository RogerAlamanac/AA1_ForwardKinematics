using UnityEngine;

//[ExecuteAlways]
public class MyRobotController : MonoBehaviour
{
    [Header("Base del brazo (no parent)")]
    public Transform baseTarget; // ancla del camión/vehículo
    [Tooltip("Elevación del punto P0 respecto a la base (en metros)")]
    public float baseHeight = 2.25f;
    public float baseWidth = 0f;


    // --- Joints (esferas visuales) ---
    [Header("Joints (visual)")]
    public GameObject joint1Sphere;   // hombro (P0)
    public GameObject joint2Sphere;   // codo   (P1)
    public GameObject joint3Sphere;   // muñeca (P2)
    public GameObject endEffector;    // efector final (P3)

    // --- Segments (cubos visuales) ---
    [Header("Segments (visual)")]
    public GameObject segment1Cube;   // une P0-P1
    public GameObject segment2Cube;   // une P1-P2
    public GameObject segment3Cube;   // une P2-P3

    // --- Longitudes ---
    [Header("Longitudes")]
    public float segment1Length = 5f;
    public float segment2Length = 4f;
    public float segment3Length = 2f;

    // --- Ángulos objetivo (deg) ---
    [Header("Ángulos objetivo (deg)")]
    public float joint1YawTarget = 0f;    // hombro: yaw (Y local-base)
    public float joint1PitchTarget = 0f;  // hombro: pitch (X local-base)
    public float joint2Target = 0f;       // codo:   pitch (X local)
    public float joint3Target = 0f;       // muñeca: pitch (X local)

    [Header("Efector final (deg)")]
    public float endEffectorYawTarget = 0f;   // yaw local del EE (← → con joint 4)
    public float endEffectorPitchTarget = 0f; // pitch local del EE (opcional)

    // --- Estado suavizado (runtime) ---
    float joint1Yaw, joint1Pitch, joint2, joint3;
    float eeYaw, eePitch;

    // --- Entrada / Suavizado ---
    [Header("Entrada y Suavizado")]
    public float rotationSpeed = 50f;     // deg/s para flechas
    [Tooltip("Constante del filtro exponencial (s). 0 = sin suavizado")]
    public float smoothTime = 0.1f;

    // --- Límites ---
    [Header("Límites (deg)")]
    public bool limitYaw = false;                 // ponlo true si quieres limitar yaw del hombro
    public Vector2 yawLimits = new Vector2(-180f, 180f);
    public Vector2 pitchLimits = new Vector2(-40f, 40f);
    public Vector2 elbowLimits = new Vector2(-120f, 120f);
    public Vector2 wristLimits = new Vector2(-120f, 120f);
    // Nota: efector final sin límites por diseño

    // --- Selección de joint ---
    [Header("Selección de Joint (1=hombro, 2=codo, 3=muñeca, 4=EE yaw)")]
    [SerializeField] int selectedJoint = 1;

    void Update()
    {
        // Entrada (Play)
        HandleSelectionKeys();
        HandleArrowInput();

        // Límites + Suavizado
        ApplyLimits();
        SmoothAngles(Time.deltaTime);

        // FK + Visuales
        ComputeForwardKinematics();
        UpdateSegments();
    }

    // ---------- Entrada ----------
    void HandleSelectionKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedJoint = 1; // Hombro (Yaw+Pitch)
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedJoint = 2; // Codo (Pitch X local)
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedJoint = 3; // Muñeca (Pitch X local)
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedJoint = 4; // Efector (Yaw local)
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

            case 4: // Efector final: ←→ yaw local (sin límites)
                if (Input.GetKey(KeyCode.LeftArrow)) endEffectorYawTarget -= step;
                if (Input.GetKey(KeyCode.RightArrow)) endEffectorYawTarget += step;
                break;
        }
    }

    // ---------- Límites / Suavizado ----------
    void ApplyLimits()
    {
        if (limitYaw)
            joint1YawTarget = LerpLib.Retallar(joint1YawTarget, yawLimits.x, yawLimits.y);

        joint1PitchTarget = LerpLib.Retallar(joint1PitchTarget, pitchLimits.x, pitchLimits.y);
        joint2Target = LerpLib.Retallar(joint2Target, elbowLimits.x, elbowLimits.y);
        joint3Target = LerpLib.Retallar(joint3Target, wristLimits.x, wristLimits.y);
        // efector final sin límites
    }

    void SmoothAngles(float dt)
    {
        if (smoothTime <= 0f)
        {
            SnapRuntimeToTargets();
            return;
        }

        // t = 1 - exp(-dt / tau)
        float t = 1f - (float)System.Math.Exp(-(double)dt / (double)smoothTime);

        joint1Yaw = LerpLib.LerpAngle(joint1Yaw, joint1YawTarget, t);
        joint1Pitch = LerpLib.LerpAngle(joint1Pitch, joint1PitchTarget, t);
        joint2 = LerpLib.LerpAngle(joint2, joint2Target, t);
        joint3 = LerpLib.LerpAngle(joint3, joint3Target, t);

        eeYaw = LerpLib.LerpAngle(eeYaw, endEffectorYawTarget, t);
        eePitch = LerpLib.LerpAngle(eePitch, endEffectorPitchTarget, t);
    }

    void SnapRuntimeToTargets()
    {
        joint1Yaw = joint1YawTarget;
        joint1Pitch = joint1PitchTarget;
        joint2 = joint2Target;
        joint3 = joint3Target;

        eeYaw = endEffectorYawTarget;
        eePitch = endEffectorPitchTarget;
    }

    // ---------- FK ----------
    // FK pura en LOCAL de la base; al final transformamos a MUNDO con Pbase,Rbase EXACTAMENTE una vez.
    void ComputeForwardKinematics()
    {
        // 1) FK en local-base (ejes canónicos)
        Vector3 P0_L = new Vector3(0f, baseHeight, baseWidth); // altura respecto a la base
        Quaternion qY_L = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
        Quaternion qX_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
        Quaternion rot1_L = QuaternionLib.Producte(qY_L, qX_L);

        Vector3 P1_L = P0_L + (rot1_L * Vector3.up) * segment1Length;

        Quaternion qElbow_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
        Quaternion rot2_L = QuaternionLib.Producte(rot1_L, qElbow_L);
        Vector3 P2_L = P1_L + (rot2_L * Vector3.up) * segment2Length;

        Quaternion qWrist_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
        Quaternion rot3_L = QuaternionLib.Producte(rot2_L, qWrist_L);
        Vector3 P3_L = P2_L + (rot3_L * Vector3.up) * segment3Length;

        Quaternion eeYawQ_L = QuaternionLib.DesDeEixAngle(Vector3.up, eeYaw);
        Quaternion eePitchQ_L = QuaternionLib.DesDeEixAngle(Vector3.right, eePitch);
        Quaternion rotEE_L = QuaternionLib.Producte(rot3_L, QuaternionLib.Producte(eeYawQ_L, eePitchQ_L));

        // 2) Pose de la base (coche) y transformación a mundo aplicada UNA sola vez
        Vector3 Pbase = baseTarget ? baseTarget.position : transform.position;
        Quaternion Rbase = baseTarget ? baseTarget.rotation : transform.rotation;

        Vector3 P0_W = Pbase + Rbase * (P0_L);  // ojo: P0_L ya incluye baseHeight sobre local-base
        Vector3 P1_W = Pbase + Rbase * (P1_L);
        Vector3 P2_W = Pbase + Rbase * (P2_L);
        Vector3 P3_W = Pbase + Rbase * (P3_L);

        Quaternion rot1_W = QuaternionLib.Producte(Rbase, rot1_L);
        Quaternion rot2_W = QuaternionLib.Producte(Rbase, rot2_L);
        Quaternion rot3_W = QuaternionLib.Producte(Rbase, rot3_L);
        Quaternion rotEE_W = QuaternionLib.Producte(Rbase, rotEE_L);

        // 3) Pintado (no hay jerarquía; sólo pos/rot absolutas)
        if (joint1Sphere) joint1Sphere.transform.position = P0_W;
        if (joint2Sphere) joint2Sphere.transform.position = P1_W;
        if (joint3Sphere) joint3Sphere.transform.position = P2_W;
        if (endEffector)
        {
            endEffector.transform.position = P3_W;
            endEffector.transform.rotation = rotEE_W;
        }
    }

    // ---------- Visual de segmentos ----------
    void UpdateSegments()
    {
        if (segment1Cube && joint1Sphere && joint2Sphere)
            PositionSegment(segment1Cube, joint1Sphere, joint2Sphere);

        if (segment2Cube && joint2Sphere && joint3Sphere)
            PositionSegment(segment2Cube, joint2Sphere, joint3Sphere);

        if (segment3Cube && joint3Sphere && endEffector)
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
        {
            // Mantén el "up" del vehículo para evitar roll raro en los cubos
            Vector3 upRef = baseTarget ? (baseTarget.rotation * Vector3.up) : Vector3.up;
            segment.transform.rotation = QuaternionLib.LookRotation(dir, upRef);
        }

        Vector3 s = segment.transform.localScale;
        s.z = Mathf.Sqrt(len2);
        segment.transform.localScale = s;
    }
}