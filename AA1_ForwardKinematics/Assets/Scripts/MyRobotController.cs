using UnityEngine;

//[ExecuteAlways]
public class MyRobotController : MonoBehaviour
{
    [Header("Base del brazo (no parent)")]
    public Transform baseTarget; // arrastra aquí tu Cube/anchor del camión

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
    public float joint1YawTarget = 0f;    // Y global (hombro)
    public float joint1PitchTarget = 0f;  // X global (hombro)
    public float joint2Target = 0f;       // X local (codo)
    public float joint3Target = 0f;       // X local (muñeca)

    // NUEVO: Yaw/Pitch del efector final (sin límites)
    public float endEffectorYawTarget = 0f;   // giro lateral con ← →
    public float endEffectorPitchTarget = 0f; // opcional (no mapeado a teclas por defecto)

    // --- Estado suavizado (runtime) ---
    float joint1Yaw, joint1Pitch, joint2, joint3;
    float eeYaw, eePitch; // estado suavizado del efector final

    // --- Entrada / Suavizado ---
    [Header("Entrada y Suavizado")]
    public float rotationSpeed = 50f;     // deg/s para flechas
    [Tooltip("Constante del filtro exponencial (s). 0 = sin suavizado")]
    public float smoothTime = 0.1f;

    // --- Límites ---
    [Header("Límites (deg)")]
    public bool limitYaw = false;                 // ponlo true si quieres limitar yaw
    public Vector2 yawLimits = new Vector2(-180f, 180f);
    public Vector2 pitchLimits = new Vector2(-40f, 40f);
    public Vector2 elbowLimits = new Vector2(-120f, 120f);
    public Vector2 wristLimits = new Vector2(-120f, 120f);
    // NOTA: el efector final NO tiene límites

    // --- Selección de joint ---
    [Header("Selección de Joint (1=hombro, 2=codo, 3=muñeca/efector)")]
    [SerializeField] int selectedJoint = 1;

    void Update()
    {
        // Entrada (Play)
        HandleSelectionKeys();
        HandleArrowInput();

        // Límites + Suavizado
        ApplyLimits();                  // no limita el efector final
        SmoothAngles(Time.deltaTime);   // incluye el efector final

        // FK + Visuales
        ComputeForwardKinematics();
        UpdateSegments();
    }

    // ---------- Entrada ----------
    void HandleSelectionKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedJoint = 1; // Hombro (Yaw+Pitch)
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedJoint = 2; // Codo (Pitch X local)
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedJoint = 3; // Muñeca (Pitch X) + EE Yaw
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedJoint = 4; // EE Yaw
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

            case 3: // Muñeca + Efector: ↑↓ flexión X de la muñeca, ←→ YAW del efector final
                if (Input.GetKey(KeyCode.UpArrow)) joint3Target += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint3Target -= step;
                break;

            case 4:
                // REQUERIDO: rotar el efector final a izquierda/derecha sin límites
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

	// ---------- FK ----------
	void ComputeForwardKinematics()
	{
		// P0 = base (sigue al target si está asignado; si no, usa este objeto)
		Vector3 P0base = baseTarget ? baseTarget.position : transform.position;
		Vector3 altura = new Vector3(0, 2.25f, 0);
		Vector3 P0 = P0base + altura;
		// Rotación acumulada del 1er eslabón: R_y * R_x (R_z=0)
		Quaternion qZ = QuaternionLib.DesDeEixAngle(Vector3.forward, 0f);
		Quaternion qX = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
		Quaternion qY = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
		Quaternion rot1 = QuaternionLib.Producte(qY, QuaternionLib.Producte(qX, qZ));

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
    void ComputeForwardKinematics()
    {
        // ===== 1) FK en LOCAL DE LA BASE (sin usar rotación del coche aún) =====
        Vector3 P0_L = Vector3.zero;

        // Hombro: yaw (Y) -> pitch (X) en local-base
        Quaternion qY_L = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
        Quaternion qX_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
        Quaternion rot1_L = QuaternionLib.Producte(qY_L, qX_L);

        Vector3 P1_L = P0_L + (rot1_L * Vector3.up) * segment1Length;

        // Codo: pitch sobre X local del eslabón 1
        Quaternion qElbow_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
        Quaternion rot2_L = QuaternionLib.Producte(rot1_L, qElbow_L);
        Vector3 P2_L = P1_L + (rot2_L * Vector3.up) * segment2Length;

        // Muñeca: pitch sobre X local del eslabón 2
        Quaternion qWrist_L = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
        Quaternion rot3_L = QuaternionLib.Producte(rot2_L, qWrist_L);
        Vector3 P3_L = P2_L + (rot3_L * Vector3.up) * segment3Length;

        // Efector final: yaw/pitch en local del EE
        Quaternion eeYawQ_L = QuaternionLib.DesDeEixAngle(Vector3.up, eeYaw);
        Quaternion eePitchQ_L = QuaternionLib.DesDeEixAngle(Vector3.right, eePitch);
        Quaternion rotEE_L = QuaternionLib.Producte(rot3_L, QuaternionLib.Producte(eeYawQ_L, eePitchQ_L));

        // ===== 2) PASO A MUNDO APLICANDO LA POSE DE LA BASE (una sola vez) =====
        Vector3 Pbase = baseTarget ? baseTarget.position : transform.position;
        Quaternion Rbase = baseTarget ? baseTarget.rotation : transform.rotation;

        Vector3 P0_W = Pbase;
        Vector3 P1_W = Pbase + Rbase * P1_L;
        Vector3 P2_W = Pbase + Rbase * P2_L;
        Vector3 P3_W = Pbase + Rbase * P3_L;

        Quaternion rot1_W = QuaternionLib.Producte(Rbase, rot1_L);
        Quaternion rot2_W = QuaternionLib.Producte(Rbase, rot2_L);
        Quaternion rot3_W = QuaternionLib.Producte(Rbase, rot3_L);
        Quaternion rotEE_W = QuaternionLib.Producte(Rbase, rotEE_L);

        // ===== 3) Pintado (sólo set de pos/rot, sin jerarquía) =====
        if (joint1Sphere) joint1Sphere.transform.position = P0_W;
        if (joint2Sphere) joint2Sphere.transform.position = P1_W;
        if (joint3Sphere) joint3Sphere.transform.position = P2_W;
        if (endEffector)
        {
            endEffector.transform.position = P3_W;
            endEffector.transform.rotation = rotEE_W; // rota EXACTAMENTE con el coche
        }
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
            // “Up” del coche para evitar rolls raros
            Vector3 upRef = baseTarget ? (baseTarget.rotation * Vector3.up) : Vector3.up;
            segment.transform.rotation = QuaternionLib.LookRotation(dir, upRef);
        }

        Vector3 s = segment.transform.localScale;
        s.z = Mathf.Sqrt(len2);
        segment.transform.localScale = s;
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
}