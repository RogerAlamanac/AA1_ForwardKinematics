using UnityEngine;

public class MyRobotController : MonoBehaviour
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
    private float joint1Yaw = 0f;   // rotació lateral (Y)
    private float joint1Pitch = 0f; // 🆕 rotació vertical (X)
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
        // --- Rotació lateral del "hombro" ---
        if (Input.GetKey(KeyCode.A)) joint1Yaw -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) joint1Yaw += rotationSpeed * Time.deltaTime;

        // --- Rotació vertical del "hombro" ---
        if (Input.GetKey(KeyCode.W)) joint1Pitch += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) joint1Pitch -= rotationSpeed * Time.deltaTime;

        // --- Colze ---
        if (Input.GetKey(KeyCode.Q)) joint2 += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) joint2 -= rotationSpeed * Time.deltaTime;

        // --- Canell ---
        if (Input.GetKey(KeyCode.R)) joint3 += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.F)) joint3 -= rotationSpeed * Time.deltaTime;
    }

    void ComputeForwardKinematics()
    {
        // Posició base
        Vector3 joint1Pos = transform.position;

        // 🧩 Rotació global del primer segment (Yaw + Pitch)
        Quaternion rot1 = Quaternion.Euler(joint1Pitch, joint1Yaw, 0);

        Vector3 joint2Pos = joint1Pos + rot1 * Vector3.forward * segment1Length;

        // Rotació del segon segment (colze)
        Quaternion rot2 = rot1 * Quaternion.Euler(joint2, 0, 0);
        Vector3 joint3Pos = joint2Pos + rot2 * Vector3.forward * segment2Length;

        // Rotació del tercer segment (canell)
        Quaternion rot3 = rot2 * Quaternion.Euler(joint3, 0, 0);
        Vector3 endEffectorPos = joint3Pos + rot3 * Vector3.forward * segment3Length;

        // Assignar posicions a les esferes
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
        if (dir != Vector3.zero)
            segment.transform.rotation = Quaternion.LookRotation(dir);

        Vector3 localScale = segment.transform.localScale;
        localScale.z = length;
        segment.transform.localScale = localScale;
    }
}
