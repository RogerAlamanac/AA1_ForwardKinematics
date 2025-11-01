using System;
using UnityEngine;

public class TruckController : MonoBehaviour
{
	// --- Cuerpo principal ---
	public GameObject truckBody;

	// --- Ruedas ---
	public GameObject frontLeftWheel;
	public GameObject frontRightWheel;
	public GameObject rearLeftWheel;
	public GameObject rearRightWheel;

	// --- Offsets locales respecto al cuerpo ---
	public Vector3 frontLeftOffset;
	public Vector3 frontRightOffset;
	public Vector3 rearLeftOffset;
	public Vector3 rearRightOffset;

	// --- Movimiento / Dirección ---
	[Header("Movimiento")]
	public float maxSpeed = 8f;
	public float acceleration = 4f;
	public float rotationSpeed = 50f;   // Velocidad de giro de la carrocería (deg/s aprox)
	public float wheelRadius = 0.5f;
	public float steerAngleMax = 25f;   // Ángulo máximo de dirección (deg)
	public float steerSmooth = 5f;      // Suavizado de la dirección
	public float speedDeadzone = 0.02f; // Umbral para considerar “parado” (evitar creep)

	// --- Estado interno ---
	float currentSpeed = 0f;            // Magnitud de velocidad (unidades/s)
	float currentSteer = 0f;            // Dirección suavizada (deg)
	float currentSteerVisual = 0f;      // Dirección visual de ruedas delanteras (deg)

	// Inputs crudos (para decidir si mover/rotar o no)
	float moveInputRaw = 0f;            // W/S -> -1..1
	float steerInputRaw = 0f;           // A/D -> -1..1

	// Giro ACUMULADO por rueda (deg)
	float spinFL = 0f, spinFR = 0f, spinRL = 0f, spinRR = 0f;


	void Start()
	{
		// Captura offsets locales desde la escena (una vez)
		frontLeftOffset = GetLocalOffset(frontLeftWheel);
		frontRightOffset = GetLocalOffset(frontRightWheel);
		rearLeftOffset = GetLocalOffset(rearLeftWheel);
		rearRightOffset = GetLocalOffset(rearRightWheel);
    }

	void Update()
	{
		HandleInput();
		UpdateBody();
		UpdateWheels();
	}

	// ---------------- Input ----------------
	void HandleInput()
	{
		moveInputRaw = 0f;
		steerInputRaw = 0f;

		if (Input.GetKey(KeyCode.W)) moveInputRaw = 1f;
		else if (Input.GetKey(KeyCode.S)) moveInputRaw = -1f;

		if (Input.GetKey(KeyCode.D)) steerInputRaw = 1f;
		else if (Input.GetKey(KeyCode.A)) steerInputRaw = -1f;

		// Velocidad objetivo y lerp suave (usa tu LerpLib)
		float targetSpeed = moveInputRaw * maxSpeed;
		currentSpeed = LerpLib.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

		// Clamp duro a 0 si no hay gas para evitar creep
		if (moveInputRaw == 0f && System.Math.Abs(currentSpeed) < 0.05f)
			currentSpeed = 0f;

		// Dirección objetivo y lerp suave
		float targetSteer = steerInputRaw * steerAngleMax;
		currentSteer = LerpLib.Lerp(currentSteer, targetSteer, steerSmooth * Time.deltaTime);
	}

	// ---------------- Carrocería ----------------
	void UpdateBody()
	{
		if (!truckBody) return;

		// SOLO avanzamos si hay gas (W/S). A/D NO traslada el coche parado.
		if (moveInputRaw != 0f)
		{
			Vector3 forward = truckBody.transform.forward;
			truckBody.transform.position += forward * currentSpeed * Time.deltaTime;
		}

		// SOLO giramos la carrocería si hay gas (como un coche real)
		if (moveInputRaw != 0f && Math.Abs(currentSpeed) > 0f)
		{
			float dirSign = currentSpeed >= 0f ? 1f : -1f;
			float turnAmount = (currentSteer / steerAngleMax) * rotationSpeed * dirSign * Time.deltaTime;

			Quaternion qTurn = QuaternionLib.DesDeEixAngle(Vector3.up, turnAmount);
			truckBody.transform.rotation = QuaternionLib.Producte(qTurn, truckBody.transform.rotation);
		}
	}




	// ---------------- Ruedas ----------------
	void UpdateWheels()
	{
		if (!truckBody) return;

		// Incremento de giro por distancia (deg/frame): dist = v*dt; vueltas = dist/(2PI R); deg = vueltas*360
		float spinDeltaDeg = 0f;
		if (wheelRadius > 1e-6f)
			spinDeltaDeg = (currentSpeed * Time.deltaTime / (2f * LerpLib.PI * wheelRadius)) * 360f;

		// Acumular giro por rueda
		spinFL += spinDeltaDeg;
		spinFR += spinDeltaDeg;
		spinRL += spinDeltaDeg;
		spinRR += spinDeltaDeg;

		// Suavizado visual del ángulo de dirección de las delanteras (siempre, aunque parados)
		currentSteerVisual = LerpLib.Lerp(currentSteerVisual, currentSteer, 8f * Time.deltaTime);
		Quaternion qSteer = QuaternionLib.DesDeEixAngle(Vector3.up, currentSteerVisual);

		// Actualizar ruedas: posición por offset local, rotación = base * steer(front) * spin(local X)
		UpdateWheel(frontLeftWheel, frontLeftOffset, qSteer, spinFL, true);
		UpdateWheel(frontRightWheel, frontRightOffset, qSteer, spinFR, true);
		UpdateWheel(rearLeftWheel, rearLeftOffset, Quaternion.identity, spinRL, false);
		UpdateWheel(rearRightWheel, rearRightOffset, Quaternion.identity, spinRR, false);
	}

	void UpdateWheel(GameObject wheel, Vector3 offsetLocal, Quaternion steerRot, float spinAngleDeg, bool isFront)
	{
		if (!wheel) return;

		Quaternion bodyRot = truckBody.transform.rotation;

		// Posición global = pos body + rot body * offsetLocal
		Vector3 worldOffset = QuaternionLib.RotarVector(bodyRot, offsetLocal);
		wheel.transform.position = truckBody.transform.position + worldOffset;

		// Rotación global = rot body * (steer si delantera) * (spin en eje local X)
		Quaternion steer = isFront ? steerRot : Quaternion.identity;
		Quaternion spinLocalX = QuaternionLib.DesDeEixAngle(Vector3.right, spinAngleDeg);

		Quaternion totalRot = QuaternionLib.Producte(bodyRot, QuaternionLib.Producte(steer, spinLocalX));
		wheel.transform.rotation = totalRot;
	}

	// ---------------- Utilidades ----------------
	Vector3 GetLocalOffset(GameObject wheel)
	{
		if (!truckBody || !wheel) return Vector3.zero;

		// world -> local del body: offLocal = R^-1 * (Pwheel - Pbody)
		Quaternion invBodyRot = QuaternionLib.Inversa(truckBody.transform.rotation);
		Vector3 delta = wheel.transform.position - truckBody.transform.position;
		return QuaternionLib.RotarVector(invBodyRot, delta);
	}
}