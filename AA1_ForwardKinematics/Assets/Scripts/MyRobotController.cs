using UnityEngine;

public class MyRobotController : MonoBehaviour
{
    [Header("Base del brazo (no parent)")]
    public Transform baseTarget; // ancla del camión/vehículo
    public float baseHeight = 2.25f;
    public float baseWidth = 0f;



    [Header("Joints (visual)")]
    public GameObject joint1Sphere;   // hombro (P0)
    public GameObject joint2Sphere;   // codo   (P1)
    public GameObject joint3Sphere;   // muñeca (P2)
    public GameObject endEffector;    // efector final (P3)

    [Header("Segments (visual)")]
    public GameObject segment1Cube;   // P0-P1
    public GameObject segment2Cube;   // P1-P2
    public GameObject segment3Cube;   // P2-P3


    [Header("Longitudes")]
    public float segment1Length = 5f;
    public float segment2Length = 4f;
    public float segment3Length = 2f;


    [Header("Ángulos objetivo (grados)")]
    public float joint1YawTarget = 0f;    // hombro: yaw 
    public float joint1PitchTarget = 0f;  // hombro: pitch 
    public float joint2Target = 0f;       // codo: pitch 
    public float joint3Target = 0f;       // muñeca: pitch 

    [Header("Efector final (grados)")]
    public float endEffectorYawTarget = 0f;   
    public float endEffectorPitchTarget = 0f; 

    float joint1Yaw, joint1Pitch, joint2, joint3;
    float eeYaw, eePitch;


    [Header("Entrada y Suavizado")]
    public float rotationSpeed = 50f;   
    public float smoothTime = 0.1f;

    [Header("Límites (grados)")]
    public bool limitYaw = false; //Para limitar yaw del hombro                
    public Vector2 shoulderYawLimits = new Vector2(-180f, 180f);
    public Vector2 shoulderPitchLimits = new Vector2(-40f, 40f);
    public Vector2 elbowLimits = new Vector2(-120f, 120f);
    public Vector2 wristLimits = new Vector2(-120f, 120f);
    
    [SerializeField] int selectedJoint = 1;

    void Update()
    {
        HandleSelectionKeys();
        HandleArrowInput();

        ApplyLimits();
        SmoothAngles(Time.deltaTime);

        ComputeForwardKinematics();
        UpdateSegments();
    }

    void HandleSelectionKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedJoint = 1; 
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedJoint = 2;  
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedJoint = 3;  
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedJoint = 4; 
    }

    void HandleArrowInput()
    {
        float dt = Time.deltaTime;
        float step = rotationSpeed * dt;

        switch (selectedJoint)
        {
            case 1: // Hombro
                if (Input.GetKey(KeyCode.LeftArrow)) joint1YawTarget -= step;
                if (Input.GetKey(KeyCode.RightArrow)) joint1YawTarget += step;
                if (Input.GetKey(KeyCode.UpArrow)) joint1PitchTarget += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint1PitchTarget -= step;
                break;

            case 2: // Codo
                if (Input.GetKey(KeyCode.UpArrow)) joint2Target += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint2Target -= step;
                break;

            case 3: // Muñeca
                if (Input.GetKey(KeyCode.UpArrow)) joint3Target += step;
                if (Input.GetKey(KeyCode.DownArrow)) joint3Target -= step;
                break;

            case 4: // Efector final
                if (Input.GetKey(KeyCode.LeftArrow)) endEffectorYawTarget -= step;
                if (Input.GetKey(KeyCode.RightArrow)) endEffectorYawTarget += step;
                break;
        }
    }

    void ApplyLimits()
    {
        if (limitYaw)
            joint1YawTarget = LerpLib.Retallar(joint1YawTarget, shoulderYawLimits.x, shoulderYawLimits.y);

        joint1PitchTarget = LerpLib.Retallar(joint1PitchTarget, shoulderPitchLimits.x, shoulderPitchLimits.y);
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

    void ComputeForwardKinematics()
    {
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

        Vector3 Pbase = baseTarget ? baseTarget.position : transform.position;
        Quaternion Rbase = baseTarget ? baseTarget.rotation : transform.rotation;

        Vector3 P0_W = Pbase + Rbase * (P0_L); 
        Vector3 P1_W = Pbase + Rbase * (P1_L);
        Vector3 P2_W = Pbase + Rbase * (P2_L);
        Vector3 P3_W = Pbase + Rbase * (P3_L);

        //Quaternion rot1_W = QuaternionLib.Producte(Rbase, rot1_L);
        //Quaternion rot2_W = QuaternionLib.Producte(Rbase, rot2_L);
        //Quaternion rot3_W = QuaternionLib.Producte(Rbase, rot3_L);
        Quaternion rotEE_W = QuaternionLib.Producte(Rbase, rotEE_L);

        if (joint1Sphere) joint1Sphere.transform.position = P0_W;
        if (joint2Sphere) joint2Sphere.transform.position = P1_W;
        if (joint3Sphere) joint3Sphere.transform.position = P2_W;
        if (endEffector)
        {
            endEffector.transform.position = P3_W;
            endEffector.transform.rotation = rotEE_W;
        }
    }

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
            Vector3 upRef = baseTarget ? (baseTarget.rotation * Vector3.up) : Vector3.up;
            segment.transform.rotation = QuaternionLib.LookRotation(dir, upRef);
        }

        Vector3 s = segment.transform.localScale;
        s.z = Mathf.Sqrt(len2);
        segment.transform.localScale = s;
    }
}