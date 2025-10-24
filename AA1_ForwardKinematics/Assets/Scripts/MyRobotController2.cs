using UnityEngine;

public class MyRobotController2 : MonoBehaviour
{
    // --- Joints (esferes) ---
    public GameObject joint1Sphere;
    public GameObject joint2Sphere;
    public GameObject joint3Sphere;
    public GameObject endEffector;

    // --- Segments (cubs visuals) ---
    public GameObject segment1Cube;
    public GameObject segment2Cube;
    public GameObject segment3Cube;

    // --- Longituds ---
    public float segment1Length = 2f;
    public float segment2Length = 1.5f;
    public float segment3Length = 1f;

    // --- Angles ---
    private float joint1Yaw = 0f;   // rotacio lateral (Y)
    private float joint1Pitch = 0f; // rotacio vertical (X)
    private float joint2 = 0f;      // colze
    private float joint3 = 0f;      // canell

    public float rotationSpeed = 50f;

    void Update()
    {
        HandleInput();
        ComputeForwardKinematics();
        UpdateSegments();
    }

    void HandleInput()
    {
        // --- Rotacio lateral del "hombro" ---
        if (Input.GetKey(KeyCode.A)) joint1Yaw -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) joint1Yaw += rotationSpeed * Time.deltaTime;

        // --- Rotacio vertical del "hombro" ---
        if (Input.GetKey(KeyCode.W)) joint1Pitch -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) joint1Pitch += rotationSpeed * Time.deltaTime;

        // --- Colze ---
        if (Input.GetKey(KeyCode.Q)) joint2 += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) joint2 -= rotationSpeed * Time.deltaTime;

        // --- Canell ---
        if (Input.GetKey(KeyCode.R)) joint3 += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.F)) joint3 -= rotationSpeed * Time.deltaTime;
    }

    void ComputeForwardKinematics()
    {
        // Posicion base
        Vector3 joint1Pos = transform.position;

        
        // Unity: Quaternion.Euler(x,y,z) => aplica Z, luego X, luego Y (orden ZXY)
        // Con nuestra libreria: q = R_y * R_x * R_z
        Quaternion qZ = QuaternionLib.DesDeEixAngle(Vector3.forward, 0f);
        Quaternion qX = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
        Quaternion qY = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);

        // Producte(b,a) = b * a (aplica a y luego b). Para ZXY:
        // rot1 = R_y * (R_x * R_z)
        Quaternion rot1 = QuaternionLib.Producte(qY,QuaternionLib.Producte(qX, qZ));

        Vector3 joint2Pos = joint1Pos + (rot1 * Vector3.forward) * segment1Length;

        // Rotacion del segundo segmento (igual que antes: rot1 * Euler(joint2,0,0))
        Quaternion colzeLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
        Quaternion rot2 = QuaternionLib.Producte(rot1, colzeLocal);
        Vector3 joint3Pos = joint2Pos + (rot2 * Vector3.forward) * segment2Length;

        // Rotacion del tercer segmento (igual que antes: rot2 * Euler(joint3,0,0))
        Quaternion canellLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
        Quaternion rot3 = QuaternionLib.Producte(rot2, canellLocal);
        Vector3 endEffectorPos = joint3Pos + (rot3 * Vector3.forward) * segment3Length;

        // Asignar posiciones a las esferas
        joint1Sphere.transform.position = joint1Pos;
        joint2Sphere.transform.position = joint2Pos;
        joint3Sphere.transform.position = joint3Pos;
        endEffector.transform.position = endEffectorPos;
    }


    void UpdateSegments()
    {
        PositionSegment(segment1Cube, joint1Sphere, joint2Sphere);
        PositionSegment(segment2Cube, joint2Sphere, joint3Sphere);
        PositionSegment(segment3Cube, joint3Sphere, endEffector);
    }

    void PositionSegment(GameObject segment, GameObject startJoint, GameObject endJoint)
    {
        Vector3 startPos = startJoint.transform.position;
        Vector3 endPos = endJoint.transform.position;
        Vector3 midPoint = (startPos + endPos) / 2f;

        Vector3 dir = endPos - startPos;
        float length = dir.magnitude;

        segment.transform.position = midPoint;
        if (dir.sqrMagnitude > 1e-12f)
        {
            // Sustituimos Quaternion.LookRotation(dir) por nuestra LookRotation
            segment.transform.rotation = QuaternionLib.LookRotation(dir, Vector3.up);
        }

        Vector3 localScale = segment.transform.localScale;
        localScale.z = length;
        segment.transform.localScale = localScale;
    }
}
