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

	// --- Estado suavizado (runtime) ---
	float joint1Yaw, joint1Pitch, joint2, joint3;

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

	// --- Selección de joint ---
	[Header("Selección de Joint (1=hombro, 2=codo, 3=muñeca)")]
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

		// t = 1 - exp(-dt / tau)  (sin Mathf)
		float t = 1f - (float)System.Math.Exp(-(double)dt / (double)smoothTime);

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

	// ---------- FK ----------
	void ComputeForwardKinematics()
	{
		// P0 = base (sigue al target si está asignado; si no, usa este objeto)
		Vector3 P0 = baseTarget ? baseTarget.position : transform.position;

		// Rotación acumulada del 1er eslabón: R_y * R_x (R_z=0)
		Quaternion qZ = QuaternionLib.DesDeEixAngle(Vector3.forward, 0f);
		Quaternion qX = QuaternionLib.DesDeEixAngle(Vector3.right, joint1Pitch);
		Quaternion qY = QuaternionLib.DesDeEixAngle(Vector3.up, joint1Yaw);
		Quaternion rot1 = QuaternionLib.Producte(qY, QuaternionLib.Producte(qX, qZ));

		// P1 = P0 + rot1 * (0,1,0) * L1
		Vector3 P1 = P0 + (rot1 * Vector3.up) * segment1Length;

		// rot2 = rot1 * R_x(joint2)
		Quaternion colzeLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint2);
		Quaternion rot2 = QuaternionLib.Producte(rot1, colzeLocal);

		// P2 = P1 + rot2 * (0,1,0) * L2
		Vector3 P2 = P1 + (rot2 * Vector3.up) * segment2Length;

		// rot3 = rot2 * R_x(joint3)
		Quaternion canellLocal = QuaternionLib.DesDeEixAngle(Vector3.right, joint3);
		Quaternion rot3 = QuaternionLib.Producte(rot2, canellLocal);

		// P3 = P2 + rot3 * (0,1,0) * L3
		Vector3 P3 = P2 + (rot3 * Vector3.up) * segment3Length;

		// Colocar joints/efector (visual)
		if (joint1Sphere) joint1Sphere.transform.position = P0;
		if (joint2Sphere) joint2Sphere.transform.position = P1;
		if (joint3Sphere) joint3Sphere.transform.position = P2;
		if (endEffector)
		{
			endEffector.transform.position = P3;
			endEffector.transform.rotation = rot3; // orientación acumulada
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

		float len2 = dir.x * dir.x + dir.y * dir.y + dir.z * dir.z; // sin Vector3.sqrMagnitude si quieres ser estricto
		if (len2 > 1e-12f)
			segment.transform.rotation = QuaternionLib.LookRotation(dir, Vector3.up);

		Vector3 s = segment.transform.localScale;
		s.z = (float)System.Math.Sqrt(len2); // sin Mathf.Sqrt
		segment.transform.localScale = s;
	}
}